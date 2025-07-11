using GestionFournituresAPI.Data;
using GestionFournituresAPI.Dtos;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAgencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UserAgencesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/UserAgences
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAgenceDto>>> GetUserAgences()
        {
            var userAgences = await _context.UserAgences
                .Include(ua => ua.User)
                .Include(ua => ua.Agence)
                .ToListAsync();
            return _mapper.Map<List<UserAgenceDto>>(userAgences);
        }

        // GET: api/UserAgences/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAgenceDto>> GetUserAgence(int id)
        {
            var userAgence = await _context.UserAgences
                .Include(ua => ua.User)
                .Include(ua => ua.Agence)
                .FirstOrDefaultAsync(ua => ua.Id == id);

            if (userAgence == null)
            {
                return NotFound();
            }

            return _mapper.Map<UserAgenceDto>(userAgence);
        }

        // GET: api/UserAgences/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<UserAgenceDto>> GetByUser(int userId)
        {
            var userAgence = await _context.UserAgences
                .Include(ua => ua.Agence)
                .FirstOrDefaultAsync(ua => ua.UserId == userId);

            if (userAgence == null)
            {
                return NotFound();
            }

            return _mapper.Map<UserAgenceDto>(userAgence);
        }

        // GET: api/UserAgences/ByAgence/5
        [HttpGet("ByAgence/{agenceId}")]
        public async Task<ActionResult<IEnumerable<UserAgenceDto>>> GetByAgence(int agenceId)
        {
            var userAgences = await _context.UserAgences
                .Include(ua => ua.User)
                .Where(ua => ua.AgenceId == agenceId)
                .ToListAsync();
            return _mapper.Map<List<UserAgenceDto>>(userAgences);
        }

         [HttpGet("GetAgenceByUserId/{userId}")]
            public IActionResult GetAgenceByUserId(int userId)
            {
                Console.WriteLine($"Requête reçue pour userId: {userId}");
                var userAgence = _context.UserAgences
                    .Where(ua => ua.UserId == userId)
                    .Select(ua => new { agenceId = ua.AgenceId })
                    .FirstOrDefault();

                if (userAgence == null)
                    return NotFound("Aucune agence trouvée pour cet utilisateur.");

                return Ok(userAgence);
            }

        // POST: api/UserAgences
        [HttpPost]
        public async Task<ActionResult<UserAgenceDto>> PostUserAgence(UserAgence userAgence)
        {
            // Vérifier si l'utilisateur existe
            if (!_context.Users.Any(u => u.Id == userAgence.UserId))
            {
                return BadRequest("L'utilisateur spécifié n'existe pas.");
            }

            // Vérifier si l'agence existe
            if (!_context.Agences.Any(a => a.Id == userAgence.AgenceId))
            {
                return BadRequest("L'agence spécifiée n'existe pas.");
            }

            // Vérifier si l'utilisateur est déjà associé à une agence
            var existingAssociation = await _context.UserAgences
                .FirstOrDefaultAsync(ua => ua.UserId == userAgence.UserId);

            if (existingAssociation != null)
            {
                // Mettre à jour l'association existante
                existingAssociation.AgenceId = userAgence.AgenceId;
                existingAssociation.DateAssociation = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(_mapper.Map<UserAgenceDto>(existingAssociation));
            }

            // Créer une nouvelle association
            _context.UserAgences.Add(userAgence);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserAgence), new { id = userAgence.Id }, _mapper.Map<UserAgenceDto>(userAgence));
        }

        // DELETE: api/UserAgences/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAgence(int id)
        {
            var userAgence = await _context.UserAgences.FindAsync(id);
            if (userAgence == null)
            {
                return NotFound();
            }

            _context.UserAgences.Remove(userAgence);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}