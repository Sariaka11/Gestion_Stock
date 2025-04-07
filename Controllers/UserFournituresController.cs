using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserFournituresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserFournituresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserFournitures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserFourniture>>> GetUserFournitures()
        {
            return await _context.UserFournitures
                .Include(uf => uf.User)
                .Include(uf => uf.Fourniture)
                .ToListAsync();
        }

        // GET: api/UserFournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserFourniture>> GetUserFourniture(int id)
        {
            var userFourniture = await _context.UserFournitures
                .Include(uf => uf.User)
                .Include(uf => uf.Fourniture)
                .FirstOrDefaultAsync(uf => uf.Id == id);

            if (userFourniture == null)
            {
                return NotFound();
            }

            return userFourniture;
        }

        // GET: api/UserFournitures/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<UserFourniture>>> GetByUser(int userId)
        {
            return await _context.UserFournitures
                .Include(uf => uf.Fourniture)
                .Where(uf => uf.UserId == userId)
                .ToListAsync();
        }

        // GET: api/UserFournitures/ByFourniture/5
        [HttpGet("ByFourniture/{fournitureId}")]
        public async Task<ActionResult<IEnumerable<UserFourniture>>> GetByFourniture(int fournitureId)
        {
            return await _context.UserFournitures
                .Include(uf => uf.User)
                .Where(uf => uf.FournitureId == fournitureId)
                .ToListAsync();
        }

        // POST: api/UserFournitures
        [HttpPost]
        public async Task<ActionResult<UserFourniture>> PostUserFourniture(UserFourniture userFourniture)
        {
            // Vérifier si l'utilisateur existe
            if (!_context.Users.Any(u => u.Id == userFourniture.UserId))
            {
                return BadRequest("L'utilisateur spécifié n'existe pas.");
            }

            // Vérifier si la fourniture existe
            if (!_context.Fournitures.Any(f => f.Id == userFourniture.FournitureId))
            {
                return BadRequest("La fourniture spécifiée n'existe pas.");
            }

            // Vérifier si l'association existe déjà
            var existingAssociation = await _context.UserFournitures
                .FirstOrDefaultAsync(uf => 
                    uf.UserId == userFourniture.UserId && 
                    uf.FournitureId == userFourniture.FournitureId);

            if (existingAssociation != null)
            {
                return Conflict("Cette association existe déjà.");
            }

            _context.UserFournitures.Add(userFourniture);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserFourniture), new { id = userFourniture.Id }, userFourniture);
        }

        // DELETE: api/UserFournitures/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserFourniture(int id)
        {
            var userFourniture = await _context.UserFournitures.FindAsync(id);
            if (userFourniture == null)
            {
                return NotFound();
            }

            _context.UserFournitures.Remove(userFourniture);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}