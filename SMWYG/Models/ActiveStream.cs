using System;
using System.Collections.Generic;
using System.Text;

namespace SMWYG.Models
{
    public class ActiveStream
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;
        public Guid StreamerId { get; set; }
        public User Streamer { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
