using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionFournituresAPI.Data;
using GestionFournituresAPI.Dtos;
using GestionFournituresAPI.Models;
using AutoMapper;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpPost]
        public async Task<IActionResult> PostBienAgence(BienAgenceDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState invalide: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                if (!dto.Quantite.HasValue || dto.Quantite <= 0)
                {
                    Console.WriteLine("Quantité non valide: " + dto.Quantite);
                    return BadRequest("La quantité doit être un nombre positif.");
                }

                if (dto.DateAffectation == default)
                {
                    dto.DateAffectation = DateTime.Today;
                    Console.WriteLine($"DateAffectation définie à : {dto.DateAffectation:dd/MM/yy}");
                }

                var immobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
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

                var agence = await _context.Agences.FirstOrDefaultAsync(a => a.Id == dto.IdAgence);
                if (agence == null)
                {
                    Console.WriteLine($"Agence non trouvée avec ID: {dto.IdAgence}");
                    return BadRequest($"L'agence avec l'ID {dto.IdAgence} n'existe pas.");
                }

                var existingAffectation = await _context.BienAgences
                    .FirstOrDefaultAsync(ba => ba.IdBien == dto.IdBien &&
                                             ba.IdAgence == dto.IdAgence &&
                                             ba.DateAffectation.Date == dto.DateAffectation.Date);

                Console.WriteLine($"Recherche affectation existante: IdBien={dto.IdBien}, IdAgence={dto.IdAgence}, Date={dto.DateAffectation:dd/MM/yy}");
                Console.WriteLine($"Affectation trouvée: {(existingAffectation != null ? "OUI" : "NON")}");

                if (existingAffectation != null)
                {
                    Console.WriteLine($"Détails affectation existante: Fonction={existingAffectation.Fonction}, Quantité={existingAffectation.Quantite}, QuantiteConso={existingAffectation.QuantiteConso}");
                }

                BienAgence bienAgence;

                if (existingAffectation != null)
                {
                    Console.WriteLine($"Mise à jour de l'affectation existante: ancienne fonction={existingAffectation.Fonction}, nouvelle fonction={dto.Fonction}, ancienne quantité={existingAffectation.Quantite}");

                    existingAffectation.Quantite += dto.Quantite.Value;
                    existingAffectation.Fonction = dto.Fonction;

                    if (dto.QuantiteConso.HasValue)
                    {
                        existingAffectation.QuantiteConso = dto.QuantiteConso.Value;
                    }

                    bienAgence = existingAffectation;
                    Console.WriteLine($"Nouvelle quantité après mise à jour: {existingAffectation.Quantite}, nouvelle fonction: {existingAffectation.Fonction}");
                }
                else
                {
                    Console.WriteLine($"Création d'une nouvelle affectation: ID_BIEN={dto.IdBien}, ID_AGENCE={dto.IdAgence}, FONCTION={dto.Fonction}");

                    bienAgence = new BienAgence
                    {
                        IdBien = dto.IdBien,
                        IdAgence = dto.IdAgence,
                        Quantite = dto.Quantite.Value,
                        QuantiteConso = dto.QuantiteConso ?? 0,
                        DateAffectation = dto.DateAffectation,
                        Fonction = dto.Fonction
                    };

                    _context.BienAgences.Add(bienAgence);
                }

                immobilisation.Quantite -= dto.Quantite.Value;
                Console.WriteLine($"Nouvelle quantité dans Immobilisations: {immobilisation.Quantite}");

                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync a affecté {saveResult} ligne(s).");

                await transaction.CommitAsync();

                var affectationResult = await _context.BienAgences
                    .AsNoTracking()
                    .Include(ba => ba.Immobilisation)
                    .ThenInclude(i => i.Categorie)
                    .Include(ba => ba.Agence)
                    .FirstOrDefaultAsync(affectation =>
                        affectation.IdBien == dto.IdBien &&
                        affectation.IdAgence == dto.IdAgence &&
                        affectation.DateAffectation.Date == dto.DateAffectation.Date);

                if (affectationResult == null)
                {
                    Console.WriteLine("Erreur: Affectation non trouvée après sauvegarde.");
                    return StatusCode(500, "Erreur lors de la création ou mise à jour de l'affectation.");
                }

                var resultDto = new BienAgenceDto
                {
                    IdBien = affectationResult.IdBien,
                    IdAgence = affectationResult.IdAgence,
                    NomBien = affectationResult.Immobilisation?.NomBien ?? "Bien inconnu",
                    NomAgence = affectationResult.Agence?.Nom ?? "Agence inconnue",
                    Categorie = affectationResult.Immobilisation?.Categorie?.NomCategorie ?? "Non catégorisé",
                    Quantite = affectationResult.Quantite,
                    QuantiteConso = affectationResult.QuantiteConso,
                    Fonction = affectationResult.Fonction ?? "Non spécifié",
                    DateAffectation = affectationResult.DateAffectation,
                    Immobilisation = affectationResult.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(affectationResult.Immobilisation) : null,
                    Agence = affectationResult.Agence != null ? _mapper.Map<AgenceDto>(affectationResult.Agence) : null
                };

                return CreatedAtAction(nameof(GetBienAgence),
                    new { idBien = dto.IdBien, idAgence = dto.IdAgence },
                    resultDto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Erreur de concurrence dans PostBienAgence: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(409, $"Erreur de concurrence: {ex.Message}. Veuillez réessayer.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Erreur dans PostBienAgence: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                return StatusCode(500, $"Erreur serveur: {ex.Message}. Détails: {ex.InnerException?.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBienAgences()
        {
            try
            {
                var bienAgences = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .ThenInclude(i => i.Categorie)
                    .Include(ba => ba.Agence)
                    .ToListAsync();

                Console.WriteLine($"Nombre d'affectations récupérées : {bienAgences.Count}");
                bienAgences.ForEach(ba =>
                {
                    if (ba.Immobilisation == null)
                        Console.WriteLine($"Immobilisation null pour IdBien: {ba.IdBien}");
                    if (ba.Agence == null)
                        Console.WriteLine($"Agence null pour IdAgence: {ba.IdAgence}");
                    if (string.IsNullOrEmpty(ba.Fonction))
                        Console.WriteLine($"Fonction vide pour IdBien: {ba.IdBien}, IdAgence: {ba.IdAgence}");
                    if (!ba.Quantite.HasValue)
                        Console.WriteLine($"Quantité nulle pour IdBien: {ba.IdBien}, IdAgence: {ba.IdAgence}");
                });

                var result = bienAgences
                    .Where(ba => ba.Quantite.HasValue && ba.Quantite > 0)
                    .Select(ba => new BienAgenceDto
                    {
                        IdBien = ba.IdBien,
                        IdAgence = ba.IdAgence,
                        NomBien = ba.Immobilisation?.NomBien ?? "Bien inconnu",
                        NomAgence = ba.Agence?.Nom ?? "Agence inconnue",
                        Categorie = ba.Immobilisation?.Categorie?.NomCategorie ?? "Non catégorisé",
                        Quantite = ba.Quantite ?? 0,
                        QuantiteConso = ba.QuantiteConso ?? 0,
                        DateAffectation = ba.DateAffectation,
                        Fonction = ba.Fonction ?? "Non spécifié",
                        Immobilisation = ba.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(ba.Immobilisation) : null,
                        Agence = ba.Agence != null ? _mapper.Map<AgenceDto>(ba.Agence) : null
                    })
                    .ToList();

                if (!result.Any())
                {
                    Console.WriteLine("Aucune affectation valide trouvée après filtrage.");
                    return Ok(new { values = new List<BienAgenceDto>() });
                }

                return Ok(new { values = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetBienAgences: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Erreur lors de la récupération des affectations: {ex.Message}");
            }
        }

        [HttpGet("Bien/{idBien}/Agence/{idAgence}")]
        public async Task<IActionResult> GetBienAgence(int idBien, int idAgence)
        {
            try
            {
                var bienAgences = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .ThenInclude(i => i.Categorie)
                    .Include(ba => ba.Agence)
                    .Where(ba => ba.IdBien == idBien && ba.IdAgence == idAgence)
                    .OrderByDescending(ba => ba.DateAffectation)
                    .ToListAsync();

                if (bienAgences == null || !bienAgences.Any())
                {
                    return NotFound("Aucune affectation trouvée pour ce bien et cette agence.");
                }

                var result = bienAgences.Select(ba => new BienAgenceDto
                {
                    IdBien = ba.IdBien,
                    IdAgence = ba.IdAgence,
                    NomBien = ba.Immobilisation != null ? ba.Immobilisation.NomBien : "Bien inconnu",
                    NomAgence = ba.Agence != null ? ba.Agence.Nom : "Agence inconnue",
                    Categorie = ba.Immobilisation != null && ba.Immobilisation.Categorie != null ? ba.Immobilisation.Categorie.NomCategorie : "Non catégorisé",
                    Quantite = ba.Quantite,
                    QuantiteConso = ba.QuantiteConso,
                    DateAffectation = ba.DateAffectation,
                    Fonction = ba.Fonction ?? "Non spécifié",
                    Immobilisation = ba.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(ba.Immobilisation) : null,
                    Agence = ba.Agence != null ? _mapper.Map<AgenceDto>(ba.Agence) : null
                }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetBienAgence: {ex.Message}");
                return StatusCode(500, "Erreur lors de la recherche de l'affectation.");
            }
        }

        [HttpGet("ByAgence/{agenceId}")]
        public async Task<IActionResult> GetBienByAgence(int agenceId)
        {
            try
            {
                var bienAgences = await _context.BienAgences
                    .Include(ba => ba.Immobilisation)
                    .ThenInclude(i => i.Categorie)
                    .Include(ba => ba.Agence)
                    .Where(ba => ba.IdAgence == agenceId)
                    .ToListAsync();

                Console.WriteLine($"Nombre d'éléments trouvés pour agence {agenceId}: {bienAgences.Count}");
                if (bienAgences == null || !bienAgences.Any())
                {
                    return NotFound(new { message = "Aucune donnée trouvée pour cette agence." });
                }

                var result = bienAgences.Select(ba => new BienAgenceDto
                {
                    IdBien = ba.IdBien,
                    IdAgence = ba.IdAgence,
                    NomBien = ba.Immobilisation != null ? ba.Immobilisation.NomBien : "Bien inconnu",
                    NomAgence = ba.Agence != null ? ba.Agence.Nom : "Agence inconnue",
                    Categorie = ba.Immobilisation != null && ba.Immobilisation.Categorie != null ? ba.Immobilisation.Categorie.NomCategorie : "Non catégorisé",
                    Quantite = ba.Quantite,
                    QuantiteConso = ba.QuantiteConso,
                    Fonction = ba.Fonction ?? "Non spécifié",
                    DateAffectation = ba.DateAffectation,
                    Immobilisation = ba.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(ba.Immobilisation) : null,
                    Agence = ba.Agence != null ? _mapper.Map<AgenceDto>(ba.Agence) : null
                }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetBienByAgence: {ex.Message}");
                return StatusCode(500, "Erreur lors de la récupération des biens par agence.");
            }
        }

        [HttpPost("Consommation")]
        public async Task<ActionResult<BienAgenceDto>> CreateConsommation([FromBody] ConsommationBienCreateDto createDto)
        {
            try
            {
                Console.WriteLine($"Requête reçue: {System.Text.Json.JsonSerializer.Serialize(createDto)}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    Console.WriteLine($"ModelState invalide: {string.Join("; ", errors)}");
                    return BadRequest(ModelState);
                }

                if (createDto.DateAffectation == default)
                {
                    Console.WriteLine("DateAffectation est vide ou invalide.");
                    return BadRequest("La date d'affectation est requise et doit être valide.");
                }

                if (!await _context.Agences.AnyAsync(a => a.Id == createDto.AgenceId))
                {
                    Console.WriteLine($"Agence non trouvée: Id={createDto.AgenceId}");
                    return BadRequest("L'agence spécifiée n'existe pas.");
                }

                var immobilisation = await _context.Immobilisations
                    .Include(i => i.Categorie)
                    .FirstOrDefaultAsync(i => i.IdBien == createDto.BienId);
                if (immobilisation == null)
                {
                    Console.WriteLine($"Immobilisation non trouvée: Id={createDto.BienId}");
                    return BadRequest("Le bien spécifié n'existe pas.");
                }

                if (!createDto.QuantiteConso.HasValue || createDto.QuantiteConso <= 0)
                {
                    Console.WriteLine($"QuantiteConso invalide: {createDto.QuantiteConso}");
                    return BadRequest("La consommation doit être un nombre positif.");
                }

                if (immobilisation.Quantite < createDto.QuantiteConso)
                {
                    Console.WriteLine($"Stock insuffisant: Quantite={immobilisation.Quantite}, Demandé={createDto.QuantiteConso}");
                    return BadRequest("La consommation dépasse le stock disponible.");
                }

                var existing = await _context.BienAgences
                    .FirstOrDefaultAsync(ba => ba.IdAgence == createDto.AgenceId && ba.IdBien == createDto.BienId);

                int resultIdBien, resultIdAgence;
                if (existing != null)
                {
                    if (existing.DateAffectation != createDto.DateAffectation)
                    {
                        Console.WriteLine($"DateAffectation ignorée: Nouvelle={createDto.DateAffectation}, Conservée={existing.DateAffectation}.");
                    }

                    if (existing.Quantite < createDto.QuantiteConso)
                    {
                        Console.WriteLine($"Quantité insuffisante dans l'affectation: Quantite={existing.Quantite}, Demandé={createDto.QuantiteConso}");
                        return BadRequest("La consommation dépasse la quantité disponible dans l'agence.");
                    }
                    existing.Quantite = Math.Max(0, (existing.Quantite ?? 0) - (int)createDto.QuantiteConso.Value);
                    existing.QuantiteConso = (existing.QuantiteConso ?? 0) + createDto.QuantiteConso;
                    _context.BienAgences.Update(existing);
                    resultIdBien = existing.IdBien;
                    resultIdAgence = existing.IdAgence;
                    Console.WriteLine($"Affectation existante mise à jour: IdBien={resultIdBien}, IdAgence={resultIdAgence}");
                }
                else
                {
                    var bienAgence = new BienAgence
                    {
                        IdAgence = createDto.AgenceId,
                        IdBien = createDto.BienId,
                        Quantite = 0,
                        QuantiteConso = createDto.QuantiteConso,
                        DateAffectation = createDto.DateAffectation
                    };
                    _context.BienAgences.Add(bienAgence);
                    resultIdBien = bienAgence.IdBien;
                    resultIdAgence = bienAgence.IdAgence;
                    Console.WriteLine($"Nouvelle affectation créée: IdBien={resultIdBien}, IdAgence={resultIdAgence}");
                }

                immobilisation.Quantite -= (int)createDto.QuantiteConso.Value;
                _context.Immobilisations.Update(immobilisation);
                Console.WriteLine($"Stock mis à jour: IdBien={immobilisation.IdBien}, Nouvelle Quantite={immobilisation.Quantite}");

                await _context.SaveChangesAsync();
                Console.WriteLine("Changements sauvegardés avec succès.");

                var result = await _context.BienAgences
                    .Include(ba => ba.Agence)
                    .Include(ba => ba.Immobilisation)
                    .ThenInclude(i => i.Categorie)
                    .FirstOrDefaultAsync(ba => ba.IdBien == resultIdBien && ba.IdAgence == resultIdAgence);

                if (result == null)
                {
                    Console.WriteLine("Erreur: Résultat non trouvé après sauvegarde.");
                    return StatusCode(500, "Erreur lors de la récupération de la consommation.");
                }

                var resultDto = new BienAgenceDto
                {
                    IdBien = result.IdBien,
                    IdAgence = result.IdAgence,
                    NomBien = result.Immobilisation != null ? result.Immobilisation.NomBien : "Bien inconnu",
                    NomAgence = result.Agence != null ? result.Agence.Nom : "Agence inconnue",
                    Categorie = result.Immobilisation != null && result.Immobilisation.Categorie != null ? result.Immobilisation.Categorie.NomCategorie : "Non catégorisé",
                    Quantite = result.Quantite,
                    QuantiteConso = result.QuantiteConso,
                    DateAffectation = result.DateAffectation,
                    Fonction = result.Fonction ?? "Non spécifié",
                    Immobilisation = result.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(result.Immobilisation) : null,
                    Agence = result.Agence != null ? _mapper.Map<AgenceDto>(result.Agence) : null
                };

                Console.WriteLine($"Résultat retourné: IdBien={resultDto.IdBien}, IdAgence={resultDto.IdAgence}");
                return CreatedAtAction(nameof(GetBienAgence), new { idBien = result.IdBien, idAgence = result.IdAgence }, resultDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans CreateConsommation: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

      [HttpPost("Consommation/Add")]
public async Task<ActionResult<BienAgenceDto>> AddConsommation([FromBody] ConsommationBienCreateDto createDto)
{
    try
    {
        Console.WriteLine($"Requête reçue pour AddConsommation: {System.Text.Json.JsonSerializer.Serialize(createDto)}");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Console.WriteLine($"ModelState invalide: {string.Join("; ", errors)}");
            return BadRequest(ModelState);
        }

        // Vérification s'il y a des doublons
        var existingRecords = await _context.BienAgences
            .Where(ba => ba.IdAgence == createDto.AgenceId && ba.IdBien == createDto.BienId)
            .ToListAsync();

        Console.WriteLine($"Nombre d'enregistrements trouvés: {existingRecords.Count}");
        
        if (existingRecords.Count == 0)
        {
            Console.WriteLine($"Aucune affectation trouvée pour AgenceId={createDto.AgenceId}, BienId={createDto.BienId}");
            return NotFound("Aucune association existante pour cette agence et ce bien.");
        }

        if (existingRecords.Count > 1)
        {
            Console.WriteLine($"Plusieurs enregistrements trouvés ({existingRecords.Count}) pour AgenceId={createDto.AgenceId}, BienId={createDto.BienId}");
            foreach (var record in existingRecords)
            {
                Console.WriteLine($"  - ID_BIEN: {record.IdBien}, ID_AGENCE: {record.IdAgence}, DATE_AFFECTATION: {record.DateAffectation}");
            }
            
            // Prendre l'enregistrement le plus récent ou celui avec la plus grande quantité
            var targetRecord = existingRecords
                .OrderByDescending(ba => ba.DateAffectation)
                .ThenByDescending(ba => ba.Quantite)
                .FirstOrDefault();
            
            Console.WriteLine($"Enregistrement sélectionné: DATE_AFFECTATION={targetRecord.DateAffectation}, QUANTITE={targetRecord.Quantite}");
            
            // Utiliser une requête spécifique avec tous les champs de la clé primaire
            var existing = await _context.BienAgences
                .FirstOrDefaultAsync(ba => ba.IdAgence == createDto.AgenceId && 
                                          ba.IdBien == createDto.BienId && 
                                          ba.DateAffectation == targetRecord.DateAffectation);
            
            if (existing == null)
            {
                Console.WriteLine("Erreur: Enregistrement spécifique non trouvé");
                return NotFound("Enregistrement spécifique non trouvé.");
            }
            
            return await ProcessConsommation(existing, createDto);
        }
        else
        {
            // Cas normal : un seul enregistrement
            return await ProcessConsommation(existingRecords.First(), createDto);
        }
    }
    catch (DbUpdateException ex)
    {
        Console.WriteLine($"Erreur de base de données dans AddConsommation: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
        return StatusCode(500, $"Erreur serveur: {ex.InnerException?.Message ?? ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur dans AddConsommation: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
        return StatusCode(500, $"Erreur lors de l'ajout de la consommation: {ex.Message}");
    }
}

        private async Task<ActionResult<BienAgenceDto>> ProcessConsommation(BienAgence existing, ConsommationBienCreateDto createDto)
        {
            var immobilisation = await _context.Immobilisations
                .FirstOrDefaultAsync(i => i.IdBien == createDto.BienId);

            if (immobilisation == null)
            {
                Console.WriteLine($"Immobilisation non trouvée: Id={createDto.BienId}");
                return BadRequest("Le bien spécifié n'existe pas.");
            }

            if (!createDto.QuantiteConso.HasValue || createDto.QuantiteConso <= 0)
            {
                Console.WriteLine($"QuantiteConso invalide: {createDto.QuantiteConso}");
                return BadRequest("La consommation doit être un nombre positif.");
            }

            // Vérification des quantités disponibles
            if (existing.Quantite < createDto.QuantiteConso)
            {
                Console.WriteLine($"Quantité insuffisante dans l'affectation: Quantite={existing.Quantite}, Demandé={createDto.QuantiteConso}");
                return BadRequest("La consommation dépasse la quantité disponible dans l'agence.");
            }

            if (immobilisation.Quantite < createDto.QuantiteConso)
            {
                Console.WriteLine($"Stock insuffisant: Quantite={immobilisation.Quantite}, Demandé={createDto.QuantiteConso}");
                return BadRequest("La consommation dépasse le stock restant.");
            }

            // Mise à jour des quantités
            existing.Quantite = Math.Max(0, (existing.Quantite ?? 0) - (int)createDto.QuantiteConso.Value);
            existing.QuantiteConso = (existing.QuantiteConso ?? 0) + createDto.QuantiteConso.Value;

            // Mise à jour de l'immobilisation
            immobilisation.Quantite -= (int)createDto.QuantiteConso.Value;

            // Sauvegarde des modifications
            await _context.SaveChangesAsync();
            Console.WriteLine($"Mise à jour effectuée: IdBien={existing.IdBien}, IdAgence={existing.IdAgence}");

            // Récupération du résultat avec les relations
            var result = await _context.BienAgences
                .Include(ba => ba.Agence)
                .Include(ba => ba.Immobilisation)
                .ThenInclude(i => i.Categorie)
                .FirstOrDefaultAsync(ba => ba.IdBien == existing.IdBien &&
                                          ba.IdAgence == existing.IdAgence &&
                                          ba.DateAffectation == existing.DateAffectation);

            if (result == null)
            {
                Console.WriteLine("Erreur: Résultat non trouvé après sauvegarde.");
                return NotFound("Erreur lors de la récupération de la consommation mise à jour.");
            }

            var resultDto = new BienAgenceDto
            {
                IdBien = result.IdBien,
                IdAgence = result.IdAgence,
                NomBien = result.Immobilisation?.NomBien ?? "Bien inconnu",
                NomAgence = result.Agence?.Nom ?? "Agence inconnue",
                Categorie = result.Immobilisation?.Categorie?.NomCategorie ?? "Non catégorisé",
                Quantite = result.Quantite ?? 0,
                QuantiteConso = result.QuantiteConso,
                DateAffectation = result.DateAffectation,
                Fonction = result.Fonction ?? "Non spécifié",
                Immobilisation = result.Immobilisation != null ? _mapper.Map<ImmobilisationDto>(result.Immobilisation) : null,
                Agence = result.Agence != null ? _mapper.Map<AgenceDto>(result.Agence) : null
            };

            Console.WriteLine($"Résultat retourné: IdBien={resultDto.IdBien}, IdAgence={resultDto.IdAgence}");
            return Ok(resultDto);
        }


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