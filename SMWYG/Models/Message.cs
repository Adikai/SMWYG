using System;
using System.Collections.Generic;
using System.Text;

namespace SMWYG.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
