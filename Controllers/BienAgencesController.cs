using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BienAgencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BienAgencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/BienAgences
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BienAgence>>> GetBienAgences()
        {
            return await _context.BienAgences
                .Include(ba => ba.Immobilisation)
                .Include(ba => ba.Agence)
                .ToListAsync();
        }

        // GET: api/BienAgences/Bien/5/Agence/3
        [HttpGet("Bien/{idBien}/Agence/{idAgence}")]
        public async Task<ActionResult<IEnumerable<BienAgence>>> GetBienAgence(int idBien, int idAgence)
        {
            var bienAgences = await _context.BienAgences
                .Include(ba => ba.Immobilisation)
                .Include(ba => ba.Agence)
                .Where(ba => ba.IdBien == idBien && ba.IdAgence == idAgence)
                .OrderByDescending(ba => ba.DateAffectation)
                .ToListAsync();

            if (bienAgences == null || !bienAgences.Any())
            {
                return NotFound();
            }

            return bienAgences;
        }

        // POST: api/BienAgences
        [HttpPost]
        public async Task<ActionResult<BienAgence>> PostBienAgence(BienAgence bienAgence)
        {
            // Vérifier si le bien existe
            if (!await _context.Immobilisations.AnyAsync(i => i.IdBien == bienAgence.IdBien))
            {
                return BadRequest("Le bien spécifié n'existe pas.");
            }

            // Vérifier si l'agence existe
            if (!await _context.Agences.AnyAsync(a => a.Id == bienAgence.IdAgence))
            {
                return BadRequest($"L'agence avec l'ID {bienAgence.IdAgence} n'existe pas.");
            }

            // Définir la date d'affectation à aujourd'hui si non spécifiée
            if (bienAgence.DateAffectation == default)
            {
                bienAgence.DateAffectation = DateTime.Now;
            }

            // Vérifier si une affectation identique existe déjà
            if (await _context.BienAgences.AnyAsync(ba =>
                ba.IdBien == bienAgence.IdBien &&
                ba.IdAgence == bienAgence.IdAgence &&
                ba.DateAffectation == bienAgence.DateAffectation))
            {
                return Conflict("Cette affectation existe déjà.");
            }

            _context.BienAgences.Add(bienAgence);
            await _context.SaveChangesAsync();

            // Récupérer l'affectation avec les relations
            var affectationCreee = await _context.BienAgences
                .Include(ba => ba.Immobilisation)
                .Include(ba => ba.Agence)
                .FirstOrDefaultAsync(ba =>
                    ba.IdBien == bienAgence.IdBien &&
                    ba.IdAgence == bienAgence.IdAgence &&
                    ba.DateAffectation == bienAgence.DateAffectation);

            return CreatedAtAction("GetBienAgence",
                new { idBien = bienAgence.IdBien, idAgence = bienAgence.IdAgence },
                affectationCreee);
        }

        // DELETE: api/BienAgences/Bien/5/Agence/3/Date/2023-04-04
        [HttpDelete("Bien/{idBien}/Agence/{idAgence}/Date/{date}")]
        public async Task<IActionResult> DeleteBienAgence(int idBien, int idAgence, DateTime date)
        {
            var bienAgence = await _context.BienAgences
                .FirstOrDefaultAsync(ba =>
                    ba.IdBien == idBien &&
                    ba.IdAgence == idAgence &&
                    ba.DateAffectation == date);

            if (bienAgence == null)
            {
                return NotFound();
            }

            _context.BienAgences.Remove(bienAgence);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}