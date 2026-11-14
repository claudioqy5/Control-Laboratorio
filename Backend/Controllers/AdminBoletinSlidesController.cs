using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Services;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/admin/boletin-slides")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminBoletinSlidesController : ControllerBase
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = [".webp", ".jpg", ".jpeg", ".png"];

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IImageProcessingService _imageService;

        public AdminBoletinSlidesController(ApplicationDbContext context, IWebHostEnvironment env, IImageProcessingService imageService)
        {
            _context = context;
            _env = env;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSlides()
        {
            var slides = await _context.BoletinSlides
                .OrderByDescending(s => s.IdSlide)
                .AsNoTracking()
                .ToListAsync();

            return Ok(slides);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSlide(
            [FromForm] IFormFile? image,
            [FromForm] string titulo,
            [FromForm] string? descripcion,
            [FromForm] byte formato,
            [FromForm] string? enlaceUrl)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { message = "Se requiere una imagen válida." });
            }

            if (image.Length > MaxImageSizeBytes)
            {
                return BadRequest(new { message = "El tamaño de la imagen no debe superar los 5 MB." });
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Formato no permitido. Solo se aceptan extensiones .webp, .jpg, .jpeg y .png." });
            }

            if (string.IsNullOrWhiteSpace(titulo))
            {
                return BadRequest(new { message = "El título es obligatorio." });
            }

            if (formato != (byte)FormatoSlide.Vertical && formato != (byte)FormatoSlide.Bipartito)
            {
                return BadRequest(new { message = "El formato especificado es inválido. Debe ser 1 (Vertical) o 2 (Bipartito)." });
            }

            if (formato == (byte)FormatoSlide.Bipartito && string.IsNullOrWhiteSpace(descripcion))
            {
                return BadRequest(new { message = "La descripción es obligatoria cuando el formato es Bipartito." });
            }

            var imagenUrl = await _imageService.SaveAsWebpAsync(image, "boletines", maxWidth: 1920, quality: 80);

            var slide = new BoletinSlide
            {
                Titulo = titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                ImagenUrl = imagenUrl,
                Formato = (FormatoSlide)formato,
                EnlaceUrl = string.IsNullOrWhiteSpace(enlaceUrl) ? null : enlaceUrl.Trim()
            };

            _context.BoletinSlides.Add(slide);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSlides), new { id = slide.IdSlide }, slide);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlide(int id)
        {
            var slide = await _context.BoletinSlides.FindAsync(id);
            if (slide == null)
            {
                return NotFound();
            }

            _imageService.DeleteImage(slide.ImagenUrl, "boletines");

            _context.BoletinSlides.Remove(slide);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
