using GestionFournituresAPI.Dtos;

namespace GestionFournituresAPI.Dtos
{
    public class UserFournitureDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FournitureId { get; set; }
        public DateTime DateAssociation { get; set; }
        public UserDto? User { get; set; }
        public FournitureDto? Fourniture { get; set; }
    }
}