using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMWYG.Models
{
    public class Message
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("channel_id")]
        public Guid ChannelId { get; set; }

        // navigation properties can be null when creating via API payload; make them nullable
        public Channel? Channel { get; set; }

        [Column("author_id")]
        public Guid AuthorId { get; set; }

        public User? Author { get; set; }

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
