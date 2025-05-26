using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AgencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Agences
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agence>>> GetAgences()
        {
            return await _context.Agences.ToListAsync();
        }
        // GET: api/Agences/5/Biens
        [HttpGet("{id}/Biens")]
        public async Task<ActionResult<IEnumerable<Immobilisation>>> GetAgenceBiens(int id)
        {
            if (!await _context.Agences.AnyAsync(a => a.Id == id))
            {
                return NotFound("L'agence spécifiée n'existe pas.");
            }

            // Récupérer les dernières affectations pour chaque bien dans cette agence
            var dernieresAffectations = await _context.BienAgences
                .Where(ba => ba.IdAgence == id)
                .GroupBy(ba => ba.IdBien)
                .Select(g => g.OrderByDescending(ba => ba.DateAffectation).First())
                .ToListAsync();

            // Récupérer les biens correspondants
            var bienIds = dernieresAffectations.Select(ba => ba.IdBien).ToList();
            var biens = await _context.Immobilisations
                .Include(i => i.Categorie)
                .Where(i => bienIds.Contains(i.IdBien))
                .ToListAsync();

            return biens;
        }

        // GET: api/Agences/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Agence>> GetAgence(int id)
        {
            var agence = await _context.Agences.FindAsync(id);

            if (agence == null)
            {
                return NotFound();
            }

            return agence;
        }

        // GET: api/Agences/5/Fournitures
        [HttpGet("{id}/Fournitures")]
        public async Task<ActionResult<IEnumerable<Fourniture>>> GetAgenceFournitures(int id)
        {
            if (!_context.Agences.Any(a => a.Id == id))
            {
                return NotFound("L'agence spécifiée n'existe pas.");
            }

            var fournitureIds = await _context.AgenceFournitures
                .Where(af => af.AgenceId == id)
                .Select(af => af.FournitureId)
                .ToListAsync();

            var fournitures = await _context.Fournitures
                .Where(f => fournitureIds.Contains(f.Id))
                .ToListAsync();

            // Calculer le CMUP pour chaque fourniture
            foreach (var fourniture in fournitures)
            {
                fourniture.CMUP = CalculerCMUP(fourniture.Id);
            }

            return fournitures;
        }

        // GET: api/Agences/5/Users
        [HttpGet("{id}/Users")]
        public async Task<ActionResult<IEnumerable<User>>> GetAgenceUsers(int id)
        {
            if (!_context.Agences.Any(a => a.Id == id))
            {
                return NotFound("L'agence spécifiée n'existe pas.");
            }

            var userIds = await _context.UserAgences
                .Where(ua => ua.AgenceId == id)
                .Select(ua => ua.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            return users;
        }

        // POST: api/Agences
        [HttpPost]
        public async Task<ActionResult<Agence>> PostAgence(Agence agence)
        {
            // Vérifier si le numéro est vide
            if (string.IsNullOrWhiteSpace(agence.Numero))
            {
                return BadRequest("Le numéro de l'agence ne peut pas être vide.");
            }

            // Vérifier si une agence avec ce numéro existe déjà
            bool numeroExists = await _context.Agences.AnyAsync(a => a.Numero == agence.Numero);
            if (numeroExists)
            {
                return Conflict($"Une agence avec le numéro '{agence.Numero}' existe déjà.");
            }

            _context.Agences.Add(agence);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAgence), new { id = agence.Id }, agence);
        }

        // PUT: api/Agences/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgence(int id, Agence agence)
        {
            if (id != agence.Id)
            {
                return BadRequest();
            }

            // Vérifier si le numéro est vide
            if (string.IsNullOrWhiteSpace(agence.Numero))
            {
                return BadRequest("Le numéro de l'agence ne peut pas être vide.");
            }

            // Vérifier si une autre agence avec ce numéro existe déjà
            bool numeroExists = await _context.Agences
                .AnyAsync(a => a.Numero == agence.Numero && a.Id != id);
                
            if (numeroExists)
            {
                return Conflict($"Une autre agence avec le numéro '{agence.Numero}' existe déjà.");
            }

            _context.Entry(agence).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgenceExists(id))
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

        // DELETE: api/Agences/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgence(int id)
        {
            var agence = await _context.Agences.FindAsync(id);
            if (agence == null)
            {
                return NotFound();
            }

            // Vérifier si des utilisateurs sont associés à cette agence
            var usersCount = await _context.UserAgences.CountAsync(ua => ua.AgenceId == id);
            if (usersCount > 0)
            {
                return BadRequest($"Impossible de supprimer cette agence car {usersCount} utilisateur(s) y sont associés.");
            }

            // Supprimer les associations avec les fournitures
            var associations = await _context.AgenceFournitures
                .Where(af => af.AgenceId == id)
                .ToListAsync();
                
            _context.AgenceFournitures.RemoveRange(associations);
            _context.Agences.Remove(agence);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AgenceExists(int id)
        {
            return _context.Agences.Any(e => e.Id == id);
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
    }
}