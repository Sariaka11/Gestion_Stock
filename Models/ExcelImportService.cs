using System;
using System.Collections.Generic;

namespace GestionFournituresAPI.Models.Import
{
    /// <summary>
    /// Modèle pour le résultat de l'importation
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ImportStatistics Statistics { get; set; }
        public List<ImportError> Errors { get; set; }
        
        public ImportResult()
        {
            Statistics = new ImportStatistics();
            Errors = new List<ImportError>();
        }
    }

    /// <summary>
    /// Statistiques d'importation
    /// </summary>
    public class ImportStatistics
    {
        public int TotalRecordsProcessed { get; set; }
        public int TotalRecordsImported { get; set; }
        public int TotalRecordsSkipped { get; set; }
        public Dictionary<string, int> ImportedByTable { get; set; }
        
        public ImportStatistics()
        {
            ImportedByTable = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Modèle pour les erreurs d'importation
    /// </summary>
    public class ImportError
    {
        public string FileName { get; set; }
        public int RowNumber { get; set; }
        public string ColumnName { get; set; }
        public string ErrorMessage { get; set; }
        public string Value { get; set; }
        public DateTime ErrorDate { get; set; }
        
        public ImportError()
        {
            ErrorDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Configuration d'importation
    /// </summary>
    public class ImportConfiguration
    {
        public bool ValidateOnly { get; set; } = false;
        public bool OverwriteExisting { get; set; } = false;
        public bool StopOnError { get; set; } = false;
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// DTOs pour l'importation Excel (sans IDs)
    /// </summary>
    public class AgenceImportDto
    {
        public string Numero { get; set; }
        public string Nom { get; set; }
    }

    public class UserImportDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        public string Fonction { get; set; }
    }

    public class CategorieImportDto
    {
        public string NomCategorie { get; set; }
        public int DureeAmortissement { get; set; }
        public string NomCategorieParent { get; set; }
    }

    public class FournitureImportDto
    {
        public string Nom { get; set; }
        public decimal PrixUnitaire { get; set; }
        public int QuantiteRestante { get; set; }
        public string Categorie { get; set; }
    }

    public class ImmobilisationImportDto
    {
        public string NomBien { get; set; }
        public string DateAcquisition { get; set; }
        public decimal ValeurAcquisition { get; set; }
        public string NomCategorie { get; set; }
        public int? DureePersonnalisee { get; set; }
        public string Statut { get; set; }
        public int Quantite { get; set; }
        public string CodeBarre { get; set; }
    }

    public class EntreeFournitureImportDto
    {
        public string NomFourniture { get; set; }
        public int QuantiteEntree { get; set; }
        public string DateEntree { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal Montant { get; set; }
    }

    public class AmortissementImportDto
    {
        public string NomBien { get; set; }
        public int Annee { get; set; }
        public decimal Montant { get; set; }
        public decimal ValeurResiduelle { get; set; }
        public string DateCalcul { get; set; }
    }

    public class UserAgenceImportDto
    {
        public string EmailUser { get; set; }
        public string NumeroAgence { get; set; }
        public string DateAssociation { get; set; }
    }

    public class BienAgenceImportDto
    {
        public string NomBien { get; set; }
        public string NumeroAgence { get; set; }
        public string DateAffectation { get; set; }
        public int? Quantite { get; set; }
        public int? QuantiteConso { get; set; }
        public string Fonction { get; set; }
    }

    public class AgenceFournitureImportDto
    {
        public string NumeroAgence { get; set; }
        public string NomFourniture { get; set; }
        public string DateAssociation { get; set; }
        public int? Quantite { get; set; }
        public int? ConsoMm { get; set; }
    }

    /// <summary>
    /// Enum pour les types de fichiers
    /// </summary>
    public enum ImportFileType
    {
        Agences,
        Users,
        Categories,
        Fournitures,
        Immobilisations,
        EntreeFournitures,
        Amortissements,
        UserAgence,
        BienAgence,
        AgenceFourniture
    }

    /// <summary>
    /// Mapping des colonnes Excel
    /// </summary>
    public static class ExcelColumnMapping
    {
        public static Dictionary<ImportFileType, List<string>> RequiredColumns = new()
        {
            [ImportFileType.Agences] = new List<string> { "NUMERO", "NOM" },
            [ImportFileType.Users] = new List<string> { "NOM", "PRENOM", "EMAIL", "MOT_DE_PASSE", "FONCTION" },
            [ImportFileType.Categories] = new List<string> { "NOM_CATEGORIE", "DUREE_AMORTISSEMENT" },
            [ImportFileType.Fournitures] = new List<string> { "NOM", "PRIX_UNITAIRE", "QUANTITE_RESTANTE", "CATEGORIE" },
            [ImportFileType.Immobilisations] = new List<string> { "NOM_BIEN", "DATE_ACQUISITION", "VALEUR_ACQUISITION", "NOM_CATEGORIE", "STATUT", "QUANTITE", "CODE_BARRE" },
            [ImportFileType.EntreeFournitures] = new List<string> { "NOM_FOURNITURE", "QUANTITE_ENTREE", "DATE_ENTREE", "PRIX_UNITAIRE", "MONTANT" },
            [ImportFileType.Amortissements] = new List<string> { "NOM_BIEN", "ANNEE", "MONTANT", "VALEUR_RESIDUELLE", "DATE_CALCUL" },
            [ImportFileType.UserAgence] = new List<string> { "EMAIL_USER", "NUMERO_AGENCE", "DATE_ASSOCIATION" },
            [ImportFileType.BienAgence] = new List<string> { "NOM_BIEN", "NUMERO_AGENCE", "DATE_AFFECTATION", "FONCTION" },
            [ImportFileType.AgenceFourniture] = new List<string> { "NUMERO_AGENCE", "NOM_FOURNITURE", "DATE_ASSOCIATION" },
        };
    }
}