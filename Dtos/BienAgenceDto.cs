namespace GestionFournituresAPI.Dtos
{
    public class BienAgenceDto
    {
        public int IdBien { get; set; }
        public int IdAgence { get; set; }

        public int? Quantite { get; set; }
        public DateTime DateAffectation { get; set; }
        public ImmobilisationDto? Immobilisation { get; set; }
        public AgenceDto? Agence { get; set; }
    }
}