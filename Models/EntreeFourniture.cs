using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("ENTREE_FOURNITURES", Schema = "SYSTEM")]
    public class EntreeFourniture
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("FOURNITURE_ID")]
        public int FournitureId { get; set; }

        [Required]
        [Column("QUANTITE_ENTREE")]
        public int QuantiteEntree { get; set; }

        [Required]
        [Column("DATE_ENTREE")]
        public DateTime DateEntree { get; set; }

        [Required]
        [Column("PRIX_UNITAIRE", TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Column("MONTANT", TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        // Propriété de navigation nullable
        [ForeignKey("FournitureId")]
        public virtual Fourniture? Fourniture { get; set; }
    }
}