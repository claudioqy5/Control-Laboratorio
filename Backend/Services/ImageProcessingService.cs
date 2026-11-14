using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ControlLaboratorio.API.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly IWebHostEnvironment _env;

        public ImageProcessingService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveAsWebpAsync(IFormFile file, string subFolder, int? maxWidth = null, int quality = 80)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("El archivo de imagen no es válido.", nameof(file));
            }

            using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);

            if (maxWidth.HasValue && image.Width > maxWidth.Value)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth.Value, 0)
                }));
            }

            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var targetFolder = Path.Combine(rootPath, subFolder);

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(targetFolder, fileName);

            await image.SaveAsWebpAsync(filePath, new WebpEncoder
            {
                Quality = quality
            });

            return $"/api/static/{subFolder}/{fileName}";
        }

        public async Task<string> SaveBytesAsWebpAsync(byte[] imageBytes, string subFolder, string? prefix = null, int? maxWidth = null, int quality = 80)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Los bytes de imagen no son válidos.", nameof(imageBytes));
            }

            using var stream = new MemoryStream(imageBytes);
            using var image = await Image.LoadAsync(stream);

            if (maxWidth.HasValue && image.Width > maxWidth.Value)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth.Value, 0)
                }));
            }

            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var targetFolder = Path.Combine(rootPath, subFolder);

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var namePart = string.IsNullOrWhiteSpace(prefix) ? Guid.NewGuid().ToString() : $"{prefix.Trim()}_{Guid.NewGuid()}";
            var fileName = $"{namePart}.webp";
            var filePath = Path.Combine(targetFolder, fileName);

            await image.SaveAsWebpAsync(filePath, new WebpEncoder
            {
                Quality = quality
            });

            return $"/api/static/{subFolder}/{fileName}";
        }

        public void DeleteImage(string? relativeUrl, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;

            try
            {
                var fileName = Path.GetFileName(relativeUrl);
                if (string.IsNullOrEmpty(fileName)) return;

                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(rootPath, subFolder, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }
    }
}
