using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Windows;

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
        public ICollection<Server> OwnedServers { get; set; } = new List<Server>();
        public ICollection<ServerMember> ServerMemberships { get; set; } = new List<ServerMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
