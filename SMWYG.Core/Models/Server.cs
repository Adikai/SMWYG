using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SMWYG.Models
{
    public class Server
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("icon")]
        public string? Icon { get; set; }

        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        [InverseProperty(nameof(User.OwnedServers))]
        public User Owner { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    }
}
