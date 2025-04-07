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
public async Task<ActionResult<Agence>> GetUserAgence(int id)
{
    try
    {
        // Vérifier si l'utilisateur existe
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Utilisateur avec ID {id} non trouvé.");
        }

        // Récupérer l'association utilisateur-agence
        var userAgence = await _context.UserAgences
            .Include(ua => ua.Agence)
            .FirstOrDefaultAsync(ua => ua.UserId == id);

        if (userAgence == null)
        {
            return NotFound($"Utilisateur avec ID {id} n'est associé à aucune agence.");
        }

        if (userAgence.Agence == null)
        {
            return NotFound($"L'agence associée à l'utilisateur avec ID {id} est introuvable.");
        }

        // Retourner l'agence
        return Ok(userAgence.Agence);
    }
    catch (Exception ex)
    {
        // Log l'exception
        Console.WriteLine($"Erreur dans GetUserAgence: {ex.Message}");
        return StatusCode(500, "Une erreur interne s'est produite.");
    }
}
        // GET: api/Users/5/Fournitures
        [HttpGet("{id}/Fournitures")]
        public async Task<ActionResult<IEnumerable<Fourniture>>> GetUserFournitures(int id)
        {
            var userFournitures = await _context.UserFournitures
                .Include(uf => uf.Fourniture)
                .Where(uf => uf.UserId == id)
                .ToListAsync();

            if (!userFournitures.Any())
            {
                return NotFound("Cet utilisateur n'est associé à aucune fourniture.");
            }

            // Filtrer les fournitures null et les convertir en liste
            var fournitures = userFournitures
                .Where(uf => uf.Fourniture != null)
                .Select(uf => uf.Fourniture!)
                .ToList();

            if (!fournitures.Any())
            {
                return NotFound("Aucune fourniture valide n'est associée à cet utilisateur.");
            }

            // Calculer le CMUP pour chaque fourniture
            foreach (var fourniture in fournitures)
            {
                fourniture.CMUP = CalculerCMUP(fourniture.Id);
            }

            return fournitures;
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
        public async Task<ActionResult<User>> Login(LoginModel login)
        {
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

            return user;
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