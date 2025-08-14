using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? UserId { get; set; }  // Rendre nullable pour correspondre à la DB
        // Supprimer UserName ou le rendre ignoré par EF
        [NotMapped]
        public string? UserName { get; set; }
        
        public int AgenceId { get; set; }
        public int? FournitureId { get; set; }
        public int? BienId { get; set; }
        public string? Titre { get; set; }
        public string? Corps { get; set; }
        public DateTime DateDemande { get; set; }
        public string? Statut { get; set; }
        
        
        // Navigation properties
        public Agence? Agence { get; set; }
        public Fourniture? Fourniture { get; set; }
        public Immobilisation? Immobilisation { get; set; }
    }
}