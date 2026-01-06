using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SMWYG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly long _maxBytes;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
        };

        public UploadsController(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            var limitMb = config.GetValue<int?>("AppSettings:UploadSizeLimitMB") ?? 50;
            _maxBytes = (long)limitMb * 1024 * 1024;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            if (file.Length > _maxBytes)
                return BadRequest(new { error = $"File too large. Maximum allowed size is {_maxBytes / (1024 * 1024)} MB." });

            // allow images and gifs
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "Only image uploads are allowed (image/*)." });

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                return BadRequest(new { error = "File type not allowed. Supported: png, jpg, jpeg, gif, bmp, webp." });

            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            var url = $"{baseUrl}/uploads/{fileName}";

            return Ok(new { Url = url, ContentType = file.ContentType });
        }
    }
}
