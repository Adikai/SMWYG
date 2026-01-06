using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMWYG;
using SMWYG.Api.DTOs;
using SMWYG.Models;
using SMWYG.Utils;

namespace SMWYG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public UsersController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.AsNoTracking().ToListAsync();
            return Ok(users.Select(u => _mapper.Map<UserDto>(u)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto create)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == create.Username.ToLower()))
                return Conflict("Username already exists");

            var user = _mapper.Map<User>(create);
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.PasswordHash = PasswordHelper.HashPassword(create.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var dto = _mapper.Map<UserDto>(user);
            return CreatedAtAction(nameof(Get), new { id = user.Id }, dto);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.InviteToken)) return BadRequest("Invite token is required");
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required");

            var normalizedToken = request.InviteToken.Trim().ToUpperInvariant();
            var invite = await _db.InviteTokens.FirstOrDefaultAsync(t => t.Token.ToUpper() == normalizedToken);
            if (invite == null) return BadRequest("Invalid invite token");
            if (invite.IsUsed || invite.MaxUses <= 0) return BadRequest("Invite token already used");
            if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value <= DateTime.UtcNow)
                return BadRequest("Invite token expired");

            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
                return Conflict("Username already exists");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsAdmin = false
            };

            _db.Users.Add(user);

            invite.MaxUses = Math.Max(0, invite.MaxUses - 1);
            invite.IsUsed = invite.MaxUses == 0;
            invite.UsedBy = user.Id;

            await _db.SaveChangesAsync();
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Missing username or password");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (user == null || !PasswordHelper.VerifyPassword(user.PasswordHash, request.Password))
                return Unauthorized();

            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserDto updated)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Username = updated.Username;
            user.DisplayName = updated.DisplayName;
            user.ProfilePicture = updated.ProfilePicture;
            user.IsAdmin = updated.IsAdmin;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(user.PasswordHash)) return BadRequest("User already deactivated");

            var memberships = await _db.ServerMembers.Where(sm => sm.UserId == id).ToListAsync();
            if (memberships.Count > 0) _db.ServerMembers.RemoveRange(memberships);

            var streams = await _db.ActiveStreams.Where(a => a.StreamerId == id).ToListAsync();
            if (streams.Count > 0) _db.ActiveStreams.RemoveRange(streams);

            user.PasswordHash = string.Empty;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.PasswordHash)) return BadRequest("User is already active");

            string tempPassword = Guid.NewGuid().ToString()[..12];
            user.PasswordHash = PasswordHelper.HashPassword(tempPassword);
            await _db.SaveChangesAsync();

            return Ok(new ReactivateResult { TemporaryPassword = tempPassword });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var ownedServers = await _db.Servers.Where(s => s.OwnerId == user.Id).ToListAsync();
            if (ownedServers.Count > 0) _db.Servers.RemoveRange(ownedServers);

            var memberships = await _db.ServerMembers.Where(sm => sm.UserId == user.Id).ToListAsync();
            if (memberships.Count > 0) _db.ServerMembers.RemoveRange(memberships);

            var messages = await _db.Messages.Where(m => m.AuthorId == user.Id).ToListAsync();
            if (messages.Count > 0) _db.Messages.RemoveRange(messages);

            var streams = await _db.ActiveStreams.Where(a => a.StreamerId == user.Id).ToListAsync();
            if (streams.Count > 0) _db.ActiveStreams.RemoveRange(streams);

            var tokensCreated = await _db.InviteTokens.Where(t => t.CreatedBy == user.Id).ToListAsync();
            foreach (var token in tokensCreated) token.CreatedBy = Guid.Empty;

            var tokensUsed = await _db.InviteTokens.Where(t => t.UsedBy == user.Id).ToListAsync();
            foreach (var token in tokensUsed) token.UsedBy = null;

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RegisterRequest
        {
            public string InviteToken { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string Password { get; set; } = string.Empty;
        }

        public class ReactivateResult
        {
            public string TemporaryPassword { get; set; } = string.Empty;
        }
    }
}
