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
                throw new ArgumentException($"Bien avec ID {idBien} non trouv�");

            if (!bien.DateAcquisition.HasValue)
                throw new ArgumentException("La date d'acquisition est requise pour calculer l'amortissement");

            if (bien.Categorie == null)
                throw new ArgumentException("La cat�gorie est requise pour calculer l'amortissement");

            // Utiliser la dur�e d'amortissement de la cat�gorie
            int dureeAmortissement = bien.Categorie.DureeAmortissement;

            // Calculer le taux d'amortissement si non d�fini
            if (bien.TauxAmortissement == 0)
            {
                bien.TauxAmortissement = 100m / dureeAmortissement;
            }

            // Date d'acquisition
            DateTime dateAcquisition = bien.DateAcquisition.Value;

            // Ann�e d'acquisition
            int anneeAcquisition = dateAcquisition.Year;

            // Valeur d'acquisition
            decimal valeurAcquisition = bien.ValeurAcquisition;

            // Calculer le prorata temporis pour la premi�re ann�e
            decimal prorata = 1.0m;
            if (dateAcquisition.Month > 1 || dateAcquisition.Day > 1)
            {
                // Nombre de jours restants dans l'ann�e / nombre total de jours dans l'ann�e
                int joursRestants = (new DateTime(dateAcquisition.Year, 12, 31) - dateAcquisition).Days + 1;
                int joursAnnee = (new DateTime(dateAcquisition.Year, 12, 31) - new DateTime(dateAcquisition.Year, 1, 1)).Days + 1;
                prorata = (decimal)joursRestants / joursAnnee;
            }

            // Cr�er le tableau d'amortissement
            var tableauAmortissement = new List<Amortissement>();
            decimal valeurResiduelle = valeurAcquisition;

            for (int i = 0; i < dureeAmortissement; i++)
            {
                int annee = anneeAcquisition + i;

                // Calculer la dotation annuelle
                decimal dotation;
                if (i == 0)
                {
                    // Premi�re ann�e avec prorata temporis
                    dotation = Math.Round(valeurAcquisition * (bien.TauxAmortissement / 100) * prorata, 2);
                }
                else if (i == dureeAmortissement - 1)
                {
                    // Derni�re ann�e : on amortit le reste
                    dotation = valeurResiduelle;
                }
                else
                {
                    // Ann�es interm�diaires
                    dotation = Math.Round(valeurAcquisition * (bien.TauxAmortissement / 100), 2);
                }

                // Mettre � jour la valeur r�siduelle
                valeurResiduelle -= dotation;
                if (valeurResiduelle < 0) valeurResiduelle = 0;

                // Cr�er l'entr�e d'amortissement
                var amortissement = new Amortissement
                {
                    IdBien = idBien,
                    Annee = annee,
                    Montant = dotation,
                    ValeurResiduelle = valeurResiduelle,
                    DateCalcul = DateTime.Now
                };

                tableauAmortissement.Add(amortissement);

                // Si la valeur r�siduelle est nulle, on arr�te
                if (valeurResiduelle == 0) break;
            }

            return tableauAmortissement;
        }

        // Enregistrer le tableau d'amortissement en base de donn�es
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
