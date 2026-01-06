using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMWYG;
using SMWYG.Models;

namespace SMWYG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InviteTokensController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InviteTokensController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tokens = await _db.InviteTokens.Include(t => t.Creator).ToListAsync();
            return Ok(tokens);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var token = await _db.InviteTokens.Include(t => t.Creator).FirstOrDefaultAsync(t => t.Id == id);
            if (token == null) return NotFound();
            return Ok(token);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InviteToken token)
        {
            token.Id = Guid.NewGuid();
            token.CreatedAt = DateTime.UtcNow;
            _db.InviteTokens.Add(token);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = token.Id }, token);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] InviteToken updated)
        {
            var token = await _db.InviteTokens.FindAsync(id);
            if (token == null) return NotFound();
            token.ExpiresAt = updated.ExpiresAt;
            token.MaxUses = updated.MaxUses;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var token = await _db.InviteTokens.FindAsync(id);
            if (token == null) return NotFound();
            _db.InviteTokens.Remove(token);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("consume")]
        public async Task<IActionResult> Consume([FromBody] ConsumeRequest req)
        {
            var token = await _db.InviteTokens.FirstOrDefaultAsync(t => t.Token == req.Token);
            if (token == null) return NotFound();
            if (token.ExpiresAt.HasValue && token.ExpiresAt.Value <= DateTime.UtcNow) return BadRequest("Token expired");
            if (token.MaxUses <= 0) return BadRequest("No uses remaining");

            token.MaxUses -= 1;
            if (token.MaxUses == 0) token.IsUsed = true;
            token.UsedBy = req.UserId;
            await _db.SaveChangesAsync();
            return Ok();
        }

        public class ConsumeRequest
        {
            public string Token { get; set; } = string.Empty;
            public Guid UserId { get; set; }
        }
    }
}
