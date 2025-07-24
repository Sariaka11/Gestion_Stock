using GestionFournituresAPI.Models;
using GestionFournituresAPI.Dtos;
using System.Linq;

namespace GestionFournituresAPI.Services
{
    public interface IImmobilisationMappingService
    {
        ImmobilisationDto? ToDto(Immobilisation? immobilisation);
        Immobilisation? ToEntity(ImmobilisationCreateDto? createDto);
        void UpdateEntity(Immobilisation? entity, ImmobilisationUpdateDto? updateDto);
        List<ImmobilisationDto> ToDtoList(List<Immobilisation>? immobilisations);
    }

    public class ImmobilisationMappingService : IImmobilisationMappingService
    {
        public ImmobilisationDto? ToDto(Immobilisation? immobilisation)
        {
            if (immobilisation == null)
                return null;

            return new ImmobilisationDto
            {
                IdBien = immobilisation.IdBien,
                NomBien = immobilisation.NomBien,
                CodeBarre = immobilisation.CodeBarre,
                ValeurAcquisition = immobilisation.ValeurAcquisition,
                Quantite = immobilisation.Quantite,
                Statut = immobilisation.Statut,
                IdCategorie = immobilisation.IdCategorie,
                DateAcquisition = immobilisation.DateAcquisition,
                ValeurNetteComptable = immobilisation.ValeurNetteComptable,
                AmortissementCumule = immobilisation.AmortissementCumule,
                DureeRestante = immobilisation.DureeRestante,
                DateFinAmortissement = immobilisation.DateFinAmortissement,
                Categorie = immobilisation.Categorie != null ? new CategorieDto
                {
                    IdCategorie = immobilisation.Categorie.IdCategorie,
                    NomCategorie = immobilisation.Categorie.NomCategorie,
                    DureeAmortissement = immobilisation.Categorie.DureeAmortissement
                } : null,
                Amortissements = immobilisation.Amortissements?.Select(a => new AmortissementDto
                {
                    IdBien = a.IdBien,
                    Annee = a.Annee,
                    Montant = a.Montant,
                    ValeurResiduelle = a.ValeurResiduelle,
                    DateCalcul = a.DateCalcul
                }).ToList()
            };
        }

        public Immobilisation? ToEntity(ImmobilisationCreateDto? createDto)
        {
            if (createDto == null)
                return null;

            return new Immobilisation
            {
                NomBien = createDto.NomBien,
                CodeBarre = createDto.CodeBarre,
                ValeurAcquisition = createDto.ValeurAcquisition,
                Quantite = createDto.Quantite,
                Statut = createDto.Statut,
                IdCategorie = createDto.IdCategorie,
                DateAcquisition = createDto.DateAcquisition
            };
        }

        public void UpdateEntity(Immobilisation? entity, ImmobilisationUpdateDto? updateDto)
        {
            if (entity == null || updateDto == null)
                return;

            if (updateDto.NomBien != null)
                entity.NomBien = updateDto.NomBien;
            if (updateDto.CodeBarre != null)
                entity.CodeBarre = updateDto.CodeBarre;
            entity.ValeurAcquisition = updateDto.ValeurAcquisition;
            if (updateDto.Quantite.HasValue)
                entity.Quantite = updateDto.Quantite;
            if (updateDto.Statut != null)
                entity.Statut = updateDto.Statut;
            if (updateDto.IdCategorie.HasValue)
                entity.IdCategorie = updateDto.IdCategorie;
            if (updateDto.DateAcquisition.HasValue)
                entity.DateAcquisition = updateDto.DateAcquisition;
        }

        public List<ImmobilisationDto> ToDtoList(List<Immobilisation>? immobilisations)
        {
            if (immobilisations == null || !immobilisations.Any())
                return new List<ImmobilisationDto>();

            return immobilisations.Select(ToDto).Where(dto => dto != null).ToList()!;
        }
    }
}