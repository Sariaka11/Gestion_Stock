using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImmobilisationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImmobilisationsController> _logger;

        public ImmobilisationsController(ApplicationDbContext context, ILogger<ImmobilisationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Immobilisations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Immobilisation>>> GetImmobilisations()
        {
            try
            {
                _logger.LogInformation("R�cup�ration de toutes les immobilisations");

                // Utiliser AsNoTracking pour am�liorer les performances en lecture seule
                var immobilisations = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Nombre d'immobilisations r�cup�r�es: {immobilisations.Count}");

                // Calculer les propri�t�s d�riv�es pour chaque immobilisation
                foreach (var immobilisation in immobilisations)
                {
                    await CalculerProprietesDerivees(immobilisation);
                }

                return Ok(immobilisations);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, "Erreur lors de la r�cup�ration des immobilisations");
                return StatusCode(500, new { message = "Erreur lors de la r�cup�ration des immobilisations", error = ex.Message });
            }
        }

        // GET: api/Immobilisations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Immobilisation>> GetImmobilisation(int id)
        {
            try
            {
                _logger.LogInformation($"R�cup�ration de l'immobilisation avec ID: {id}");

                var immobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .Include(i => i.Amortissements)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                if (immobilisation == null)
                {
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouv�e");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouv�e" });
                }

                // Calculer les propri�t�s d�riv�es
                await CalculerProprietesDerivees(immobilisation);

                return Ok(immobilisation);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, $"Erreur lors de la r�cup�ration de l'immobilisation {id}");
                return StatusCode(500, new { message = $"Erreur lors de la r�cup�ration de l'immobilisation {id}", error = ex.Message });
            }
        }

        // POST: api/Immobilisations
        [HttpPost]
        public async Task<ActionResult<Immobilisation>> PostImmobilisation(Immobilisation immobilisation)
        {
            try
            {
                _logger.LogInformation("Cr�ation d'une nouvelle immobilisation");
                _logger.LogDebug($"Donn�es re�ues: {System.Text.Json.JsonSerializer.Serialize(immobilisation)}");

                _context.Immobilisations.Add(immobilisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation cr��e avec ID: {immobilisation.IdBien}");

                // R�cup�rer l'immobilisation avec sa cat�gorie
                var createdImmobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.IdBien == immobilisation.IdBien);

                return CreatedAtAction(nameof(GetImmobilisation), new { id = immobilisation.IdBien }, createdImmobilisation);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, "Erreur lors de la cr�ation de l'immobilisation");
                return StatusCode(500, new { message = "Erreur lors de la cr�ation de l'immobilisation", error = ex.Message });
            }
        }

        // PUT: api/Immobilisations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutImmobilisation(int id, Immobilisation immobilisation)
        {
            if (id != immobilisation.IdBien)
            {
                _logger.LogWarning("L'ID dans l'URL ne correspond pas � l'ID dans les donn�es");
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas � l'ID dans les donn�es" });
            }

            try
            {
                _logger.LogInformation($"Mise � jour de l'immobilisation avec ID: {id}");
                _logger.LogDebug($"Donn�es re�ues: {System.Text.Json.JsonSerializer.Serialize(immobilisation)}");

                _context.Entry(immobilisation).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation avec ID {id} mise � jour avec succ�s");

                // R�cup�rer l'immobilisation mise � jour avec sa cat�gorie
                var updatedImmobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                return Ok(updatedImmobilisation);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ImmobilisationExists(id))
                {
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouv�e lors de la mise � jour");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouv�e" });
                }
                else
                {
                    _logger.LogError(ex, $"Erreur de concurrence lors de la mise � jour de l'immobilisation {id}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, $"Erreur lors de la mise � jour de l'immobilisation {id}");
                return StatusCode(500, new { message = $"Erreur lors de la mise � jour de l'immobilisation {id}", error = ex.Message });
            }
        }

        // DELETE: api/Immobilisations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImmobilisation(int id)
        {
            try
            {
                _logger.LogInformation($"Suppression de l'immobilisation avec ID: {id}");

                var immobilisation = await _context.Immobilisations.FindAsync(id);
                if (immobilisation == null)
                {
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouv�e lors de la suppression");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouv�e" });
                }

                _context.Immobilisations.Remove(immobilisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation avec ID {id} supprim�e avec succ�s");
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, $"Erreur lors de la suppression de l'immobilisation {id}");
                return StatusCode(500, new { message = $"Erreur lors de la suppression de l'immobilisation {id}", error = ex.Message });
            }
        }

        private bool ImmobilisationExists(int id)
        {
            return _context.Immobilisations.Any(e => e.IdBien == id);
        }

        // M�thode pour calculer les propri�t�s d�riv�es d'une immobilisation
        private async Task CalculerProprietesDerivees(Immobilisation immobilisation)
        {
            try
            {
                // Charger les amortissements si ce n'est pas d�j� fait
                if (immobilisation.Amortissements == null)
                {
                    immobilisation.Amortissements = await _context.Amortissements
                        .Where(a => a.IdBien == immobilisation.IdBien)
                        .OrderBy(a => a.Annee)
                        .ToListAsync();
                }

                // Calculer l'amortissement cumul�
                immobilisation.AmortissementCumule = immobilisation.Amortissements?.Sum(a => a.Montant) ?? 0;

                // Calculer la valeur nette comptable
                immobilisation.ValeurNetteComptable = immobilisation.ValeurAcquisition - immobilisation.AmortissementCumule;

                // Calculer la dur�e restante et la date de fin d'amortissement
                if (immobilisation.Categorie != null && immobilisation.DateAcquisition.HasValue)
                {
                    int dureeAmortissement = immobilisation.Categorie.DureeAmortissement;
                    DateTime dateAcquisition = immobilisation.DateAcquisition.Value;
                    DateTime dateFin = dateAcquisition.AddYears(dureeAmortissement);

                    immobilisation.DateFinAmortissement = dateFin;

                    // Calculer la dur�e restante en ann�es
                    int anneesEcoulees = DateTime.Now.Year - dateAcquisition.Year;
                    if (DateTime.Now.Month < dateAcquisition.Month ||
                        (DateTime.Now.Month == dateAcquisition.Month && DateTime.Now.Day < dateAcquisition.Day))
                    {
                        anneesEcoulees--;
                    }

                    immobilisation.DureeRestante = Math.Max(0, dureeAmortissement - anneesEcoulees);
                }
                else
                {
                    immobilisation.DureeRestante = 0;
                    immobilisation.DateFinAmortissement = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du calcul des propri�t�s d�riv�es pour l'immobilisation {immobilisation.IdBien}");
                // Ne pas propager l'exception pour �viter de bloquer la r�cup�ration des donn�es
            }
        }
    }
}
