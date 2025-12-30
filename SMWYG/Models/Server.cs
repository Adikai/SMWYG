using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SMWYG.Models
{
    public class Server
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    }
}
