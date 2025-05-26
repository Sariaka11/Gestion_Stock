using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("CATEGORIES")]
    public class Categorie
    {
        [Key]
        [Column("ID_CATEGORIE")]
        public int IdCategorie { get; set; }

        [Required]
        [Column("NOM_CATEGORIE")]
        [StringLength(100)]
        public string NomCategorie { get; set; } = string.Empty;

        [Required]
        [Column("DUREE_AMORTISSEMENT")]
        [Range(1, 100)]
        public int DureeAmortissement { get; set; }

        // Navigation property
        public virtual ICollection<Immobilisation>? Immobilisations { get; set; }
    }
}
