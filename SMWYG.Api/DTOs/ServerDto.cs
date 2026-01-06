using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class ServerDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public Guid OwnerId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
