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

        // GET: api/AgenceFournitures/ByAgence/5
        [HttpGet("ByAgence/{agenceId}")]
        public async Task<ActionResult<IEnumerable<AgenceFournitureDto>>> GetAgenceFournitures(int agenceId)
        {
            var agenceFournitures = await _context.AgenceFournitures
                .Where(af => af.AgenceId == agenceId)
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
                    DateAssociation = af.DateAssociation,
                    ConsoMm = af.ConsoMm
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
                    af.DateAssociation,
                    af.ConsoMm
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
                DateAssociation = agenceFourniture.DateAssociation,
                ConsoMm = agenceFourniture.ConsoMm
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
                    af.DateAssociation,
                    af.ConsoMm
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
                DateAssociation = af.DateAssociation,
                ConsoMm = af.ConsoMm
            }).ToList();

            return Ok(result);
        }

        // GET: api/AgenceFournitures
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
                    DateAssociation = af.DateAssociation,
                    ConsoMm = af.ConsoMm
                })
                .ToListAsync();

            return Ok(agenceFournitures);
        }

        // POST: api/AgenceFournitures
        [HttpPost]
        public async Task<ActionResult<AgenceFournitureDto>> PostAgenceFourniture(AgenceFourniture agenceFourniture)
        {
            if (!_context.Agences.Any(a => a.Id == agenceFourniture.AgenceId))
            {
                return BadRequest("L'agence spécifiée n'existe pas.");
            }

            var fourniture = await _context.Fournitures
                .FirstOrDefaultAsync(f => f.Id == agenceFourniture.FournitureId);
            if (fourniture == null)
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            if (fourniture.QuantiteRestante < agenceFourniture.Quantite)
            {
                return BadRequest("La quantité demandée dépasse le stock restant.");
            }

            var existing = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af =>
                    af.AgenceId == agenceFourniture.AgenceId &&
                    af.FournitureId == agenceFourniture.FournitureId);

            int resultId;
            if (existing != null)
            {
                if (fourniture.QuantiteRestante < (existing.Quantite + agenceFourniture.Quantite))
                {
                    return BadRequest("La quantité totale dépasse le stock restant.");
                }

                existing.Quantite += agenceFourniture.Quantite;
                existing.DateAssociation = DateTime.Now;
                _context.AgenceFournitures.Update(existing);
                resultId = existing.Id;

                fourniture.QuantiteRestante -= agenceFourniture.Quantite;
                _context.Fournitures.Update(fourniture);
            }
            else
            {
                agenceFourniture.DateAssociation = DateTime.Now;
                _context.AgenceFournitures.Add(agenceFourniture);
                resultId = agenceFourniture.Id;

                fourniture.QuantiteRestante -= agenceFourniture.Quantite;
                _context.Fournitures.Update(fourniture);
            }

            await _context.SaveChangesAsync();

            var result = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.Id == resultId)
                .Select(af => new AgenceFournitureDto
                {
                    Id = af.Id,
                    AgenceId = af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : "Agence inconnue",
                    FournitureId = af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : "Fourniture inconnue",
                    Categorie = af.Fourniture != null ? af.Fourniture.Categorie : "Non catégorisé",
                    Quantite = af.Quantite,
                    DateAssociation = af.DateAssociation,
                    ConsoMm = af.ConsoMm
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("Erreur lors de la récupération de l'association créée.");
            }

            return CreatedAtAction(nameof(GetAgenceFourniture), new { id = result.Id }, result);
        }

        // POST: api/AgenceFournitures/Consommation
        [HttpPost("Consommation")]
        public async Task<ActionResult<AgenceFournitureDto>> CreateConsommation([FromBody] ConsommationCreateDto createDto)
        {
            if (!await _context.Agences.AnyAsync(a => a.Id == createDto.AgenceId))
            {
                return BadRequest("L'agence spécifiée n'existe pas.");
            }

            var fourniture = await _context.Fournitures
                .FirstOrDefaultAsync(f => f.Id == createDto.FournitureId);
            if (fourniture == null)
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            if (!createDto.ConsoMm.HasValue || createDto.ConsoMm <= 0)
            {
                return BadRequest("La consommation doit être un nombre positif.");
            }

            var existing = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af => af.AgenceId == createDto.AgenceId && af.FournitureId == createDto.FournitureId);

            int resultId;
            if (existing != null)
            {
                if (existing.Quantite < createDto.ConsoMm)
                {
                    return BadRequest("La consommation dépasse la quantité disponible dans l'agence.");
                }
                existing.Quantite = Math.Max(0, existing.Quantite - (int)createDto.ConsoMm.Value);
                existing.ConsoMm = createDto.ConsoMm;
                existing.DateAssociation = DateTime.Now;
                _context.AgenceFournitures.Update(existing);
                resultId = existing.Id;
            }
            else
            {
                var agenceFourniture = new AgenceFourniture
                {
                    AgenceId = createDto.AgenceId,
                    FournitureId = createDto.FournitureId,
                    Quantite = 0,
                    ConsoMm = createDto.ConsoMm,
                    DateAssociation = DateTime.Now
                };
                _context.AgenceFournitures.Add(agenceFourniture);
                resultId = agenceFourniture.Id;
            }

            await _context.SaveChangesAsync();

            var result = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.Id == resultId)
                .Select(af => new AgenceFournitureDto
                {
                    Id = af.Id,
                    AgenceId = af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : "Agence inconnue",
                    FournitureId = af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : "Fourniture inconnue",
                    Categorie = af.Fourniture != null ? af.Fourniture.Categorie : "Non catégorisé",
                    Quantite = af.Quantite,
                    DateAssociation = af.DateAssociation,
                    ConsoMm = af.ConsoMm
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("Erreur lors de la récupération de la consommation créée.");
            }

            return CreatedAtAction(nameof(GetAgenceFourniture), new { id = result.Id }, result);
        }

        // POST: api/AgenceFournitures/Consommation/Add
        [HttpPost("Consommation/Add")]
        public async Task<ActionResult<AgenceFournitureDto>> AddConsommation([FromBody] ConsommationCreateDto createDto)
        {
            var existing = await _context.AgenceFournitures
                .FirstOrDefaultAsync(af => af.AgenceId == createDto.AgenceId && af.FournitureId == createDto.FournitureId);
            if (existing == null)
            {
                return NotFound("Aucune association existante pour cette agence et fourniture.");
            }

            var fourniture = await _context.Fournitures
                .FirstOrDefaultAsync(f => f.Id == createDto.FournitureId);
            if (fourniture == null)
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            if (!createDto.ConsoMm.HasValue || createDto.ConsoMm <= 0)
            {
                return BadRequest("La consommation doit être un nombre positif.");
            }

            if (existing.Quantite < createDto.ConsoMm)
            {
                return BadRequest("La consommation dépasse la quantité disponible dans l'agence.");
            }

            existing.Quantite = Math.Max(0, existing.Quantite - (int)createDto.ConsoMm.Value);
            existing.ConsoMm = (existing.ConsoMm ?? 0) + createDto.ConsoMm.Value;
            existing.DateAssociation = DateTime.Now;
            _context.AgenceFournitures.Update(existing);

            await _context.SaveChangesAsync();

            var result = await _context.AgenceFournitures
                .Include(af => af.Agence)
                .Include(af => af.Fourniture)
                .Where(af => af.Id == existing.Id)
                .Select(af => new AgenceFournitureDto
                {
                    Id = af.Id,
                    AgenceId = af.AgenceId,
                    AgenceNom = af.Agence != null ? af.Agence.Nom : "Agence inconnue",
                    FournitureId = af.FournitureId,
                    FournitureNom = af.Fourniture != null ? af.Fourniture.Nom : "Fourniture inconnue",
                    Categorie = af.Fourniture != null ? af.Fourniture.Categorie : "Non catégorisé",
                    Quantite = af.Quantite,
                    DateAssociation = af.DateAssociation,
                    ConsoMm = af.ConsoMm
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("Erreur lors de la récupération de la consommation mise à jour.");
            }

            return Ok(result);
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

    public class AgenceFournitureDto
    {
        public int Id { get; set; }
        public int AgenceId { get; set; }
        public string? AgenceNom { get; set; }
        public int FournitureId { get; set; }
        public string? FournitureNom { get; set; }
        public string? Categorie { get; set; }
        public int Quantite { get; set; }
        public DateTime DateAssociation { get; set; }
        public decimal? ConsoMm { get; set; }
    }

    public class ConsommationCreateDto
    {
        public int AgenceId { get; set; }
        public int FournitureId { get; set; }
        public decimal? ConsoMm { get; set; }
    }

    public class AgenceFournitureUpdateDto
    {
        public int Quantite { get; set; }
    }
}