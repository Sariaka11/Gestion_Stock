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
                return StatusCode(500, "Erreur lors de la r�cup�ration des affectations.");
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
                    return NotFound("Aucune affectation trouv�e pour ce bien et cette agence.");
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

                // V�rifier si la quantit� est positive
                if (!dto.Quantite.HasValue || dto.Quantite <= 0)
                {
                    Console.WriteLine("Quantit� non valide: " + dto.Quantite);
                    return BadRequest("La quantit� doit �tre un nombre positif.");
                }

                // V�rifier si l'immobilisation existe et a suffisamment de stock
                var immobilisation = await _context.Immobilisations
                    .FirstOrDefaultAsync(i => i.IdBien == dto.IdBien);
                if (immobilisation == null)
                {
                    Console.WriteLine($"Immobilisation non trouv�e pour IdBien: {dto.IdBien}");
                    return NotFound("Le bien sp�cifi� n'existe pas.");
                }

                Console.WriteLine($"Stock disponible: {immobilisation.Quantite}, Quantit� demand�e: {dto.Quantite}");
                if (immobilisation.Quantite < dto.Quantite)
                {
                    return BadRequest("Quantit� insuffisante en stock.");
                }

                // V�rifier si l'agence existe
                var agence = await _context.Agences.FirstOrDefaultAsync(a => a.Id == dto.IdAgence);
                if (agence == null)
                {
                    Console.WriteLine($"Agence non trouv�e avec ID: {dto.IdAgence}");
                    return BadRequest($"L'agence avec l'ID {dto.IdAgence} n'existe pas.");
                }

                // D�finir la date d'affectation � aujourd'hui si non sp�cifi�e
                if (dto.DateAffectation == default)
                {
                    dto.DateAffectation = DateTime.Now;
                    Console.WriteLine($"DateAffectation d�finie � : {dto.DateAffectation}");
                }

                // V�rifier si une affectation existe pour IdBien et IdAgence
                var existingAffectation = await _context.BienAgences
                    .FirstOrDefaultAsync(ba => ba.IdBien == dto.IdBien && ba.IdAgence == dto.IdAgence);

                if (existingAffectation != null)
                {
                    // Mettre � jour la quantit� existante sans modifier DateAffectation
                    Console.WriteLine($"Affectation existante trouv�e: Quantit� actuelle = {existingAffectation.Quantite}, Nouvelle quantit� = {dto.Quantite}");
                    existingAffectation.Quantite = (existingAffectation.Quantite ?? 0) + dto.Quantite.Value;
                }
                else
                {
                    // Cr�er une nouvelle affectation
                    Console.WriteLine("Cr�ation d'une nouvelle affectation.");
                    var bienAgence = _mapper.Map<BienAgence>(dto);
                    _context.BienAgences.Add(bienAgence);
                }

                // Diminuer la quantit� dans Immobilisations
                immobilisation.Quantite -= dto.Quantite.Value;
                Console.WriteLine($"Nouvelle quantit� dans Immobilisations: {immobilisation.Quantite}");

                // Sauvegarder les changements
                await _context.SaveChangesAsync();

                // R�cup�rer l'affectation (existante ou nouvelle) avec relations
                var affectationResult = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .Include(ba => ba.Agence)
                    .FirstOrDefaultAsync(affectation =>
                        affectation.IdBien == dto.IdBien &&
                        affectation.IdAgence == dto.IdAgence);

                if (affectationResult == null)
                {
                    Console.WriteLine("Erreur: Affectation non trouv�e apr�s sauvegarde.");
                    return StatusCode(500, "Erreur lors de la cr�ation de l'affectation.");
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
                    Console.WriteLine($"Affectation non trouv�e pour suppression: IdBien={idBien}, IdAgence={idAgence}, Date={date}");
                    return NotFound("Affectation non trouv�e.");
                }

                // Restaurer la quantit� dans Immobilisations si Quantite existe
                if (bienAgence.Quantite.HasValue && bienAgence.Immobilisation != null)
                {
                    bienAgence.Immobilisation.Quantite += bienAgence.Quantite.Value;
                    Console.WriteLine($"Quantit� restaur�e dans Immobilisations: {bienAgence.Immobilisation.Quantite}");
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