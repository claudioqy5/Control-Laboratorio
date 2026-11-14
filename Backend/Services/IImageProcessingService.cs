using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ControlLaboratorio.API.Services
{
    public interface IImageProcessingService
    {
        Task<string> SaveAsWebpAsync(IFormFile file, string subFolder, int? maxWidth = null, int quality = 80);
        Task<string> SaveBytesAsWebpAsync(byte[] imageBytes, string subFolder, string? prefix = null, int? maxWidth = null, int quality = 80);
        void DeleteImage(string? relativeUrl, string subFolder);
    }
}
