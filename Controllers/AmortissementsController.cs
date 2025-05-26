using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmortissementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AmortissementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Amortissements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Amortissement>>> GetAmortissements()
        {
            return await _context.Amortissements
                .Include(a => a.Immobilisation)
                .ToListAsync();
        }

        // GET: api/Amortissements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Amortissement>> GetAmortissement(int id)
        {
            var amortissement = await _context.Amortissements
                .Include(a => a.Immobilisation)
                .FirstOrDefaultAsync(a => a.IdAmortissement == id);

            if (amortissement == null)
            {
                return NotFound();
            }

            return amortissement;
        }

        // GET: api/Amortissements/Bien/5
        [HttpGet("Bien/{idBien}")]
        public async Task<ActionResult<IEnumerable<Amortissement>>> GetAmortissementsByBien(int idBien)
        {
            if (!await _context.Immobilisations.AnyAsync(i => i.IdBien == idBien))
            {
                return NotFound("Le bien spécifié n'existe pas.");
            }

            var amortissements = await _context.Amortissements
                .Where(a => a.IdBien == idBien)
                .OrderBy(a => a.Annee)
                .ToListAsync();

            return amortissements;
        }

        // GET: api/Amortissements/Annee/2023
        [HttpGet("Annee/{annee}")]
        public async Task<ActionResult<IEnumerable<Amortissement>>> GetAmortissementsByAnnee(int annee)
        {
            var amortissements = await _context.Amortissements
                .Include(a => a.Immobilisation)
                .Where(a => a.Annee == annee)
                .ToListAsync();

            return amortissements;
        }

        // DELETE: api/Amortissements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmortissement(int id)
        {
            var amortissement = await _context.Amortissements.FindAsync(id);
            if (amortissement == null)
            {
                return NotFound();
            }

            _context.Amortissements.Remove(amortissement);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}