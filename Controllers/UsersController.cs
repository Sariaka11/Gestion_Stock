using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // GET: api/Users/5/Agence
        [HttpGet("{id}/Agence")]
        public async Task<IActionResult> GetUserAgence(int id)
        {
            try
            {
                // Vérifier si l'utilisateur existe
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé.");
                }

                // Récupérer l'association utilisateur-agence
                var userAgence = await _context.UserAgences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ua => ua.UserId == id);

                if (userAgence == null)
                {
                    return NotFound("Cet utilisateur n'est associé à aucune agence.");
                }

                // Récupérer l'agence séparément
                var agence = await _context.Agences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == userAgence.AgenceId);

                if (agence == null)
                {
                    return NotFound("L'agence associée n'existe pas.");
                }

                // Retourner un objet anonyme simple
                return Ok(new
                {
                    id = agence.Id,
                    numero = agence.Numero,
                    nom = agence.Nom
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // GET: api/Users/5/Fournitures
        [HttpGet("{id}/Fournitures")]
        public async Task<ActionResult<IEnumerable<Fourniture>>> GetUserFournitures(int id)
        {
            try
            {
                // Vérifier si l'utilisateur existe
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé.");
                }

                var userFournitures = await _context.UserFournitures
                    .AsNoTracking()
                    .Where(uf => uf.UserId == id)
                    .ToListAsync();

                if (!userFournitures.Any())
                {
                    return NotFound("Cet utilisateur n'est associé à aucune fourniture.");
                }

                // Récupérer les fournitures séparément
                var fournitureIds = userFournitures.Select(uf => uf.FournitureId).ToList();
                var fournitures = await _context.Fournitures
                    .AsNoTracking()
                    .Where(f => fournitureIds.Contains(f.Id))
                    .ToListAsync();

                if (!fournitures.Any())
                {
                    return NotFound("Aucune fourniture valide n'est associée à cet utilisateur.");
                }

                // Calculer le CMUP pour chaque fourniture
                foreach (var fourniture in fournitures)
                {
                    fourniture.CMUP = CalculerCMUP(fourniture.Id);
                }

                return Ok(fournitures);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            // Vérifier si l'email existe déjà
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return Conflict("Un utilisateur avec cet email existe déjà.");
            }

            // Hasher le mot de passe
            user.MotDePasse = HashPassword(user.MotDePasse);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            // Vérifier si l'email existe déjà pour un autre utilisateur
            if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id))
            {
                return Conflict("Un autre utilisateur avec cet email existe déjà.");
            }

            // Récupérer l'utilisateur existant
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Mettre à jour les propriétés
            existingUser.Nom = user.Nom;
            existingUser.Prenom = user.Prenom;
            existingUser.Email = user.Email;
            existingUser.Fonction = user.Fonction;

            // Ne mettre à jour le mot de passe que s'il a été modifié
            if (!string.IsNullOrEmpty(user.MotDePasse) && user.MotDePasse != existingUser.MotDePasse)
            {
                existingUser.MotDePasse = HashPassword(user.MotDePasse);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Supprimer les associations avec les agences et les fournitures
            var userAgence = await _context.UserAgences.FirstOrDefaultAsync(ua => ua.UserId == id);
            if (userAgence != null)
            {
                _context.UserAgences.Remove(userAgence);
            }

            var userFournitures = await _context.UserFournitures.Where(uf => uf.UserId == id).ToListAsync();
            _context.UserFournitures.RemoveRange(userFournitures);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Users/Login
        [HttpPost("Login")]
        [Consumes("application/json")] // Spécifier explicitement le type de média accepté
        public async Task<ActionResult<User>> Login([FromBody] LoginModel login)
        {
            try
            {
                if (login == null)
                {
                    return BadRequest("Les données de connexion sont requises.");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);

                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé.");
                }

                // Vérifier le mot de passe
                if (!VerifyPassword(login.Password, user.MotDePasse))
                {
                    return Unauthorized("Mot de passe incorrect.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
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

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}