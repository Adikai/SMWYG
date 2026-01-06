using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        public bool IsAdmin { get; set; }
    }
}
