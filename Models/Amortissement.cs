using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("AMORTISSEMENTS")]
    public class Amortissement
    {
        [Key]
        [Column("ID_AMORTISSEMENT")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAmortissement { get; set; }

        [Required]
        [Column("ID_BIEN")]
        public int IdBien { get; set; }

        [Required]
        [Column("ANNEE")]
        public int Annee { get; set; }

        [Required]
        [Column("MONTANT")]
        [Range(0, double.MaxValue)]
        public decimal Montant { get; set; }

        [Required]
        [Column("VALEUR_RESIDUELLE")]
        [Range(0, double.MaxValue)]
        public decimal ValeurResiduelle { get; set; }

        [Required]
        [Column("DATE_CALCUL")]
        public DateTime DateCalcul { get; set; }

        // Navigation property
        [ForeignKey("IdBien")]
        public virtual Immobilisation? Immobilisation { get; set; }
    }
}
