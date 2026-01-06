using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class CreateChannelDto
    {
        [Required]
        public Guid ServerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(text|voice)$")]
        public string Type { get; set; } = "text";

        public string? Category { get; set; }
    }
}
