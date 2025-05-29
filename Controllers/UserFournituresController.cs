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
    public class UserFournituresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UserFournituresController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/UserFournitures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserFournitureDto>>> GetUserFournitures()
        {
            var userFournitures = await _context.UserFournitures
                .Include(uf => uf.User)
                .Include(uf => uf.Fourniture)
                .ToListAsync();
            return _mapper.Map<List<UserFournitureDto>>(userFournitures);
        }

        // GET: api/UserFournitures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserFournitureDto>> GetUserFourniture(int id)
        {
            var userFourniture = await _context.UserFournitures
                .Include(uf => uf.User)
                .Include(uf => uf.Fourniture)
                .FirstOrDefaultAsync(uf => uf.Id == id);

            if (userFourniture == null)
            {
                return NotFound();
            }

            return _mapper.Map<UserFournitureDto>(userFourniture);
        }

        // GET: api/UserFournitures/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<UserFournitureDto>>> GetByUser(int userId)
        {
            var userFournitures = await _context.UserFournitures
                .Include(uf => uf.Fourniture)
                .Where(uf => uf.UserId == userId)
                .ToListAsync();
            return _mapper.Map<List<UserFournitureDto>>(userFournitures);
        }

        // GET: api/UserFournitures/ByFourniture/5
        [HttpGet("ByFourniture/{fournitureId}")]
        public async Task<ActionResult<IEnumerable<UserFournitureDto>>> GetByFourniture(int fournitureId)
        {
            var userFournitures = await _context.UserFournitures
                .Include(uf => uf.User)
                .Where(uf => uf.FournitureId == fournitureId)
                .ToListAsync();
            return _mapper.Map<List<UserFournitureDto>>(userFournitures);
        }

        // POST: api/UserFournitures
        [HttpPost]
        public async Task<ActionResult<UserFournitureDto>> PostUserFourniture(UserFourniture userFourniture)
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

            // Récupérer l'association avec les relations pour la réponse
            var createdAssociation = await _context.UserFournitures
                .Include(uf => uf.User)
                .Include(uf => uf.Fourniture)
                .FirstOrDefaultAsync(uf => uf.Id == userFourniture.Id);

            return CreatedAtAction(nameof(GetUserFourniture), new { id = userFourniture.Id }, _mapper.Map<UserFournitureDto>(createdAssociation));
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