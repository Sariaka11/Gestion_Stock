using AutoMapper;
using GestionFournituresAPI.Models;
using GestionFournituresAPI.Dtos;

namespace GestionFournituresAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mappage pour Amortissement
            CreateMap<Amortissement, AmortissementDto>()
                .ForMember(dest => dest.IdAmortissement, opt => opt.MapFrom(src => src.IdAmortissement))
                .ForMember(dest => dest.IdBien, opt => opt.MapFrom(src => src.IdBien))
                .ForMember(dest => dest.Annee, opt => opt.MapFrom(src => src.Annee))
                .ForMember(dest => dest.Montant, opt => opt.MapFrom(src => src.Montant))
                .ForMember(dest => dest.ValeurResiduelle, opt => opt.MapFrom(src => src.ValeurResiduelle))
                .ForMember(dest => dest.DateCalcul, opt => opt.MapFrom(src => src.DateCalcul));

            // Mappage pour Immobilisation
            CreateMap<Immobilisation, ImmobilisationDto>()
                .ForMember(dest => dest.Categorie, opt => opt.MapFrom(src => src.Categorie))
                .ForMember(dest => dest.Amortissements, opt => opt.MapFrom(src => src.Amortissements));

            // Mappage pour BienAgence
            CreateMap<BienAgence, BienAgenceDto>()
                .ForMember(dest => dest.Immobilisation, opt => opt.MapFrom(src => src.Immobilisation))
                .ForMember(dest => dest.Agence, opt => opt.MapFrom(src => src.Agence));
            CreateMap<BienAgenceDto, BienAgence>();

            // Mappage pour Agence
            CreateMap<Agence, AgenceDto>();

            // Mappage pour UserAgence
            CreateMap<UserAgence, UserAgenceDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Agence, opt => opt.MapFrom(src => src.Agence));

            // Mappage pour User
            CreateMap<User, UserDto>();

            // Mappage pour Fourniture
            CreateMap<Fourniture, FournitureDto>();

            // Mappage pour Categorie
            CreateMap<Categorie, CategorieDto>();
        }
    }
}