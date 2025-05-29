namespace GestionFournituresAPI.Dtos
{
    public class UserAgenceDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AgenceId { get; set; }
        public DateTime DateAssociation { get; set; }
        public UserDto? User { get; set; }
        public AgenceDto? Agence { get; set; }
    }
}