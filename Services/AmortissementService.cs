using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Services
{
    public class AmortissementService
    {
        private readonly ApplicationDbContext _context;

        public AmortissementService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Calculer le tableau d'amortissement pour un bien
        public async Task<List<Amortissement>> CalculerTableauAmortissement(int idBien)
        {
            var bien = await _context.Immobilisations
                .Include(i => i.Categorie)
                .FirstOrDefaultAsync(i => i.IdBien == idBien);

            if (bien == null)
                throw new ArgumentException($"Bien avec ID {idBien} non trouvé");

            if (!bien.DateAcquisition.HasValue)
                throw new ArgumentException("La date d'acquisition est requise pour calculer l'amortissement");

            if (bien.Categorie == null)
                throw new ArgumentException("La catégorie est requise pour calculer l'amortissement");

            // Utiliser la durée d'amortissement de la catégorie
            int dureeAmortissement = bien.Categorie.DureeAmortissement;

            // Calculer le taux d'amortissement si non défini
            if (bien.TauxAmortissement == 0)
            {
                bien.TauxAmortissement = 100m / dureeAmortissement;
            }

            // Date d'acquisition
            DateTime dateAcquisition = bien.DateAcquisition.Value;

            // Année d'acquisition
            int anneeAcquisition = dateAcquisition.Year;

            // Valeur d'acquisition
            decimal valeurAcquisition = bien.ValeurAcquisition;

            // Calculer le prorata temporis pour la première année
            decimal prorata = 1.0m;
            if (dateAcquisition.Month > 1 || dateAcquisition.Day > 1)
            {
                // Nombre de jours restants dans l'année / nombre total de jours dans l'année
                int joursRestants = (new DateTime(dateAcquisition.Year, 12, 31) - dateAcquisition).Days + 1;
                int joursAnnee = (new DateTime(dateAcquisition.Year, 12, 31) - new DateTime(dateAcquisition.Year, 1, 1)).Days + 1;
                prorata = (decimal)joursRestants / joursAnnee;
            }

            // Créer le tableau d'amortissement
            var tableauAmortissement = new List<Amortissement>();
            decimal valeurResiduelle = valeurAcquisition;

            for (int i = 0; i < dureeAmortissement; i++)
            {
                int annee = anneeAcquisition + i;

                // Calculer la dotation annuelle
                decimal dotation;
                if (i == 0)
                {
                    // Première année avec prorata temporis
                    dotation = Math.Round(valeurAcquisition * (bien.TauxAmortissement / 100) * prorata, 2);
                }
                else if (i == dureeAmortissement - 1)
                {
                    // Dernière année : on amortit le reste
                    dotation = valeurResiduelle;
                }
                else
                {
                    // Années intermédiaires
                    dotation = Math.Round(valeurAcquisition * (bien.TauxAmortissement / 100), 2);
                }

                // Mettre à jour la valeur résiduelle
                valeurResiduelle -= dotation;
                if (valeurResiduelle < 0) valeurResiduelle = 0;

                // Créer l'entrée d'amortissement
                var amortissement = new Amortissement
                {
                    IdBien = idBien,
                    Annee = annee,
                    Montant = dotation,
                    ValeurResiduelle = valeurResiduelle,
                    DateCalcul = DateTime.Now
                };

                tableauAmortissement.Add(amortissement);

                // Si la valeur résiduelle est nulle, on arrête
                if (valeurResiduelle == 0) break;
            }

            return tableauAmortissement;
        }

        // Enregistrer le tableau d'amortissement en base de données
        public async Task EnregistrerTableauAmortissement(List<Amortissement> tableau)
        {
            if (tableau == null || !tableau.Any())
                return;

            int idBien = tableau.First().IdBien;

            // Supprimer les amortissements existants pour ce bien
            var amortissementsExistants = await _context.Amortissements
                .Where(a => a.IdBien == idBien)
                .ToListAsync();

            if (amortissementsExistants.Any())
            {
                _context.Amortissements.RemoveRange(amortissementsExistants);
            }

            // Ajouter les nouveaux amortissements
            await _context.Amortissements.AddRangeAsync(tableau);
            await _context.SaveChangesAsync();
        }
    }
}
