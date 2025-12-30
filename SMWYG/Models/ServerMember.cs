using System;
using System.Collections.Generic;
using System.Text;

namespace SMWYG.Models
{
    public class ServerMember
    {
        // Foreign keys
        public Guid ServerId { get; set; }
        public Guid UserId { get; set; }

        public DateTime JoinedAt { get; set; }
        public string Role { get; set; } = "member";

        // Navigation properties
        public Server Server { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
