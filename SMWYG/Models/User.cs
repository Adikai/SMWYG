using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SMWYG.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }

        // Navigation
        public ICollection<Server> OwnedServers { get; set; } = new List<Server>();
        public ICollection<ServerMember> ServerMemberships { get; set; } = new List<ServerMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
