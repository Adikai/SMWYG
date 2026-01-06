using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMWYG.Models
{
    public class Channel
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("server_id")]
        public Guid ServerId { get; set; }

        public Server? Server { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = "text";

        [Column("category")]
        public string? Category { get; set; }

        [Column("position")]
        public int Position { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}
