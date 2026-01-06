using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Guid AuthorId { get; set; }

        // include minimal author info for UI (profile picture, display name, username)
        public UserDto? Author { get; set; }

        [StringLength(2000, MinimumLength = 0)]
        public string? Content { get; set; }

        // Optional attachment (image/gif) URL served by the API
        public string? AttachmentUrl { get; set; }
        public string? AttachmentContentType { get; set; }

        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
