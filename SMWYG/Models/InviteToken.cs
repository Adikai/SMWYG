using System;
using System.Collections.Generic;
using System.Text;

namespace SMWYG.Models
{
    public class InviteToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public User Creator { get; set; } = null!;
        public Guid? UsedBy { get; set; }
        public User? User { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int MaxUses { get; set; } = 1;
    }
}
