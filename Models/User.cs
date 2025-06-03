using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionFournituresAPI.Models
{
    [Table("USERS", Schema = "SYSTEM")]
    public class User
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NOM")]
        [StringLength(100)]
        public string Nom { get; set; }

        [Required]
        [Column("PRENOM")]
        [StringLength(100)]
        public string Prenom { get; set; }

        [Required]
        [Column("EMAIL")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [Column("MOT_DE_PASSE")]
        [StringLength(2000)]
        public string MotDePasse { get; set; }

        [Required]
        [Column("FONCTION")]
        [StringLength(20)]
        public string Fonction { get; set; }
        public virtual ICollection<UserFourniture> UserFournitures { get; set; } = new List<UserFourniture>();

        // Relations avec UserAgence
        public virtual ICollection<UserAgence> UserAgences { get; set; } = new List<UserAgence>();     // public virtual Agence Agence { get; set; }
    }
}