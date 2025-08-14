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
                _logger.LogInformation("R�cup�ration de toutes les cat�gories");

                var categories = await _context.Categories
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Nombre de cat�gories r�cup�r�es: {categories.Count}");

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la r�cup�ration des cat�gories");
                return StatusCode(500, new { message = "Erreur lors de la r�cup�ration des cat�gories", error = ex.Message });
            }
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Categorie>> GetCategorie(int id)
        {
            try
            {
                _logger.LogInformation($"R�cup�ration de la cat�gorie avec ID: {id}");

                var categorie = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCategorie == id);

                if (categorie == null)
                {
                    _logger.LogWarning($"Cat�gorie avec ID {id} non trouv�e");
                    return NotFound(new { message = $"Cat�gorie avec ID {id} non trouv�e" });
                }

                return Ok(categorie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la r�cup�ration de la cat�gorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la r�cup�ration de la cat�gorie {id}", error = ex.Message });
            }
        }

        // GET: api/Categories/5/sous-categories
        [HttpGet("{id}/sous-categories")]
        public async Task<ActionResult<IEnumerable<CategorieSimpleDto>>> GetSousCategories(int id)
        {
            try
            {
                _logger.LogInformation($"Récupération des sous-catégories pour la catégorie ID: {id}");

                var sousCategories = await _context.Categories
                    .Where(c => c.ParentCategorieId == id)
                    .Select(c => new CategorieSimpleDto
                    {
                        IdCategorie = c.IdCategorie,
                        NomCategorie = c.NomCategorie
                    })
                    .ToListAsync();

                return Ok(sousCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des sous-catégories pour la catégorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la récupération des sous-catégories", error = ex.Message });
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

        // Vérifier si ParentCategorieId est valide
        if (categorie.ParentCategorieId.HasValue)
        {
            var parentExists = await _context.Categories.AnyAsync(c => c.IdCategorie == categorie.ParentCategorieId);
            if (!parentExists)
            {
                _logger.LogWarning($"Catégorie parente avec ID {categorie.ParentCategorieId} non trouvée");
                return BadRequest(new { message = $"Catégorie parente avec ID {categorie.ParentCategorieId} non trouvée" });
            }
        }

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
                _logger.LogWarning("L'ID dans l'URL ne correspond pas � l'ID dans les donn�es");
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas � l'ID dans les donn�es" });
            }

            try
            {
                _logger.LogInformation($"Mise � jour de la cat�gorie avec ID: {id}");
                _logger.LogDebug($"Donn�es re�ues: {System.Text.Json.JsonSerializer.Serialize(categorie)}");

                _context.Entry(categorie).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cat�gorie avec ID {id} mise � jour avec succ�s");

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CategorieExists(id))
                {
                    _logger.LogWarning($"Cat�gorie avec ID {id} non trouv�e lors de la mise � jour");
                    return NotFound(new { message = $"Cat�gorie avec ID {id} non trouv�e" });
                }
                else
                {
                    _logger.LogError(ex, $"Erreur de concurrence lors de la mise � jour de la cat�gorie {id}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la mise � jour de la cat�gorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la mise � jour de la cat�gorie {id}", error = ex.Message });
            }
        }

        [HttpGet("principales")]
public async Task<ActionResult<IEnumerable<CategorieSimpleDto>>> GetCategoriesPrincipales()
{
    var principales = await _context.Categories
        .Where(c => c.ParentCategorieId == null)
        .Select(c => new CategorieSimpleDto
        {
            IdCategorie = c.IdCategorie,
            NomCategorie = c.NomCategorie
        })
        .ToListAsync();

    return Ok(principales);
}


        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategorie(int id)
        {
            try
            {
                _logger.LogInformation($"Suppression de la cat�gorie avec ID: {id}");

                var categorie = await _context.Categories.FindAsync(id);
                if (categorie == null)
                {
                    _logger.LogWarning($"Cat�gorie avec ID {id} non trouv�e lors de la suppression");
                    return NotFound(new { message = $"Cat�gorie avec ID {id} non trouv�e" });
                }

                _context.Categories.Remove(categorie);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cat�gorie avec ID {id} supprim�e avec succ�s");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la cat�gorie {id}");
                return StatusCode(500, new { message = $"Erreur lors de la suppression de la cat�gorie {id}", error = ex.Message });
            }
        }

        private bool CategorieExists(int id)
        {
            return _context.Categories.Any(e => e.IdCategorie == id);
        }
    }
     public class CategorieSimpleDto
    {
        public int IdCategorie { get; set; }
        public string NomCategorie { get; set; } = string.Empty;
    }
}
