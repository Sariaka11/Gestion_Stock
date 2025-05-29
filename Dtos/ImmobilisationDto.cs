using System.ComponentModel.DataAnnotations;

namespace GestionFournituresAPI.Dtos
{
    public class ImmobilisationDto
    {
        public int IdBien { get; set; }
        public string NomBien { get; set; } = string.Empty;
        public string? CodeBarre { get; set; }
        public decimal ValeurAcquisition { get; set; }
        public decimal TauxAmortissement { get; set; }
        public int? Quantite { get; set; }
        public string? Statut { get; set; }
        public int? IdCategorie { get; set; }
        public DateTime? DateAcquisition { get; set; }

        // Propriétés calculées
        public decimal ValeurNetteComptable { get; set; }
        public decimal AmortissementCumule { get; set; }
        public int DureeRestante { get; set; }
        public DateTime? DateFinAmortissement { get; set; }

        // Navigation properties
        public CategorieDto? Categorie { get; set; }
        public List<AmortissementDto>? Amortissements { get; set; }
    }

    public class ImmobilisationCreateDto
    {
        [Required(ErrorMessage = "Le nom du bien est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom du bien ne peut pas dépasser 100 caractères")]
        public string NomBien { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Le code barre ne peut pas dépasser 20 caractères")]
        public string? CodeBarre { get; set; }

        [Required(ErrorMessage = "La valeur d'acquisition est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "La valeur d'acquisition doit être positive")]
        public decimal ValeurAcquisition { get; set; }

        [Required(ErrorMessage = "Le taux d'amortissement est obligatoire")]
        [Range(0, 100, ErrorMessage = "Le taux d'amortissement doit être entre 0 et 100")]
        public decimal TauxAmortissement { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être positive")]
        public int? Quantite { get; set; }

        [StringLength(20, ErrorMessage = "Le statut ne peut pas dépasser 20 caractères")]
        public string? Statut { get; set; }

        public int? IdCategorie { get; set; }

        public DateTime? DateAcquisition { get; set; }
    }

    public class ImmobilisationUpdateDto
    {
        [Required(ErrorMessage = "Le nom du bien est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom du bien ne peut pas dépasser 100 caractères")]
        public string NomBien { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Le code barre ne peut pas dépasser 20 caractères")]
        public string? CodeBarre { get; set; }

        [Required(ErrorMessage = "La valeur d'acquisition est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "La valeur d'acquisition doit être positive")]
        public decimal ValeurAcquisition { get; set; }

        [Required(ErrorMessage = "Le taux d'amortissement est obligatoire")]
        [Range(0, 100, ErrorMessage = "Le taux d'amortissement doit être entre 0 et 100")]
        public decimal TauxAmortissement { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être positive")]
        public int? Quantite { get; set; }

        [StringLength(20, ErrorMessage = "Le statut ne peut pas dépasser 20 caractères")]
        public string? Statut { get; set; }

        public int? IdCategorie { get; set; }

        public DateTime? DateAcquisition { get; set; }
    }

}