using GestionFournituresAPI.Data;
using GestionFournituresAPI.Dtos;
using GestionFournituresAPI.Models;
using GestionFournituresAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImmobilisationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IImmobilisationMappingService _mappingService;

        public ImmobilisationsController(ApplicationDbContext context, IImmobilisationMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/Immobilisations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImmobilisationDto>>> GetImmobilisations()
        {
            var immobilisations = await _context.Immobilisations
                .Include(i => i.Categorie)
                .Include(i => i.Amortissements)
                .ToListAsync();

            foreach (var immobilisation in immobilisations)
            {
                await CalculerProprietesDerivees(immobilisation);
            }

            return Ok(_mappingService.ToDtoList(immobilisations));
        }

        // GET: api/Immobilisations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ImmobilisationDto>> GetImmobilisation(int id)
        {
            var immobilisation = await _context.Immobilisations
                .Include(i => i.Categorie)
                .Include(i => i.Amortissements)
                .FirstOrDefaultAsync(i => i.IdBien == id);

            if (immobilisation == null)
            {
                return NotFound();
            }

            await CalculerProprietesDerivees(immobilisation);
            return Ok(_mappingService.ToDto(immobilisation));
        }

        // POST: api/Immobilisations
        [HttpPost]
        public async Task<ActionResult<ImmobilisationDto>> PostImmobilisation(ImmobilisationCreateDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Données d'entrée invalides.");
            }

            // Vérifier si la catégorie existe
            if (!await _context.Categories.AnyAsync(c => c.IdCategorie == createDto.IdCategorie))
            {
                return BadRequest("La catégorie spécifiée n'existe pas.");
            }

            var immobilisation = _mappingService.ToEntity(createDto);
            if (immobilisation == null)
            {
                return BadRequest("Erreur lors du mappage des données.");
            }

            await CalculerProprietesDerivees(immobilisation);
            _context.Immobilisations.Add(immobilisation);
            await _context.SaveChangesAsync();

            var immobilisationDto = _mappingService.ToDto(immobilisation);
            return CreatedAtAction(nameof(GetImmobilisation), new { id = immobilisation.IdBien }, immobilisationDto);
        }

        // PUT: api/Immobilisations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutImmobilisation(int id, ImmobilisationUpdateDto updateDto)
        {
            if (updateDto == null)
            {
                return BadRequest("Données d'entrée invalides.");
            }

            var immobilisation = await _context.Immobilisations
                .FirstOrDefaultAsync(i => i.IdBien == id);

            if (immobilisation == null)
            {
                return NotFound();
            }

            _mappingService.UpdateEntity(immobilisation, updateDto);
            await CalculerProprietesDerivees(immobilisation);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ImmobilisationExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Immobilisations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImmobilisation(int id)
        {
            try
            {
                var Immobilisation = await _context.Immobilisations
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                if (Immobilisation == null)
                {
                    Console.WriteLine($"Immobilisation non trouvé pour IdBien: {id}");
                    return NotFound("Immobilisation non trouvé.");
                }

                // Vérifier les affectations associées dans BIEN_AGENCE
                var affectations = await _context.BienAgences
                    .Where(ba => ba.IdBien == id)
                    .ToListAsync();

                if (affectations.Any())
                {
                    // Supprimer les affectations associées
                    Console.WriteLine($"Suppression de {affectations.Count} affectations pour IdBien: {id}");
                    _context.BienAgences.RemoveRange(affectations);
                }

                // Supprimer l'Immobilisation
                _context.Immobilisations.Remove(Immobilisation);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Immobilisation supprimé avec succès: IdBien={id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans DeleteImmobilisation: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Erreur lors de la suppression: {ex.Message}");
            }
        }
    

private async Task CalculerProprietesDerivees(Immobilisation immobilisation)
        {
            if (immobilisation == null)
            {
                return;
            }

            // Calculer les propriétés dérivées
            var amortissements = await _context.Amortissements
                .Where(a => a.IdBien == immobilisation.IdBien)
                .OrderBy(a => a.Annee)
                .ToListAsync();

            immobilisation.AmortissementCumule = amortissements.Sum(a => a.Montant);
            immobilisation.ValeurNetteComptable = immobilisation.ValeurAcquisition - immobilisation.AmortissementCumule;

            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == immobilisation.IdCategorie);

            if (categorie != null && immobilisation.DateAcquisition.HasValue)
            {
                var anneeAcquisition = immobilisation.DateAcquisition.Value.Year;
                var anneeActuelle = DateTime.Now.Year;
                var dureeEcoulee = anneeActuelle - anneeAcquisition;
                immobilisation.DureeRestante = Math.Max(0, categorie.DureeAmortissement - dureeEcoulee);

                if (immobilisation.DureeRestante == 0)
                {
                    immobilisation.DateFinAmortissement = immobilisation.DateAcquisition.Value.AddYears(categorie.DureeAmortissement);
                    immobilisation.Statut = "amorti";
                }
                else
                {
                    immobilisation.DateFinAmortissement = immobilisation.DateAcquisition.Value.AddYears(categorie.DureeAmortissement);
                    immobilisation.Statut = "actif";
                }
            }
            else
            {
                // Gérer le cas où DateAcquisition est null ou catégorie introuvable
                immobilisation.DureeRestante = 0;
                immobilisation.DateFinAmortissement = null;
                immobilisation.Statut = "inconnu";
            }
        }

        private bool ImmobilisationExists(int id)
        {
            return _context.Immobilisations.Any(e => e.IdBien == id);
        }
    }
}