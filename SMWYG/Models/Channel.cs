using System;
using System.Collections.Generic;
using System.Text;

namespace SMWYG.Models
{
    public class Channel
    {
        public Guid Id { get; set; }
        public Guid ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // "text" or "voice"
        public string? Category { get; set; }
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
