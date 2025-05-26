using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgenceFournituresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AgenceFournituresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AgenceFournitures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgenceFourniture>>> GetAgenceFournitures()
        {
            return await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .ToListAsync();
        }

        // GET: api/AgenceFournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AgenceFourniture>> GetAgenceFourniture(int id)
        {
            var agenceFourniture = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .FirstOrDefaultAsync(af => af.Id == id);

            if (agenceFourniture == null)
            {
                return NotFound();
            }

            return agenceFourniture;
        }

        // GET: api/AgenceFournitures/ByAgence/5
        [HttpGet("ByAgence/{agenceId}")]
        public async Task<ActionResult<IEnumerable<AgenceFourniture>>> GetByAgence(int agenceId)
        {
            return await _context.AgenceFournitures
                .Include(af => af.Fourniture)
                .Where(af => af.AgenceId == agenceId)
                .ToListAsync();
        }

        // GET: api/AgenceFournitures/ByFourniture/5
        [HttpGet("ByFourniture/{fournitureId}")]
        public async Task<ActionResult<IEnumerable<AgenceFourniture>>> GetByFourniture(int fournitureId)
        {
            return await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Where(af => af.FournitureId == fournitureId)
                .ToListAsync();
        }

        // POST: api/AgenceFournitures
        [HttpPost]
        public async Task<ActionResult<AgenceFourniture>> PostAgenceFourniture(AgenceFourniture agenceFourniture)
        {
            // Vérifier si l'agence existe
            if (!_context.Agences.Any(a => a.Id == agenceFourniture.AgenceId))
            {
                return BadRequest("L'agence spécifiée n'existe pas.");
            }

            // Vérifier si la fourniture existe
            if (!_context.Fournitures.Any(f => f.Id == agenceFourniture.FournitureId))
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            // Vérifier si l'association existe déjà
            if (_context.AgenceFournitures.Any(af => 
                af.AgenceId == agenceFourniture.AgenceId && 
                af.FournitureId == agenceFourniture.FournitureId))
            {
                return Conflict("Cette association existe déjà.");
            }
            agenceFourniture.DateAssociation = DateTime.Now;
            _context.AgenceFournitures.Add(agenceFourniture);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAgenceFourniture), new { id = agenceFourniture.Id }, agenceFourniture);
        }


        // DELETE: api/AgenceFournitures/Agence/1/Fourniture/1
        [HttpDelete("Agence/{agenceId}/Fourniture/{fournitureId}")]
        public async Task<IActionResult> DeleteAgenceFourniture(int agenceId, int fournitureId)
        {
            var agenceFourniture = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af => af.AgenceId == agenceId && af.FournitureId == fournitureId);

            if (agenceFourniture == null)
            {
                return NotFound($"Association entre l'agence {agenceId} et la fourniture {fournitureId} non trouvée.");
            }

            _context.AgenceFournitures.Remove(agenceFourniture);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AgenceFournitureExists(int agenceId, int fournitureId)
        {
            return _context.AgenceFournitures.Any(af => af.AgenceId == agenceId && af.FournitureId == fournitureId);
        }

    }
}