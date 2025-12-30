using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SMWYG.Models
{
    public class ServerMember
    {
        // Foreign keys
        [Column("server_id")]
        public Guid ServerId { get; set; }
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("joined_at")] 
        public DateTime JoinedAt { get; set; }
        [Column("role")]
        public string Role { get; set; } = "member";

        // Navigation properties
        public Server Server { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
