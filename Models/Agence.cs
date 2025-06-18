using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace GestionFournituresAPI.Models
{
    [Table("AGENCES")]
    public class Agence
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NUMERO")]
        [StringLength(50)]
        public string? Numero { get; set; } = string.Empty;
       
        [Required]
        [Column("NOM")]
        [StringLength(100)]
        public string? Nom { get; set; } = string.Empty;

        // Propriétés de navigation
        public virtual ICollection<UserAgence> UserAgences { get; set; } = new List<UserAgence>();
        public virtual ICollection<AgenceFourniture> AgenceFournitures { get; set; } = new List<AgenceFourniture>();
        public virtual ICollection<BienAgence> BienAgences { get; set; } = new List<BienAgence>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}