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
        public string Categorie { get; set; } = string.Empty; // Ajout de la propriété Categorie
        public int Quantite { get; set; }
        public DateTime DateAssociation { get; set; }
    }
}