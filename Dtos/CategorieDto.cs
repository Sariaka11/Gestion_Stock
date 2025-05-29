namespace GestionFournituresAPI.Dtos
{
    public class CategorieDto
    {
        public int IdCategorie { get; set; }
        public string NomCategorie { get; set; } = string.Empty;
        public int DureeAmortissement { get; set; }
    }
}