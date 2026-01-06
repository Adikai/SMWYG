using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMWYG.Models
{
    public class ServerMember
    {
        [Column("server_id")]
        public Guid ServerId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }

        [Column("role")]
        public string Role { get; set; } = "member";

        public Server Server { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
