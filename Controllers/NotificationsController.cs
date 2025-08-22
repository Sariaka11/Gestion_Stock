using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Notifications
        [HttpPost]
        public async Task<ActionResult<Notification>> CreateNotification([FromBody] NotificationCreateDto createDto)
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

                // Vérifier l'utilisateur
                var userExists = await _context.Users.AnyAsync(u => u.Id == createDto.UserId);
                if (!userExists)
                {
                    Console.WriteLine($"Utilisateur non trouvé: Id={createDto.UserId}");
                    return BadRequest("L'utilisateur spécifié n'existe pas.");
                }

                // Vérifier l'agence
                var agence = await _context.Agences.FirstOrDefaultAsync(a => a.Id == createDto.AgenceId);
                if (agence == null)
                {
                    Console.WriteLine($"Agence non trouvée: Id={createDto.AgenceId}");
                    return BadRequest("L'agence spécifiée n'existe pas.");
                }

                // Vérifier la fourniture ou le bien
                decimal quantiteRestante = 0;
                string itemNom = "";
                if (createDto.FournitureId.HasValue)
                {
                    var fourniture = await _context.AgenceFournitures
                        .Include(af => af.Fourniture)
                        .FirstOrDefaultAsync(af => af.AgenceId == createDto.AgenceId && af.FournitureId == createDto.FournitureId);
                    if (fourniture == null)
                    {
                        Console.WriteLine($"Fourniture non trouvée: Id={createDto.FournitureId}");
                        return BadRequest("La fourniture spécifiée n'existe pas pour cette agence.");
                    }
                    quantiteRestante = fourniture.Quantite;
                    itemNom = fourniture.Fourniture?.Nom ?? "Fourniture inconnue";
                }
                else if (createDto.BienId.HasValue)
                {
                    var bien = await _context.BienAgences
                        .Include(ba => ba.Immobilisation)
                        .FirstOrDefaultAsync(ba => ba.IdAgence == createDto.AgenceId && ba.IdBien == createDto.BienId);
                    if (bien == null)
                    {
                        Console.WriteLine($"Bien non trouvé: Id={createDto.BienId}");
                        return BadRequest("Le bien spécifié n'existe pas pour cette agence.");
                    }
                    quantiteRestante = bien.Quantite.GetValueOrDefault(0);
                    itemNom = bien.Immobilisation?.NomBien ?? "Bien inconnu";
                }
                else
                {
                    Console.WriteLine("FournitureId ou BienId requis.");
                    return BadRequest("Une fourniture ou un bien doit être spécifié.");
                }

                // SOLUTION : Générer l'ID manuellement
                int nextId = 1;
                if (await _context.Notifications.AnyAsync())
                {
                    nextId = await _context.Notifications.MaxAsync(n => n.Id) + 1;
                }

                // Créer la notification avec l'ID généré
                var notification = new Notification
                {
                    Id = nextId, // Définir l'ID manuellement
                    UserId = createDto.UserId,
                    UserName = createDto.UserName,
                    AgenceId = createDto.AgenceId,
                    FournitureId = createDto.FournitureId,
                    BienId = createDto.BienId,
                    Titre = $"Demande de {agence.Nom} par {createDto.UserName}",
                    Corps = $"Demande d'envoi pour {itemNom} avec un stock restant de {quantiteRestante} en date du {DateTime.UtcNow:yyyy-MM-dd}.",
                    DateDemande = DateTime.UtcNow,
                    Statut = "Non vue"
                };

                _context.Notifications.Add(notification);

                // Utiliser une transaction pour éviter les problèmes de concurrence
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        Console.WriteLine($"Notification créée: Id={notification.Id}");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans CreateNotification: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }


        // GET: api/Notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int? userId, [FromQuery] bool isAdmin = false)
        {
            try
            {
                IQueryable<Notification> query = _context.Notifications
                    .Include(n => n.Agence)
                    .Include(n => n.Fourniture)
                    .Include(n => n.Immobilisation);

                if (isAdmin)
                {
                    // Admin voit toutes les notifications
                }
                else if (userId.HasValue)
                {
                    // Utilisateur voit ses propres notifications
                    query = query.Where(n => n.UserId == userId.Value);
                }
                else
                {
                    return BadRequest("UserId ou isAdmin requis.");
                }

                var notifications = await query.ToListAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetNotifications: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // GET: api/Notifications/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications
                    .Include(n => n.Agence)
                    .Include(n => n.Fourniture)
                    .Include(n => n.Immobilisation)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (notification == null)
                {
                    return NotFound();
                }

                return Ok(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetNotification: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }

        // PUT: api/Notifications/{id}/mark-as-read
        [HttpPut("{id}/mark-as-read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    Console.WriteLine($"Notification non trouvée: Id={id}");
                    return NotFound();
                }

                notification.Statut = "Vue";
                await _context.SaveChangesAsync();
                Console.WriteLine($"Notification marquée comme vue: Id={id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans MarkNotificationAsRead: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Erreur serveur: {ex.Message}");
            }
        }
    }

    public class NotificationCreateDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int AgenceId { get; set; }
        public int? FournitureId { get; set; }
        public int? BienId { get; set; }
    }
}