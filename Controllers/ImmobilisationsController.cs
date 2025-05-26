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
                _logger.LogInformation("Récupération de toutes les immobilisations");

                // Utiliser AsNoTracking pour améliorer les performances en lecture seule
                var immobilisations = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Nombre d'immobilisations récupérées: {immobilisations.Count}");

                // Calculer les propriétés dérivées pour chaque immobilisation
                foreach (var immobilisation in immobilisations)
                {
                    await CalculerProprietesDerivees(immobilisation);
                }

                return Ok(immobilisations);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, "Erreur lors de la récupération des immobilisations");
                return StatusCode(500, new { message = "Erreur lors de la récupération des immobilisations", error = ex.Message });
            }
        }

        // GET: api/Immobilisations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Immobilisation>> GetImmobilisation(int id)
        {
            try
            {
                _logger.LogInformation($"Récupération de l'immobilisation avec ID: {id}");

                var immobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .Include(i => i.Amortissements)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                if (immobilisation == null)
                {
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouvée");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouvée" });
                }

                // Calculer les propriétés dérivées
                await CalculerProprietesDerivees(immobilisation);

                return Ok(immobilisation);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, $"Erreur lors de la récupération de l'immobilisation {id}");
                return StatusCode(500, new { message = $"Erreur lors de la récupération de l'immobilisation {id}", error = ex.Message });
            }
        }

        // POST: api/Immobilisations
        [HttpPost]
        public async Task<ActionResult<Immobilisation>> PostImmobilisation(Immobilisation immobilisation)
        {
            try
            {
                _logger.LogInformation("Création d'une nouvelle immobilisation");
                _logger.LogDebug($"Données reçues: {System.Text.Json.JsonSerializer.Serialize(immobilisation)}");

                _context.Immobilisations.Add(immobilisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation créée avec ID: {immobilisation.IdBien}");

                // Récupérer l'immobilisation avec sa catégorie
                var createdImmobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.IdBien == immobilisation.IdBien);

                return CreatedAtAction(nameof(GetImmobilisation), new { id = immobilisation.IdBien }, createdImmobilisation);
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, "Erreur lors de la création de l'immobilisation");
                return StatusCode(500, new { message = "Erreur lors de la création de l'immobilisation", error = ex.Message });
            }
        }

        // PUT: api/Immobilisations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutImmobilisation(int id, Immobilisation immobilisation)
        {
            if (id != immobilisation.IdBien)
            {
                _logger.LogWarning("L'ID dans l'URL ne correspond pas à l'ID dans les données");
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans les données" });
            }

            try
            {
                _logger.LogInformation($"Mise à jour de l'immobilisation avec ID: {id}");
                _logger.LogDebug($"Données reçues: {System.Text.Json.JsonSerializer.Serialize(immobilisation)}");

                _context.Entry(immobilisation).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation avec ID {id} mise à jour avec succès");

                // Récupérer l'immobilisation mise à jour avec sa catégorie
                var updatedImmobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                return Ok(updatedImmobilisation);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ImmobilisationExists(id))
                {
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouvée lors de la mise à jour");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouvée" });
                }
                else
                {
                    _logger.LogError(ex, $"Erreur de concurrence lors de la mise à jour de l'immobilisation {id}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log l'exception
                _logger.LogError(ex, $"Erreur lors de la mise à jour de l'immobilisation {id}");
                return StatusCode(500, new { message = $"Erreur lors de la mise à jour de l'immobilisation {id}", error = ex.Message });
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
                    _logger.LogWarning($"Immobilisation avec ID {id} non trouvée lors de la suppression");
                    return NotFound(new { message = $"Immobilisation avec ID {id} non trouvée" });
                }

                _context.Immobilisations.Remove(immobilisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Immobilisation avec ID {id} supprimée avec succès");
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

        // Méthode pour calculer les propriétés dérivées d'une immobilisation
        private async Task CalculerProprietesDerivees(Immobilisation immobilisation)
        {
            try
            {
                // Charger les amortissements si ce n'est pas déjà fait
                if (immobilisation.Amortissements == null)
                {
                    immobilisation.Amortissements = await _context.Amortissements
                        .Where(a => a.IdBien == immobilisation.IdBien)
                        .OrderBy(a => a.Annee)
                        .ToListAsync();
                }

                // Calculer l'amortissement cumulé
                immobilisation.AmortissementCumule = immobilisation.Amortissements?.Sum(a => a.Montant) ?? 0;

                // Calculer la valeur nette comptable
                immobilisation.ValeurNetteComptable = immobilisation.ValeurAcquisition - immobilisation.AmortissementCumule;

                // Calculer la durée restante et la date de fin d'amortissement
                if (immobilisation.Categorie != null && immobilisation.DateAcquisition.HasValue)
                {
                    int dureeAmortissement = immobilisation.Categorie.DureeAmortissement;
                    DateTime dateAcquisition = immobilisation.DateAcquisition.Value;
                    DateTime dateFin = dateAcquisition.AddYears(dureeAmortissement);

                    immobilisation.DateFinAmortissement = dateFin;

                    // Calculer la durée restante en années
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
                _logger.LogError(ex, $"Erreur lors du calcul des propriétés dérivées pour l'immobilisation {immobilisation.IdBien}");
                // Ne pas propager l'exception pour éviter de bloquer la récupération des données
            }
        }
    }
}
