using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMWYG.Models
{
    public class User
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("username")]
        public string Username { get; set; } = string.Empty;
        [Column("display_name")]
        public string? DisplayName { get; set; }
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;
        [Column("profile_picture")]
        public string? ProfilePicture { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        // Navigation
        public ICollection<Server>? OwnedServers { get; set; }
        public ICollection<ServerMember>? ServerMemberships { get; set; }
        public ICollection<Message>? Messages { get; set; }

        [NotMapped]
        public bool IsDeactivated => string.IsNullOrEmpty(PasswordHash);
    }
}
