using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class CreateServerDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid OwnerId { get; set; }
    }
}
