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
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public MessagesController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _db.Messages.Include(m => m.Author).Include(m => m.Channel).ToListAsync();
            var dtos = messages.Select(m =>
            {
                var dto = _mapper.Map<MessageDto>(m);
                dto.Author = _mapper.Map<UserDto>(m.Author);
                return dto;
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var message = await _db.Messages.Include(m => m.Author).Include(m => m.Channel).FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            var dto = _mapper.Map<MessageDto>(message);
            dto.Author = _mapper.Map<UserDto>(message.Author);
            return Ok(dto);
        }

        [HttpGet("channel/{channelId}")]
        public async Task<IActionResult> GetByChannel(Guid channelId, [FromQuery] DateTime? since = null)
        {
            var query = _db.Messages
                .Where(m => m.ChannelId == channelId)
                .Include(m => m.Author)
                .OrderBy(m => m.SentAt);

            if (since.HasValue && since.Value > DateTime.MinValue)
            {
                query = query.Where(m => m.SentAt > since.Value).OrderBy(m => m.SentAt);
            }

            var msgs = await query.ToListAsync();
            var dtos = msgs.Select(m =>
            {
                var dto = _mapper.Map<MessageDto>(m);
                dto.Author = _mapper.Map<UserDto>(m.Author);
                return dto;
            }).ToList();
            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MessageDto messageDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = messageDto.ChannelId,
                AuthorId = messageDto.AuthorId,
                Content = messageDto.Content,
                SentAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var dto = _mapper.Map<MessageDto>(message);
            dto.Author = _mapper.Map<UserDto>(await _db.Users.FindAsync(message.AuthorId));
            return CreatedAtAction(nameof(Get), new { id = message.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MessageDto updated)
        {
            var message = await _db.Messages.FindAsync(id);
            if (message == null) return NotFound();
            message.Content = updated.Content;
            message.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var message = await _db.Messages.FindAsync(id);
            if (message == null) return NotFound();
            message.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
