using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("FOURNITURES", Schema = "SYSTEM")]
    public class Fourniture
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NOM")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [Column("PRIX_UNITAIRE", TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Column("QUANTITE_RESTANTE")]
        public int QuantiteRestante { get; set; }

        [Required]
        [Column("CATEGORIE")]
        [StringLength(100)]
        public string Categorie { get; set; } = string.Empty;

        // Ajout des propriétés de navigation
        public virtual ICollection<EntreeFourniture> EntreesFournitures { get; set; } = new List<EntreeFourniture>();
        public virtual ICollection<AgenceFourniture> AgenceFournitures { get; set; } = new List<AgenceFourniture>();
        public virtual ICollection<UserFourniture> UserFournitures { get; set; } = new List<UserFourniture>();

        // Propriétés calculées (non stockées en base de données)
        [NotMapped]
        public decimal CMUP { get; set; }

        [NotMapped]
        public int Quantite { get; set; } // Quantité totale des entrées (calculée)

        [NotMapped]
        public decimal Montant { get; set; } // Montant total des entrées (calculé)

        [NotMapped]
        public decimal PrixTotal { get; set; } // Prix total basé sur la quantité restante (calculé)
    }
}