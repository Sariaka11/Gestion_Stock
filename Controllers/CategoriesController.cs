using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categorie>>> GetCategories()
        {
            try
            {
                _logger.LogInformation("Récupération de toutes les catégories");

                var categories = await _context.Categories
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Nombre de catégories récupérées: {categories.Count}");

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories");
                return StatusCode(500, new { message = "Erreur lors de la récupération des catégories", error = ex.Message });
            }
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Categorie>> GetCategorie(int id)
        {
            try
            {
                _logger.LogInformation($"Récupération de la catégorie avec ID: {id}");

                var categorie = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCategorie == id);

                if (categorie == null)
                {
                    _logger.LogWarning($"Catégorie avec ID {id} non trouvée");
                    return NotFound(new { message = $"Catégorie avec ID {id} non trouvée" });
                }

                return Ok(categorie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de la catégorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la récupération de la catégorie {id}", error = ex.Message });
            }
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<Categorie>> PostCategorie(Categorie categorie)
        {
            try
            {
                _logger.LogInformation("Création d'une nouvelle catégorie");
                _logger.LogDebug($"Données reçues: {System.Text.Json.JsonSerializer.Serialize(categorie)}");

                _context.Categories.Add(categorie);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Catégorie créée avec ID: {categorie.IdCategorie}");

                return CreatedAtAction(nameof(GetCategorie), new { id = categorie.IdCategorie }, categorie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la catégorie");
                return StatusCode(500, new { message = "Erreur lors de la création de la catégorie", error = ex.Message });
            }
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategorie(int id, Categorie categorie)
        {
            if (id != categorie.IdCategorie)
            {
                _logger.LogWarning("L'ID dans l'URL ne correspond pas à l'ID dans les données");
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans les données" });
            }

            try
            {
                _logger.LogInformation($"Mise à jour de la catégorie avec ID: {id}");
                _logger.LogDebug($"Données reçues: {System.Text.Json.JsonSerializer.Serialize(categorie)}");

                _context.Entry(categorie).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Catégorie avec ID {id} mise à jour avec succès");

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CategorieExists(id))
                {
                    _logger.LogWarning($"Catégorie avec ID {id} non trouvée lors de la mise à jour");
                    return NotFound(new { message = $"Catégorie avec ID {id} non trouvée" });
                }
                else
                {
                    _logger.LogError(ex, $"Erreur de concurrence lors de la mise à jour de la catégorie {id}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la mise à jour de la catégorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la mise à jour de la catégorie {id}", error = ex.Message });
            }
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategorie(int id)
        {
            try
            {
                _logger.LogInformation($"Suppression de la catégorie avec ID: {id}");

                var categorie = await _context.Categories.FindAsync(id);
                if (categorie == null)
                {
                    _logger.LogWarning($"Catégorie avec ID {id} non trouvée lors de la suppression");
                    return NotFound(new { message = $"Catégorie avec ID {id} non trouvée" });
                }

                _context.Categories.Remove(categorie);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Catégorie avec ID {id} supprimée avec succès");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la catégorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la suppression de la catégorie {id}", error = ex.Message });
            }
        }

        private bool CategorieExists(int id)
        {
            return _context.Categories.Any(e => e.IdCategorie == id);
        }
    }
}
