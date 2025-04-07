using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("USER_AGENCE", Schema = "SYSTEM")]
    public class UserAgence
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("AGENCE_ID")]
        public int AgenceId { get; set; }

        [Column("DATE_ASSOCIATION")]
        public DateTime DateAssociation { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("AgenceId")]
        public virtual Agence? Agence { get; set; }
    }
}