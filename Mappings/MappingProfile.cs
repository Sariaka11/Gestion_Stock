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
                .ForMember(dest => dest.IdBien, opt => opt.MapFrom(src => src.IdBien))
                .ForMember(dest => dest.Annee, opt => opt.MapFrom(src => src.Annee))
                .ForMember(dest => dest.Montant, opt => opt.MapFrom(src => src.Montant))
                .ForMember(dest => dest.ValeurResiduelle, opt => opt.MapFrom(src => src.ValeurResiduelle))
                .ForMember(dest => dest.DateCalcul, opt => opt.MapFrom(src => src.DateCalcul));

            // Mappage inverse pour AmortissementDto vers Amortissement
            CreateMap<AmortissementDto, Amortissement>()
                .ForMember(dest => dest.IdAmortissement, opt => opt.Ignore())
                .ForMember(dest => dest.IdBien, opt => opt.MapFrom(src => src.IdBien))
                .ForMember(dest => dest.Annee, opt => opt.MapFrom(src => src.Annee))
                .ForMember(dest => dest.Montant, opt => opt.MapFrom(src => src.Montant))
                .ForMember(dest => dest.ValeurResiduelle, opt => opt.MapFrom(src => src.ValeurResiduelle))
                .ForMember(dest => dest.DateCalcul, opt => opt.MapFrom(src => src.DateCalcul));

            // Mappage pour Immobilisation
            CreateMap<ImmobilisationCreateDto, Immobilisation>()
                .ForMember(dest => dest.Amortissements, opt => opt.Ignore())
                .ForMember(dest => dest.Categorie, opt => opt.Ignore());

            CreateMap<Immobilisation, ImmobilisationDto>()
                .ForMember(dest => dest.Categorie, opt => opt.MapFrom(src => src.Categorie))
                .ForMember(dest => dest.Amortissements, opt => opt.MapFrom(src => src.Amortissements));

            // Autres mappages
            CreateMap<BienAgence, BienAgenceDto>()
                .ForMember(dest => dest.Immobilisation, opt => opt.MapFrom(src => src.Immobilisation))
                .ForMember(dest => dest.Agence, opt => opt.MapFrom(src => src.Agence));
            CreateMap<BienAgenceDto, BienAgence>();
            CreateMap<Agence, AgenceDto>();
            CreateMap<UserAgence, UserAgenceDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Agence, opt => opt.MapFrom(src => src.Agence));
            CreateMap<User, UserDto>();
            CreateMap<Fourniture, FournitureDto>();
            CreateMap<Categorie, CategorieDto>();
        }
    }
}