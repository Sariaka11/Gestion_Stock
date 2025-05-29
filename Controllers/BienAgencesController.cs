using GestionFournituresAPI.Data;
using GestionFournituresAPI.Dtos;
using GestionFournituresAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BienAgencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public BienAgencesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/BienAgences
        [HttpGet]
        public async Task<IActionResult> GetBienAgences()
        {
            try
            {
                var bienAgences = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .Include(ba => ba.Agence)
                    .ToListAsync();
                return Ok(_mapper.Map<List<BienAgenceDto>>(bienAgences));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetBienAgences: {ex.Message}");
                return StatusCode(500, "Erreur lors de la récupération des affectations.");
            }
        }

        // GET: api/BienAgences/Bien/5/Agence/3
        [HttpGet("Bien/{idBien}/Agence/{idAgence}")]
        public async Task<IActionResult> GetBienAgence(int idBien, int idAgence)
        {
            try
            {
                var bienAgences = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .Include(ba => ba.Agence)
                    .Where(ba => ba.IdBien == idBien && ba.IdAgence == idAgence)
                    .OrderByDescending(ba => ba.DateAffectation)
                    .ToListAsync();

                if (bienAgences == null || !bienAgences.Any())
                {
                    return NotFound("Aucune affectation trouvée pour ce bien et cette agence.");
                }

                return Ok(_mapper.Map<List<BienAgenceDto>>(bienAgences));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetBienAgence: {ex.Message}");
                return StatusCode(500, "Erreur lors de la recherche de l'affectation.");
            }
        }

        // POST: api/BienAgences
        [HttpPost]
        public async Task<IActionResult> PostBienAgence(BienAgenceDto dto)
        {
            try
            {
                // Valider le DTO
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState invalide: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                // Vérifier si la quantité est positive
                if (!dto.Quantite.HasValue || dto.Quantite <= 0)
                {
                    Console.WriteLine("Quantité non valide: " + dto.Quantite);
                    return BadRequest("La quantité doit être un nombre positif.");
                }

                // Vérifier si l'immobilisation existe et a suffisamment de stock
                var immobilisation = await _context.Immobilisations
                    .FirstOrDefaultAsync(i => i.IdBien == dto.IdBien);
                if (immobilisation == null)
                {
                    Console.WriteLine($"Immobilisation non trouvée pour IdBien: {dto.IdBien}");
                    return NotFound("Le bien spécifié n'existe pas.");
                }

                Console.WriteLine($"Stock disponible: {immobilisation.Quantite}, Quantité demandée: {dto.Quantite}");
                if (immobilisation.Quantite < dto.Quantite)
                {
                    return BadRequest("Quantité insuffisante en stock.");
                }

                // Vérifier si l'agence existe
                var agence = await _context.Agences.FirstOrDefaultAsync(a => a.Id == dto.IdAgence);
                if (agence == null)
                {
                    Console.WriteLine($"Agence non trouvée avec ID: {dto.IdAgence}");
                    return BadRequest($"L'agence avec l'ID {dto.IdAgence} n'existe pas.");
                }

                // Définir la date d'affectation à aujourd'hui si non spécifiée
                if (dto.DateAffectation == default)
                {
                    dto.DateAffectation = DateTime.Now;
                    Console.WriteLine($"DateAffectation définie à : {dto.DateAffectation}");
                }

                // Vérifier si une affectation existe pour IdBien et IdAgence
                var existingAffectation = await _context.BienAgences
                    .FirstOrDefaultAsync(ba => ba.IdBien == dto.IdBien && ba.IdAgence == dto.IdAgence);

                if (existingAffectation != null)
                {
                    // Mettre à jour la quantité existante sans modifier DateAffectation
                    Console.WriteLine($"Affectation existante trouvée: Quantité actuelle = {existingAffectation.Quantite}, Nouvelle quantité = {dto.Quantite}");
                    existingAffectation.Quantite = (existingAffectation.Quantite ?? 0) + dto.Quantite.Value;
                }
                else
                {
                    // Créer une nouvelle affectation
                    Console.WriteLine("Création d'une nouvelle affectation.");
                    var bienAgence = _mapper.Map<BienAgence>(dto);
                    _context.BienAgences.Add(bienAgence);
                }

                // Diminuer la quantité dans Immobilisations
                immobilisation.Quantite -= dto.Quantite.Value;
                Console.WriteLine($"Nouvelle quantité dans Immobilisations: {immobilisation.Quantite}");

                // Sauvegarder les changements
                await _context.SaveChangesAsync();

                // Récupérer l'affectation (existante ou nouvelle) avec relations
                var affectationResult = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .Include(ba => ba.Agence)
                    .FirstOrDefaultAsync(affectation =>
                        affectation.IdBien == dto.IdBien &&
                        affectation.IdAgence == dto.IdAgence);

                if (affectationResult == null)
                {
                    Console.WriteLine("Erreur: Affectation non trouvée après sauvegarde.");
                    return StatusCode(500, "Erreur lors de la création de l'affectation.");
                }

                return CreatedAtAction(nameof(GetBienAgence),
                    new { idBien = dto.IdBien, idAgence = dto.IdAgence },
                    _mapper.Map<BienAgenceDto>(affectationResult));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans PostBienAgence: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // DELETE: api/BienAgences/Bien/5/Agence/3/Date/2023-04-01
        [HttpDelete("Bien/{idBien}/Agence/{idAgence}/Date/{date}")]
        public async Task<IActionResult> DeleteBienAgence(int idBien, int idAgence, DateTime date)
        {
            try
            {
                var bienAgence = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .FirstOrDefaultAsync(ba =>
                        ba.IdBien == idBien &&
                        ba.IdAgence == idAgence &&
                        ba.DateAffectation.Date == date.Date);

                if (bienAgence == null)
                {
                    Console.WriteLine($"Affectation non trouvée pour suppression: IdBien={idBien}, IdAgence={idAgence}, Date={date}");
                    return NotFound("Affectation non trouvée.");
                }

                // Restaurer la quantité dans Immobilisations si Quantite existe
                if (bienAgence.Quantite.HasValue && bienAgence.Immobilisation != null)
                {
                    bienAgence.Immobilisation.Quantite += bienAgence.Quantite.Value;
                    Console.WriteLine($"Quantité restaurée dans Immobilisations: {bienAgence.Immobilisation.Quantite}");
                }

                _context.BienAgences.Remove(bienAgence);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans DeleteBienAgence: {ex.Message}");
                return StatusCode(500, "Erreur lors de la suppression de l'affectation.");
            }
        }
    }
}