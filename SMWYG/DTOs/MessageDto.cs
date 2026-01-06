
using SMWYG.Models;

namespace SMWYG.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Guid AuthorId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentContentType { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public User? Author { get; set; }
    }
}
