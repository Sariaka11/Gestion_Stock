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
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == createDto.IdCategorie);
            if (categorie == null)
            {
                return BadRequest("La catégorie spécifiée n'existe pas.");
            }

            var immobilisation = _mappingService.ToEntity(createDto);
            if (immobilisation == null)
            {
                return BadRequest("Erreur lors du mappage des données.");
            }

            // Vérifier que la date d'acquisition est fournie
            if (!immobilisation.DateAcquisition.HasValue)
            {
                return BadRequest("La date d'acquisition est requise pour calculer l'amortissement.");
            }

            // Calculer le taux d'amortissement à partir de la durée de la catégorie
            decimal tauxAmortissement = 100m / categorie.DureeAmortissement;
            decimal amortissementAnnuel = immobilisation.ValeurAcquisition * (tauxAmortissement / 100m);
            decimal valeurResiduelle = immobilisation.ValeurAcquisition;

            // Générer les enregistrements d'amortissement
            immobilisation.Amortissements = new List<Amortissement>();
            int anneeDebut = immobilisation.DateAcquisition.Value.Year;
            for (int i = 0; i < categorie.DureeAmortissement; i++)
            {
                valeurResiduelle -= amortissementAnnuel;
                var amortissement = new Amortissement
                {
                    IdBien = immobilisation.IdBien,
                    Annee = anneeDebut + i,
                    Montant = amortissementAnnuel,
                    ValeurResiduelle = Math.Max(0, valeurResiduelle),
                    DateCalcul = DateTime.Now
                };
                immobilisation.Amortissements.Add(amortissement);
            }

            // Calculer les propriétés dérivées
            await CalculerProprietesDerivees(immobilisation);

            // Enregistrer l'immobilisation et les amortissements
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
                .Include(i => i.Amortissements)
                .FirstOrDefaultAsync(i => i.IdBien == id);

            if (immobilisation == null)
            {
                return NotFound();
            }

            // Mettre à jour l'entité
            _mappingService.UpdateEntity(immobilisation, updateDto);

            // Vérifier si la catégorie existe
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == immobilisation.IdCategorie);
            if (categorie == null)
            {
                return BadRequest("La catégorie spécifiée n'existe pas.");
            }

            // Recalculer les amortissements si nécessaire
            // Puisque ValeurAcquisition est requis, il est toujours présent
            if (updateDto.IdCategorie.HasValue || updateDto.DateAcquisition.HasValue)
            {
                // Supprimer les anciens amortissements
                if (immobilisation.Amortissements != null)
                {
                    _context.Amortissements.RemoveRange(immobilisation.Amortissements);
                }

                // Recalculer les nouveaux amortissements
                decimal tauxAmortissement = 100m / categorie.DureeAmortissement;
                decimal amortissementAnnuel = immobilisation.ValeurAcquisition * (tauxAmortissement / 100m);
                decimal valeurResiduelle = immobilisation.ValeurAcquisition;
                immobilisation.Amortissements = new List<Amortissement>();

                if (immobilisation.DateAcquisition.HasValue)
                {
                    int anneeDebut = immobilisation.DateAcquisition.Value.Year;
                    for (int i = 0; i < categorie.DureeAmortissement; i++)
                    {
                        valeurResiduelle -= amortissementAnnuel;
                        var amortissement = new Amortissement
                        {
                            IdBien = immobilisation.IdBien,
                            Annee = anneeDebut + i,
                            Montant = amortissementAnnuel,
                            ValeurResiduelle = Math.Max(0, valeurResiduelle),
                            DateCalcul = DateTime.Now
                        };
                        immobilisation.Amortissements.Add(amortissement);
                    }
                }
            }

            // Recalculer les propriétés dérivées
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
                var immobilisation = await _context.Immobilisations
                    .FirstOrDefaultAsync(i => i.IdBien == id);

                if (immobilisation == null)
                {
                    Console.WriteLine($"Immobilisation non trouvée pour IdBien: {id}");
                    return NotFound("Immobilisation non trouvée.");
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

                // Supprimer l'immobilisation
                _context.Immobilisations.Remove(immobilisation);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Immobilisation supprimée avec succès: IdBien={id}");
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

            // Charger les amortissements
            var amortissements = await _context.Amortissements
                .Where(a => a.IdBien == immobilisation.IdBien)
                .OrderBy(a => a.Annee)
                .ToListAsync();

            // Calculer l'amortissement cumulé et la valeur nette comptable
            immobilisation.AmortissementCumule = amortissements.Sum(a => a.Montant);
            immobilisation.ValeurNetteComptable = immobilisation.ValeurAcquisition - immobilisation.AmortissementCumule;

            // Charger la catégorie
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == immobilisation.IdCategorie);

            if (categorie != null && immobilisation.DateAcquisition.HasValue)
            {
                var anneeAcquisition = immobilisation.DateAcquisition.Value.Year;
                var anneeActuelle = DateTime.Now.Year;
                var dureeEcoulee = anneeActuelle - anneeAcquisition;
                immobilisation.DureeRestante = Math.Max(0, categorie.DureeAmortissement - dureeEcoulee);

                immobilisation.DateFinAmortissement = immobilisation.DateAcquisition.Value.AddYears(categorie.DureeAmortissement);

                // Déterminer le statut
                immobilisation.Statut = immobilisation.DureeRestante == 0 ? "amorti" : "actif";
            }
            else
            {
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