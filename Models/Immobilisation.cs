using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("IMMOBILISATIONS")]
    public class Immobilisation
    {
        [Key]
        [Column("ID_BIEN")]
        public int IdBien { get; set; }

        [Required]
        [Column("NOM_BIEN")]
        [StringLength(100)]
        public string NomBien { get; set; } = string.Empty;

        [Column("CODE_BARRE")]
        [StringLength(20)]
        public string? CodeBarre { get; set; }

        [Required]
        [Column("VALEUR_ACQUISITION")]
        [Range(0, double.MaxValue)]
        public decimal ValeurAcquisition { get; set; }

        [Column("QUANTITE")]
        public int? Quantite { get; set; }

        [Column("STATUT")]
        [StringLength(20)]
        public string? Statut { get; set; }

        [Column("ID_CATEGORIE")]
        public int? IdCategorie { get; set; }

        [Column("DATE_ACQUISITION")]
        public DateTime? DateAcquisition { get; set; }

        // Navigation properties
        [ForeignKey("IdCategorie")]
        public virtual Categorie? Categorie { get; set; }

        public virtual ICollection<Amortissement>? Amortissements { get; set; }

        public virtual ICollection<BienAgence>? BienAgences { get; set; }

        // Propriétés calculées
        [NotMapped]
        public decimal ValeurNetteComptable { get; set; }

        [NotMapped]
        public decimal AmortissementCumule { get; set; }

        [NotMapped]
        public int DureeRestante { get; set; }

        [NotMapped]
        public DateTime? DateFinAmortissement { get; set; }
    }
}