namespace GestionFournituresAPI.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Fonction { get; set; } = string.Empty;
    }
}