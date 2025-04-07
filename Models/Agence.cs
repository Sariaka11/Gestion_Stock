using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("AGENCES", Schema = "SYSTEM")]
    public class Agence
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NUMERO")]
        [StringLength(50, MinimumLength = 1)]
        public string Numero { get; set; } = string.Empty;

        [Required]
        [Column("NOM")]
        [StringLength(100, MinimumLength = 1)]
        public string Nom { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<UserAgence>? UserAgences { get; set; }
        public virtual ICollection<AgenceFourniture>? AgenceFournitures { get; set; }
    }
}