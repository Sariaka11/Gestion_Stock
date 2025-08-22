using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace GestionFournituresAPI.Models
{
    [Table("FOURNITURES")]
    public class Fourniture
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NOM")]
        [StringLength(100)]
        public string? Nom { get; set; } = string.Empty;

        [Required]
        [Column("PRIX_UNITAIRE", TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Column("QUANTITE_RESTANTE")]
        public int QuantiteRestante { get; set; }

        [Required]
        [Column("CATEGORIE")]
        [StringLength(100)]
        public string? Categorie { get; set; } = string.Empty;

        // Ajout des propriétés de navigation
        public virtual ICollection<EntreeFourniture> EntreesFournitures { get; set; } = new List<EntreeFourniture>();
        public virtual ICollection<AgenceFourniture> AgenceFournitures { get; set; } = new List<AgenceFourniture>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // Propriétés calculées (non stockées en base de données)
        [NotMapped]
        public decimal CMUP { get; set; }

        [NotMapped]
        public int Quantite { get; set; }

        [NotMapped]
        public decimal Montant { get; set; }

        [NotMapped]
        public decimal PrixTotal { get; set; }
    }
}