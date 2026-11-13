using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoletinSlidesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BoletinSlidesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoletinSlide>>> GetSlides()
        {
            var slides = await _context.BoletinSlides
                .OrderByDescending(s => s.IdSlide)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            return Ok(slides);
        }
    }
}
