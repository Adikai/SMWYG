using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class InviteTokenDto
    {
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public Guid CreatedBy { get; set; }
        public Guid? UsedBy { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int MaxUses { get; set; }
    }
}
