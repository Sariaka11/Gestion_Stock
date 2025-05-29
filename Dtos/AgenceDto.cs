namespace GestionFournituresAPI.Dtos
{
    public class AgenceDto
    {
        public int Id { get; set; }
        public string? Nom { get; set; } // Supposé basé sur les modèles
        public string? Numero { get; set; } // Utilisé dans UsersController
    }
}