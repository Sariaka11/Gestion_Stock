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
                return BadRequest("Donn�es d'entr�e invalides.");
            }

            // V�rifier si la cat�gorie existe
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == createDto.IdCategorie);
            if (categorie == null)
            {
                return BadRequest("La cat�gorie sp�cifi�e n'existe pas.");
            }

            var immobilisation = _mappingService.ToEntity(createDto);
            if (immobilisation == null)
            {
                return BadRequest("Erreur lors du mappage des donn�es.");
            }

            // V�rifier que la date d'acquisition est fournie
            if (!immobilisation.DateAcquisition.HasValue)
            {
                return BadRequest("La date d'acquisition est requise pour calculer l'amortissement.");
            }

            // Calculer le taux d'amortissement � partir de la dur�e de la cat�gorie
            decimal tauxAmortissement = 100m / categorie.DureeAmortissement;
            decimal amortissementAnnuel = immobilisation.ValeurAcquisition * (tauxAmortissement / 100m);
            decimal valeurResiduelle = immobilisation.ValeurAcquisition;

            // G�n�rer les enregistrements d'amortissement
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

            // Calculer les propri�t�s d�riv�es
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
                return BadRequest("Donn�es d'entr�e invalides.");
            }

            var immobilisation = await _context.Immobilisations
                .Include(i => i.Amortissements)
                .FirstOrDefaultAsync(i => i.IdBien == id);

            if (immobilisation == null)
            {
                return NotFound();
            }

            // Mettre � jour l'entit�
            _mappingService.UpdateEntity(immobilisation, updateDto);

            // V�rifier si la cat�gorie existe
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == immobilisation.IdCategorie);
            if (categorie == null)
            {
                return BadRequest("La cat�gorie sp�cifi�e n'existe pas.");
            }

            // Recalculer les amortissements si n�cessaire
            // Puisque ValeurAcquisition est requis, il est toujours pr�sent
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

            // Recalculer les propri�t�s d�riv�es
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
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Vérifier l'existence de l'immobilisation
        var immobilisationExists = await _context.Immobilisations
            .AsNoTracking()
            .AnyAsync(i => i.IdBien == id);
        
        if (!immobilisationExists)
        {
            Console.WriteLine($"Immobilisation non trouvée pour IdBien: {id}");
            return NotFound("Immobilisation non trouvée.");
        }

        // 1. Supprimer les amortissements 
        // Utilisez le vrai nom de la colonne dans Oracle (probablement ID_BIEN au lieu de IdBien)
        var amortissementsCount = await _context.Database
            .ExecuteSqlRawAsync("DELETE FROM AMORTISSEMENTS WHERE ID_BIEN = {0}", id);
        
        Console.WriteLine($"Suppression de {amortissementsCount} amortissements pour IdBien: {id}");

        // 2. Supprimer les affectations
        var affectationsCount = await _context.Database
            .ExecuteSqlRawAsync("DELETE FROM BIEN_AGENCE WHERE ID_BIEN = {0}", id);
        
        Console.WriteLine($"Suppression de {affectationsCount} affectations pour IdBien: {id}");

        // 3. Supprimer l'immobilisation
        var immobilisationCount = await _context.Database
            .ExecuteSqlRawAsync("DELETE FROM IMMOBILISATIONS WHERE ID_BIEN = {0}", id);
        
        if (immobilisationCount == 0)
        {
            await transaction.RollbackAsync();
            return NotFound("Immobilisation non trouvée lors de la suppression.");
        }

        // 4. Confirmer la transaction
        await transaction.CommitAsync();
        
        Console.WriteLine($"Immobilisation supprimée avec succès: IdBien={id}");
        return NoContent();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
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

            // Calculer l'amortissement cumul� et la valeur nette comptable
            immobilisation.AmortissementCumule = amortissements.Sum(a => a.Montant);
            immobilisation.ValeurNetteComptable = immobilisation.ValeurAcquisition - immobilisation.AmortissementCumule;

            // Charger la cat�gorie
            var categorie = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategorie == immobilisation.IdCategorie);

            if (categorie != null && immobilisation.DateAcquisition.HasValue)
            {
                var anneeAcquisition = immobilisation.DateAcquisition.Value.Year;
                var anneeActuelle = DateTime.Now.Year;
                var dureeEcoulee = anneeActuelle - anneeAcquisition;
                immobilisation.DureeRestante = Math.Max(0, categorie.DureeAmortissement - dureeEcoulee);

                immobilisation.DateFinAmortissement = immobilisation.DateAcquisition.Value.AddYears(categorie.DureeAmortissement);

                // D�terminer le statut
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