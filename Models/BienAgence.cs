using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("BIEN_AGENCE")]
    public class BienAgence
    {
        [Column("ID_BIEN")]
        public int IdBien { get; set; }

        [Column("ID_AGENCE")]
        public int IdAgence { get; set; }

        [Column("DATE_AFFECTATION")]
        public DateTime DateAffectation { get; set; }

        [Column("QUANTITE")]
        public int? Quantite { get; set; }

        // Navigation properties
        [ForeignKey("IdBien")]
        public virtual Immobilisation? Immobilisation { get; set; }

        [ForeignKey("IdAgence")]
        public virtual Agence? Agence { get; set; }
    }
}
