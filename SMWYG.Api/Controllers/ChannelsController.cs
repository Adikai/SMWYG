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
    public class ChannelsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public ChannelsController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var channels = await _db.Channels.Include(c => c.Server).ToListAsync();
            var dtos = channels.Select(c => _mapper.Map<ChannelDto>(c));
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var channel = await _db.Channels.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == id);
            if (channel == null) return NotFound();
            return Ok(_mapper.Map<ChannelDto>(channel));
        }

        [HttpGet("server/{serverId}")]
        public async Task<IActionResult> GetByServer(Guid serverId)
        {
            var channels = await _db.Channels.Where(c => c.ServerId == serverId).OrderBy(c => c.Position).ToListAsync();
            return Ok(channels.Select(c => _mapper.Map<ChannelDto>(c)));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateChannelDto create)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var server = await _db.Servers.FindAsync(create.ServerId);
            if (server == null) return BadRequest("Server does not exist");

            int nextPosition = (await _db.Channels.Where(c => c.ServerId == create.ServerId).Select(c => (int?)c.Position).MaxAsync()) ?? -1;

            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                ServerId = create.ServerId,
                Name = create.Name,
                Type = create.Type,
                Category = create.Category,
                Position = nextPosition + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Channels.Add(channel);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = channel.Id }, _mapper.Map<ChannelDto>(channel));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ChannelDto updatedDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var channel = await _db.Channels.FindAsync(id);
            if (channel == null) return NotFound();

            channel.Name = updatedDto.Name;
            channel.Type = updatedDto.Type;
            channel.Category = updatedDto.Category;
            channel.Position = updatedDto.Position;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("server/{serverId}/reorder")]
        public async Task<IActionResult> Reorder(Guid serverId, [FromBody] Guid[] orderedChannelIds)
        {
            var channels = await _db.Channels.Where(c => c.ServerId == serverId).ToListAsync();
            if (!channels.Any()) return NotFound();

            var dict = channels.ToDictionary(c => c.Id);
            for (int i = 0; i < orderedChannelIds.Length; i++)
            {
                if (dict.TryGetValue(orderedChannelIds[i], out var ch))
                {
                    ch.Position = i;
                }
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var channel = await _db.Channels.FindAsync(id);
            if (channel == null) return NotFound();
            _db.Channels.Remove(channel);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
