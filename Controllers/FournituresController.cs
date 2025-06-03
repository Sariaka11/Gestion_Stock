using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FournituresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FournituresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Fournitures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Fourniture>>> GetFournitures()
        {
            var fournitures = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .Include(f => f.AgenceFournitures)
                .ToListAsync();

            foreach (var fourniture in fournitures)
            {
                CalculerValeurs(fourniture);
                fourniture.CMUP = CalculerCMUP(fourniture.Id);
            }

            return fournitures;
        }

        // GET: api/Fournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Fourniture>> GetFourniture(int id)
        {
            var fourniture = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .Include(f => f.AgenceFournitures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fourniture == null)
            {
                return NotFound();
            }

            CalculerValeurs(fourniture);
            fourniture.CMUP = CalculerCMUP(id);

            return fourniture;
        }

        // POST: api/Fournitures
        [HttpPost]
        public async Task<ActionResult<Fourniture>> PostFourniture(Fourniture fourniture)
        {
            var existingFourniture = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .FirstOrDefaultAsync(f => f.Nom == fourniture.Nom && f.Categorie == fourniture.Categorie);

            if (existingFourniture != null)
            {
                var nouvelleEntree = new EntreeFourniture
                {
                    FournitureId = existingFourniture.Id,
                    QuantiteEntree = fourniture.Quantite,
                    DateEntree = DateTime.Now,
                    PrixUnitaire = fourniture.PrixUnitaire,
                    Montant = fourniture.PrixUnitaire * fourniture.Quantite
                };

                existingFourniture.EntreesFournitures.Add(nouvelleEntree);
                existingFourniture.PrixUnitaire = fourniture.PrixUnitaire;
                existingFourniture.QuantiteRestante += fourniture.Quantite;

                CalculerValeurs(existingFourniture);

                try
                {
                    await _context.SaveChangesAsync();
                    existingFourniture.CMUP = CalculerCMUP(existingFourniture.Id);
                    return Ok(existingFourniture);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FournitureExists(existingFourniture.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                fourniture.QuantiteRestante = fourniture.Quantite;
                var nouvelleEntree = new EntreeFourniture
                {
                    QuantiteEntree = fourniture.Quantite,
                    DateEntree = DateTime.Now,
                    PrixUnitaire = fourniture.PrixUnitaire,
                    Montant = fourniture.PrixUnitaire * fourniture.Quantite
                };

                fourniture.EntreesFournitures = new List<EntreeFourniture> { nouvelleEntree };
                CalculerValeurs(fourniture);

                _context.Fournitures.Add(fourniture);
                await _context.SaveChangesAsync();

                fourniture.CMUP = CalculerCMUP(fourniture.Id);
                return CreatedAtAction(nameof(GetFourniture), new { id = fourniture.Id }, fourniture);
            }
        }

        // PUT: api/Fournitures/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFourniture(int id, Fourniture fourniture)
        {
            if (id != fourniture.Id)
            {
                return BadRequest();
            }

            var existingFourniture = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (existingFourniture == null)
            {
                return NotFound();
            }

            var nouvelleEntree = new EntreeFourniture
            {
                FournitureId = existingFourniture.Id,
                QuantiteEntree = fourniture.Quantite,
                DateEntree = DateTime.Now,
                PrixUnitaire = fourniture.PrixUnitaire,
                Montant = fourniture.PrixUnitaire * fourniture.Quantite
            };

            existingFourniture.EntreesFournitures.Add(nouvelleEntree);
            existingFourniture.PrixUnitaire = fourniture.PrixUnitaire;
            existingFourniture.QuantiteRestante += fourniture.Quantite - existingFourniture.Quantite;
            CalculerValeurs(existingFourniture);

            _context.Entry(existingFourniture).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FournitureExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Fournitures/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFourniture(int id)
        {
            var fourniture = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .Include(f => f.AgenceFournitures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fourniture == null)
            {
                return NotFound();
            }

            _context.EntreeFournitures.RemoveRange(fourniture.EntreesFournitures);
            _context.AgenceFournitures.RemoveRange(fourniture.AgenceFournitures);
            _context.Fournitures.Remove(fourniture);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Fournitures/CMUP/5
        [HttpGet("CMUP/{id}")]
        public ActionResult<decimal> GetCMUP(int id)
        {
            if (!FournitureExists(id))
            {
                return NotFound();
            }

            return CalculerCMUP(id);
        }

        // Nouveau endpoint : POST api/Fournitures/{id}/Entrees
        [HttpPost("{id}/Entrees")]
        public async Task<ActionResult<Fourniture>> PostEntreeFourniture(int id, EntreeFourniture entreeFourniture)
        {
            // Vérifier si la fourniture existe
            var fourniture = await _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fourniture == null)
            {
                return NotFound();
            }

            // Vérifier si l'ID de la fourniture correspond
            if (entreeFourniture.FournitureId != id)
            {
                return BadRequest("L'ID de la fourniture dans EntreeFourniture ne correspond pas à l'ID fourni.");
            }

            // Ajouter l'entrée
            entreeFourniture.Montant = entreeFourniture.QuantiteEntree * entreeFourniture.PrixUnitaire;
            fourniture.EntreesFournitures.Add(entreeFourniture);
            fourniture.PrixUnitaire = entreeFourniture.PrixUnitaire;
            fourniture.QuantiteRestante += entreeFourniture.QuantiteEntree;

            // Recalculer les valeurs
            CalculerValeurs(fourniture);

            try
            {
                await _context.SaveChangesAsync();
                fourniture.CMUP = CalculerCMUP(fourniture.Id);
                return Ok(fourniture);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FournitureExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private decimal CalculerCMUP(int fournitureId)
        {
            var fourniture = _context.Fournitures
                .Include(f => f.EntreesFournitures)
                .FirstOrDefault(f => f.Id == fournitureId);

            if (fourniture == null || !fourniture.EntreesFournitures.Any())
            {
                return 0;
            }

            var sommeProduits = fourniture.EntreesFournitures.Sum(e => e.QuantiteEntree * e.PrixUnitaire);
            var sommeQuantites = fourniture.EntreesFournitures.Sum(e => e.QuantiteEntree);

            return sommeQuantites == 0 ? 0 : sommeProduits / sommeQuantites;
        }

        private void CalculerValeurs(Fourniture fourniture)
        {
            var entrees = fourniture.EntreesFournitures.ToList();
            fourniture.Quantite = entrees.Sum(e => e.QuantiteEntree);
            fourniture.Montant = entrees.Sum(e => e.Montant);
            fourniture.PrixTotal = fourniture.QuantiteRestante * fourniture.PrixUnitaire;
        }

        private bool FournitureExists(int id)
        {
            return _context.Fournitures.Any(e => e.Id == id);
        }
    }
}