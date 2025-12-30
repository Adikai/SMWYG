using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMWYG.Models
{
    public class ActiveStream
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("channel_id")]
        public Guid ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;
        [Column("streamer_id")]
        public Guid StreamerId { get; set; }
        public User Streamer { get; set; } = null!;
        [Column("started_at")]
        public DateTime StartedAt { get; set; }
        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }
    }
}
