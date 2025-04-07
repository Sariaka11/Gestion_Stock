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
            var fournitures = await _context.Fournitures.ToListAsync();
            
            // Calculer le CMUP pour chaque fourniture
            foreach (var fourniture in fournitures)
            {
                fourniture.CMUP = CalculerCMUP(fourniture.Id);
            }
            
            return fournitures;
        }

        // GET: api/Fournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Fourniture>> GetFourniture(int id)
        {
            var fourniture = await _context.Fournitures.FindAsync(id);

            if (fourniture == null)
            {
                return NotFound();
            }

            // Calculer le CMUP pour cette fourniture
            fourniture.CMUP = CalculerCMUP(id);

            return fourniture;
        }

        // POST: api/Fournitures
        [HttpPost]
        public async Task<ActionResult<Fourniture>> PostFourniture(Fourniture fourniture)
        {
            // Vérifier si une fourniture avec le même nom existe déjà
            var existingFourniture = await _context.Fournitures
                .FirstOrDefaultAsync(f => f.Nom == fourniture.Nom);

            if (existingFourniture != null)
            {
                // Mettre à jour la fourniture existante
                existingFourniture.PrixUnitaire = fourniture.PrixUnitaire;
                existingFourniture.Categorie = fourniture.Categorie;
                
                // Ajouter la nouvelle quantité à la quantité restante existante
                existingFourniture.QuantiteRestante += fourniture.Quantite;
                
                // Mettre à jour la quantité totale
                existingFourniture.Quantite += fourniture.Quantite;
                
                // Recalculer les valeurs
                existingFourniture.Montant = existingFourniture.PrixUnitaire * existingFourniture.Quantite;
                existingFourniture.PrixTotal = existingFourniture.QuantiteRestante * existingFourniture.PrixUnitaire;
                
                // Mettre à jour la date
                existingFourniture.Date = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                    
                    // Calculer le CMUP pour cette fourniture
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
                // C'est une nouvelle fourniture, calculer les valeurs
                fourniture.Date = DateTime.Now;
                CalculerValeurs(fourniture, true);

                _context.Fournitures.Add(fourniture);
                await _context.SaveChangesAsync();

                // Calculer le CMUP pour cette fourniture
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

            // Récupérer la fourniture existante pour les calculs
            var existingFourniture = await _context.Fournitures.FindAsync(id);
            if (existingFourniture == null)
            {
                return NotFound();
            }

            // Mettre à jour la quantité restante en fonction de la nouvelle quantité
            fourniture.QuantiteRestante = existingFourniture.QuantiteRestante + (fourniture.Quantite - existingFourniture.Quantite);
            
            // Recalculer les valeurs
            CalculerValeurs(fourniture, false);

            _context.Entry(existingFourniture).State = EntityState.Detached;
            _context.Entry(fourniture).State = EntityState.Modified;

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
            var fourniture = await _context.Fournitures.FindAsync(id);
            if (fourniture == null)
            {
                return NotFound();
            }

            // Supprimer également les associations avec les agences
            var associations = await _context.AgenceFournitures
                .Where(af => af.FournitureId == id)
                .ToListAsync();
                
            _context.AgenceFournitures.RemoveRange(associations);
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

        // Méthode pour calculer le CMUP (Coût Moyen Unitaire Pondéré)
        private decimal CalculerCMUP(int fournitureId)
        {
            // Récupérer toutes les fournitures du même type (même nom)
            var fourniture = _context.Fournitures.Find(fournitureId);
            if (fourniture == null)
            {
                return 0;
            }

            var fournituresMemeType = _context.Fournitures
                .Where(f => f.Nom == fourniture.Nom && f.Categorie == fourniture.Categorie)
                .ToList();

            if (!fournituresMemeType.Any() || fournituresMemeType.Sum(f => f.QuantiteRestante) == 0)
            {
                return 0;
            }

            // Calculer le CMUP: Somme(Quantité * Prix Unitaire) / Somme(Quantité)
            decimal sommeProduits = fournituresMemeType.Sum(f => f.QuantiteRestante * f.PrixUnitaire);
            int sommeQuantites = fournituresMemeType.Sum(f => f.QuantiteRestante);

            return sommeProduits / sommeQuantites;
        }

        private void CalculerValeurs(Fourniture fourniture, bool isNew)
        {
            // Calcul du montant: prix_unitaire * quantité
            fourniture.Montant = fourniture.PrixUnitaire * fourniture.Quantite;

            // Si c'est une nouvelle fourniture, initialiser la quantité restante
            if (isNew)
            {
                fourniture.QuantiteRestante = fourniture.Quantite;
            }

            // Calcul du prix total: quantité_restante * prix_unitaire
            fourniture.PrixTotal = fourniture.QuantiteRestante * fourniture.PrixUnitaire;
        }

        private bool FournitureExists(int id)
        {
            return _context.Fournitures.Any(e => e.Id == id);
        }
    }
}