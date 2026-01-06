using System.ComponentModel.DataAnnotations;

namespace SMWYG.Api.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsAdmin { get; set; }
    }
}
