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
        [Column("DATE")]
        public DateTime Date { get; set; }

        [Required]
        [Column("PRIX_UNITAIRE", TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Column("QUANTITE")]
        public int Quantite { get; set; }

        [Required]
        [Column("PRIX_TOTAL", TypeName = "decimal(18,2)")]
        public decimal PrixTotal { get; set; }

        [Required]
        [Column("QUANTITE_RESTANTE")]
        public int QuantiteRestante { get; set; }

        [Required]
        [Column("MONTANT", TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [Required]
        [Column("CATEGORIE")]
        [StringLength(100)]
        public string Categorie { get; set; } = string.Empty;


        // Ajout des propriétés de navigation
        public virtual ICollection<AgenceFourniture> AgenceFournitures { get; set; } = new List<AgenceFourniture>();
        public virtual ICollection<UserFourniture> UserFournitures { get; set; } = new List<UserFourniture>();
        // Propriété calculée pour le CMUP (non stockée en base de données)
        [NotMapped]
        public decimal CMUP { get; set; }
    }
}