using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using GestionFournituresAPI.Dtos;

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

        // GET: api/AgenceFournitures/ByAgence/5 (filtré par agenceId)
        [HttpGet("ByAgence/{agenceId}")]
        public async Task<ActionResult<IEnumerable<AgenceFournitureDto>>> GetAgenceFournitures(int agenceId)
        {
            var agenceFournitures = await _context.AgenceFournitures
                .Where(af => af.AgenceId == agenceId) // Filtrer par agenceId
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Select(af => new AgenceFournitureDto
                {
                    Id = af.Id,
                    AgenceId = af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : "Agence inconnue",
                    FournitureId = af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : "Fourniture inconnue",
                    Categorie = af.Fourniture != null ? af.Fourniture.Categorie : "Non catégorisé",
                    Quantite = af.Quantite,
                    DateAssociation = af.DateAssociation
                })
                .ToListAsync();

            if (agenceFournitures == null || !agenceFournitures.Any())
                return NotFound("Aucune fourniture trouvée pour cette agence.");

            return Ok(agenceFournitures);
        }

        // GET: api/AgenceFournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AgenceFournitureDto>> GetAgenceFourniture(int id)
        {
            var agenceFourniture = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Select(af => new
                {
                    af.Id,
                    af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : null,
                    af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : null,
                    af.Quantite,
                    af.DateAssociation
                })
                .FirstOrDefaultAsync(af => af.Id == id);

            if (agenceFourniture == null)
            {
                return NotFound();
            }

            var result = new AgenceFournitureDto
            {
                Id = agenceFourniture.Id,
                AgenceId = agenceFourniture.AgenceId,
                AgenceNom = agenceFourniture.AgenceNom ?? "Agence inconnue",
                FournitureId = agenceFourniture.FournitureId,
                FournitureNom = agenceFourniture.FournitureNom ?? "Fourniture inconnue",
                Quantite = agenceFourniture.Quantite,
                DateAssociation = agenceFourniture.DateAssociation
            };

            return Ok(result);
        }

        // GET: api/AgenceFournitures/ByFourniture/5
        [HttpGet("ByFourniture/{fournitureId}")]
        public async Task<ActionResult<IEnumerable<AgenceFournitureDto>>> GetByFourniture(int fournitureId)
        {
            var agenceFournitures = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.FournitureId == fournitureId)
                .Select(af => new
                {
                    af.Id,
                    af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : null,
                    af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : null,
                    af.Quantite,
                    af.DateAssociation
                })
                .ToListAsync();

            var result = agenceFournitures.Select(af => new AgenceFournitureDto
            {
                Id = af.Id,
                AgenceId = af.AgenceId,
                AgenceNom = af.AgenceNom ?? "Agence inconnue",
                FournitureId = af.FournitureId,
                FournitureNom = af.FournitureNom ?? "Fourniture inconnue",
                Quantite = af.Quantite,
                DateAssociation = af.DateAssociation
            }).ToList();

            return Ok(result);
        }

        // Controllers/AgenceFournituresController.cs
[HttpGet]
public async Task<ActionResult<IEnumerable<AgenceFournitureDto>>> GetAgenceFournitures()
{
    var agenceFournitures = await _context.AgenceFournitures
        .Include(af => af.Agence)
        .Include(af => af.Fourniture)
        .Select(af => new AgenceFournitureDto
        {
            Id = af.Id,
            AgenceId = af.AgenceId,
            AgenceNom = af.Agence != null ? af.Agence.Nom : "Agence inconnue",
            FournitureId = af.FournitureId,
            FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : "Fourniture inconnue",
            Categorie = af.Fourniture != null ? af.Fourniture.Categorie : "Non catégorisé",
            Quantite = af.Quantite,
            DateAssociation = af.DateAssociation
        })
        .ToListAsync();

    return Ok(agenceFournitures);
}

        // POST: api/AgenceFournitures
        [HttpPost]
        public async Task<ActionResult<AgenceFournitureDto>> PostAgenceFourniture(AgenceFourniture agenceFourniture)
        {
            // Vérifier si l'agence existe
            if (!_context.Agences.Any(a => a.Id == agenceFourniture.AgenceId))
            {
                return BadRequest("L'agence spécifiée n'existe pas.");
            }

            // Vérifier si la fourniture existe
            var fourniture = await _context.Fournitures
                .FirstOrDefaultAsync(f => f.Id == agenceFourniture.FournitureId);
            if (fourniture == null)
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            // Vérifier si la quantité est suffisante
            if (fourniture.QuantiteRestante < agenceFourniture.Quantite)
            {
                return BadRequest("La quantité demandée dépasse le stock restant.");
            }

            // Vérifier si l'association existe déjà
            var existing = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af =>
                    af.AgenceId == agenceFourniture.AgenceId &&
                    af.FournitureId == agenceFourniture.FournitureId);

            int resultId;
            if (existing != null)
            {
                // Vérifier si la quantité mise à jour dépasse le stock restant
                if (fourniture.QuantiteRestante < (existing.Quantite + agenceFourniture.Quantite))
                {
                    return BadRequest("La quantité totale dépasse le stock restant.");
                }

                // Mettre à jour la quantité existante
                existing.Quantite += agenceFourniture.Quantite;
                existing.DateAssociation = DateTime.Now;
                _context.AgenceFournitures.Update(existing);
                resultId = existing.Id;

                // Mettre à jour la quantité restante
                fourniture.QuantiteRestante -= agenceFourniture.Quantite;
                _context.Fournitures.Update(fourniture);
            }
            else
            {
                // Créer une nouvelle association
                agenceFourniture.DateAssociation = DateTime.Now;
                _context.AgenceFournitures.Add(agenceFourniture);
                resultId = agenceFourniture.Id;

                // Mettre à jour la quantité restante
                fourniture.QuantiteRestante -= agenceFourniture.Quantite;
                _context.Fournitures.Update(fourniture);
            }

            await _context.SaveChangesAsync();

            // Retourner le DTO de l'association créée ou mise à jour
            var result = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.Id == resultId)
                .Select(af => new
                {
                    af.Id,
                    af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : null,
                    af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : null,
                    af.Quantite,
                    af.DateAssociation
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("Erreur lors de la récupération de l'association créée.");
            }

            var resultDto = new AgenceFournitureDto
            {
                Id = result.Id,
                AgenceId = result.AgenceId,
                AgenceNom = result.AgenceNom ?? "Agence inconnue",
                FournitureId = result.FournitureId,
                FournitureNom = result.FournitureNom ?? "Fourniture inconnue",
                Quantite = result.Quantite,
                DateAssociation = result.DateAssociation
            };

            return CreatedAtAction(nameof(GetAgenceFourniture), new { id = resultDto.Id }, resultDto);
        }

        // PUT: api/AgenceFournitures/Agence/1/Fourniture/1
        [HttpPut("Agence/{agenceId}/Fourniture/{fournitureId}")]
        public async Task<ActionResult<AgenceFournitureDto>> UpdateAgenceFourniture(int agenceId, int fournitureId, [FromBody] AgenceFournitureUpdateDto updateDto)
        {
            var agenceFourniture = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af => af.AgenceId == agenceId && af.FournitureId == fournitureId);

            if (agenceFourniture == null)
            {
                return NotFound($"Association entre l'agence {agenceId} et la fourniture {fournitureId} non trouvée.");
            }

            // Mettre à jour la quantité
            agenceFourniture.Quantite = updateDto.Quantite;
            agenceFourniture.DateAssociation = DateTime.Now;
            _context.AgenceFournitures.Update(agenceFourniture);
            await _context.SaveChangesAsync();

            // Retourner le DTO mis à jour
            var result = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.Id == agenceFourniture.Id)
                .Select(af => new
                {
                    af.Id,
                    af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : null,
                    af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : null,
                    af.Quantite,
                    af.DateAssociation
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("Erreur lors de la récupération de l'association mise à jour.");
            }

            var resultDto = new AgenceFournitureDto
            {
                Id = result.Id,
                AgenceId = result.AgenceId,
                AgenceNom = result.AgenceNom ?? "Agence inconnue",
                FournitureId = result.FournitureId,
                FournitureNom = result.FournitureNom ?? "Fourniture inconnue",
                Quantite = result.Quantite,
                DateAssociation = result.DateAssociation
            };

            return Ok(resultDto);
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

    // DTO pour la mise à jour
    public class AgenceFournitureUpdateDto
    {
        public int Quantite { get; set; }
    }
}