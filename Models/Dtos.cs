namespace GestionFournituresAPI.Models.Dtos
{
	public class AgenceFournitureDto
	{
		public int Id { get; set; }
		public int AgenceId { get; set; }
		public string? AgenceNom { get; set; }
		public int FournitureId { get; set; }
		public string? FournitureNom { get; set; }
		public int Quantite { get; set; }
		public DateTime DateAssociation { get; set; }
	}
}