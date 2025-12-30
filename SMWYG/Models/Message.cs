using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SMWYG.Models
{
    public class Message
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("channel_id")]
        public Guid ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;
        [Column("author_id")]
        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;
        [Column("content")]
        public string Content { get; set; } = string.Empty;
        [Column("sent_at")]
        public DateTime SentAt { get; set; }
        [Column("edited_at")]
        public DateTime? EditedAt { get; set; }
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}
