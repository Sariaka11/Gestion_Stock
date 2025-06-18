// Dtos/AgenceFournitureDto.cs (créez ou modifiez ce fichier si nécessaire)
using System;

namespace GestionFournituresAPI.Dtos
{
    public class AgenceFournitureDto
    {
        public int Id { get; set; }
        public int AgenceId { get; set; }
        public string AgenceNom { get; set; } = string.Empty;
        public int FournitureId { get; set; }
        public string FournitureNom { get; set; } = string.Empty;
        public string? Categorie { get; set; } = string.Empty;
        public decimal? ConsoMm { get; set; } // Ajouté pour corriger CS0117
        // Ajout de la propriété Categorie
        public int Quantite { get; set; }
        public DateTime DateAssociation { get; set; }
    }

    public class ConsommationCreateDto
    {
        public int AgenceId { get; set; }
        public int FournitureId { get; set; }
        public decimal? ConsoMm { get; set; }
        
    }
}