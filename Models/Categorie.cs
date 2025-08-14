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

    // 🔽 Auto-référence : sous-catégorie d'une catégorie parent
    [ForeignKey("ParentCategorie")]
    [Column("ID_PARENT")]
    public int? ParentCategorieId { get; set; }

    public virtual Categorie? ParentCategorie { get; set; }
    public virtual ICollection<Categorie>? SousCategories { get; set; }

    // Relation existante avec Immobilisations
    public virtual ICollection<Immobilisation>? Immobilisations { get; set; }
}

}
