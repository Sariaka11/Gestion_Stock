namespace GestionFournituresAPI.Dtos
{
    public class AmortissementDto
    {
        public int IdAmortissement { get; set; }
        public int IdBien { get; set; } // Reference to Immobilisation by ID, not object
        public int Annee { get; set; }
        public decimal Montant { get; set; }
        public decimal ValeurResiduelle { get; set; }
        public DateTime DateCalcul { get; set; }
        // Removed: public ImmobilisationDto? Immobilisation { get; set; }
    }
}