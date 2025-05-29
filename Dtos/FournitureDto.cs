namespace GestionFournituresAPI.Dtos
{
    public class FournitureDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public int QuantiteRestante { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal CMUP { get; set; }
    }
}