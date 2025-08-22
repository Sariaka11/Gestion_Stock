using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("AGENCE_FOURNITURE")]
    public class AgenceFourniture
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("AGENCE_ID")]
        public int AgenceId { get; set; }

        [Required]
        [Column("FOURNITURE_ID")]
        public int FournitureId { get; set; }

        [Required]
        [Column("QUANTITE")]
        public int Quantite { get; set; }

        [Column("DATE_ASSOCIATION")]
        public DateTime DateAssociation { get; set; } = DateTime.Now;

        [Column("CONSO_MM")]
        public decimal? ConsoMm { get; set; }

        // Navigation properties
        [ForeignKey("AgenceId")]
        public virtual Agence? Agence { get; set; }

        [ForeignKey("FournitureId")]
        public virtual Fourniture? Fourniture { get; set; }
    }
}