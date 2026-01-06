using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMWYG;
using SMWYG.Api.DTOs;
using SMWYG.Models;

namespace SMWYG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public ServersController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var servers = await _db.Servers.Include(s => s.Owner).ToListAsync();
            return Ok(servers.Select(s => _mapper.Map<ServerDto>(s)));
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetForUser(Guid userId)
        {
            var servers = await _db.Servers
                .Where(s => _db.ServerMembers.Any(sm => sm.ServerId == s.Id && sm.UserId == userId))
                .OrderBy(s => s.Name)
                .ToListAsync();
            return Ok(servers.Select(s => _mapper.Map<ServerDto>(s)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var server = await _db.Servers
                .Include(s => s.Owner)
                .Include(s => s.Members)
                .Include(s => s.Channels)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (server == null) return NotFound();
            return Ok(_mapper.Map<ServerDto>(server));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServerDto create)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // ensure owner exists
            var owner = await _db.Users.FindAsync(create.OwnerId);
            if (owner == null) return BadRequest("Owner user not found");

            var server = new Server
            {
                Id = Guid.NewGuid(),
                Name = create.Name,
                OwnerId = create.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Servers.Add(server);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = server.Id }, _mapper.Map<ServerDto>(server));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ServerDto updated)
        {
            var server = await _db.Servers.FindAsync(id);
            if (server == null) return NotFound();
            server.Name = updated.Name;
            server.Icon = updated.Icon;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var server = await _db.Servers.FindAsync(id);
            if (server == null) return NotFound();
            _db.Servers.Remove(server);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
        {
            var server = await _db.Servers.FindAsync(id);
            if (server == null) return NotFound();

            var exists = await _db.ServerMembers.AnyAsync(sm => sm.ServerId == id && sm.UserId == request.UserId);
            if (exists) return Conflict("User is already a member");

            var membership = new ServerMember
            {
                ServerId = id,
                UserId = request.UserId,
                Role = request.Role ?? "member",
                JoinedAt = DateTime.UtcNow
            };

            _db.ServerMembers.Add(membership);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("findByInvite/{code}")]
        public async Task<IActionResult> FindByInvite(string code)
        {
            var servers = await _db.Servers.AsNoTracking().ToListAsync();
            var normalized = code.Trim().ToUpperInvariant();
            var match = servers.FirstOrDefault(s => GenerateInviteCode(s.Id).Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (match == null) return NotFound();
            return Ok(_mapper.Map<ServerDto>(match));
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(Guid id)
        {
            var members = await _db.ServerMembers.Where(sm => sm.ServerId == id).ToListAsync();
            return Ok(members);
        }

        private static string GenerateInviteCode(Guid serverId)
        {
            var compact = serverId.ToString("N").ToUpperInvariant();
            return compact.Substring(0, Math.Min(8, compact.Length));
        }

        public class AddMemberRequest
        {
            public Guid UserId { get; set; }
            public string? Role { get; set; }
        }
    }
}
