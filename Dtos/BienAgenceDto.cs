using System;

namespace GestionFournituresAPI.Dtos
{
    public class BienAgenceDto
    {
        public int IdBien { get; set; }
        public int IdAgence { get; set; }
        public string? NomBien { get; set; }
        public string? NomAgence { get; set; }
        public string? Categorie { get; set; }
        public int? Quantite { get; set; }
        public decimal? QuantiteConso { get; set; }
        public DateTime DateAffectation { get; set; }
        public ImmobilisationDto? Immobilisation { get; set; }
        public AgenceDto? Agence { get; set; }
    }

    public class ConsommationBienCreateDto
    {
        
        public int AgenceId { get; set; }
        public int BienId { get; set; }
        public decimal? QuantiteConso { get; set; }
        public DateTime DateAffectation { get; set; }
     }
}