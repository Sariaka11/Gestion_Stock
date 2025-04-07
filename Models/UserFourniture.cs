using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("USER_FOURNITURE", Schema = "SYSTEM")]
    public class UserFourniture
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("FOURNITURE_ID")]
        public int FournitureId { get; set; }

        [Column("DATE_ASSOCIATION")]
        public DateTime DateAssociation { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("FournitureId")]
        public virtual Fourniture? Fourniture { get; set; }
    }
}