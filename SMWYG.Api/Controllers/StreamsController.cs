using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMWYG;
using SMWYG.Models;

namespace SMWYG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StreamsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var streams = await _db.ActiveStreams.Include(s => s.Channel).Include(s => s.Streamer).ToListAsync();
            return Ok(streams.Where(s => s.EndedAt == null));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var stream = await _db.ActiveStreams.Include(s => s.Channel).Include(s => s.Streamer).FirstOrDefaultAsync(s => s.Id == id);
            if (stream == null) return NotFound();
            return Ok(stream);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ActiveStream stream)
        {
            stream.Id = Guid.NewGuid();
            stream.StartedAt = DateTime.UtcNow;
            _db.ActiveStreams.Add(stream);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = stream.Id }, stream);
        }

        [HttpPut("{id}/end")]
        public async Task<IActionResult> End(Guid id)
        {
            var stream = await _db.ActiveStreams.FindAsync(id);
            if (stream == null) return NotFound();
            stream.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
