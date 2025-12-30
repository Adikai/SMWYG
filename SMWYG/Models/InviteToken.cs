using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SMWYG.Models
{
    public class InviteToken
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("token")]
        public string Token { get; set; } = string.Empty;
        [Column("created_by")]
        public Guid CreatedBy { get; set; }
        public User Creator { get; set; } = null!;
        [Column("used_by")]
        public Guid? UsedBy { get; set; }
        public User? User { get; set; }
        [Column("is_used")]
        public bool IsUsed { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }
        [Column("max_uses")]
        public int MaxUses { get; set; } = 1;
    }
}
