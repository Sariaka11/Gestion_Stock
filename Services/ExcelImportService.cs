using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel; // Pour XLWorkbook et IXLWorksheet
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Pour ILogger
using GestionFournituresAPI.Models;
using GestionFournituresAPI.Models.Import;
using GestionFournituresAPI.Data; // Pour ApplicationDbContext
using BCrypt.Net;

namespace GestionFournituresAPI.Services
{
    public interface IExcelImportService
    {
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream, ImportFileType fileType, ImportConfiguration config = null);
        Task<ImportResult> ImportMultipleFilesAsync(Dictionary<ImportFileType, Stream> files, ImportConfiguration config = null);
        Task<bool> ValidateExcelFileAsync(Stream fileStream, ImportFileType fileType);
    }

    public class ExcelImportService : IExcelImportService
    {
        private readonly ApplicationDbContext _context; // Remplacez par le nom de votre DbContext
        private readonly ILogger<ExcelImportService> _logger;
        
        // Cache pour stocker les références pendant l'importation
        private readonly Dictionary<int, int> _userCache = new Dictionary<int, int>();
private readonly Dictionary<int, int> _agenceCache = new Dictionary<int, int>();
private readonly Dictionary<string, int> _categorieCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<string, int> _fournitureCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<int, int> _fournitureIdCache = new Dictionary<int, int>();
private readonly Dictionary<string, int> _immobilisationCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, int> _immobilisationIdCache = new Dictionary<int, int>();


        private async Task InitializeCaches()
        {
            _categorieCache.Clear();
            var categories = await _context.Categories.ToDictionaryAsync(c => c.NomCategorie, c => c.IdCategorie, StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categories) _categorieCache[cat.Key] = cat.Value;

            _fournitureCache.Clear();
            _fournitureIdCache.Clear();
            var fournitures = await _context.Fournitures.ToDictionaryAsync(f => f.Nom, f => f.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var f in fournitures)
            {
                _fournitureCache[f.Key] = f.Value;
                _fournitureIdCache[f.Value] = f.Value;
            }

            _immobilisationCache.Clear();
            _immobilisationIdCache.Clear();
            var immobilisations = await _context.Immobilisations.ToDictionaryAsync(i => i.NomBien, i => i.IdBien, StringComparer.OrdinalIgnoreCase);
            foreach (var i in immobilisations)
            {
                _immobilisationCache[i.Key] = i.Value;
                _immobilisationIdCache[i.Value] = i.Value;
            }

            _userCache.Clear();
            var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.Id);
            foreach (var u in users) _userCache[u.Key] = u.Value;

            _agenceCache.Clear();
            var agences = await _context.Agences.ToDictionaryAsync(a => a.Id, a => a.Id);
            foreach (var a in agences) _agenceCache[a.Key] = a.Value;

            _logger.LogInformation("Caches initialisés avec {CategoryCount} catégories, {FournitureCount} fournitures, {ImmobilisationCount} immobilisations, {UserCount} utilisateurs, {AgenceCount} agences.",
                _categorieCache.Count, _fournitureCache.Count, _immobilisationCache.Count, _userCache.Count, _agenceCache.Count);
        }


        public ExcelImportService(ApplicationDbContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
            LoadCaches().Wait();
        }

        private async Task LoadCaches()
        {
            try
            {
                _categorieCache.Clear();
                var categories = await _context.Categories.ToListAsync();
                foreach (var categorie in categories)
                {
                    _categorieCache[categorie.NomCategorie] = categorie.IdCategorie;
                }

                _fournitureCache.Clear();
                _fournitureIdCache.Clear();
                var fournitures = await _context.Fournitures.ToListAsync();
                foreach (var fourniture in fournitures)
                {
                    _fournitureCache[fourniture.Nom] = fourniture.Id;
                    _fournitureIdCache[fourniture.Id] = fourniture.Id;
                }

                _immobilisationCache.Clear();
                _immobilisationIdCache.Clear();
                var immobilisations = await _context.Immobilisations.ToListAsync();
                foreach (var immo in immobilisations)
                {
                    _immobilisationCache[immo.NomBien] = immo.IdBien;
                    _immobilisationIdCache[immo.IdBien] = immo.IdBien;
                }

                _userCache.Clear();
                var users = await _context.Users.ToListAsync();
                foreach (var user in users)
                {
                    _userCache[user.Id] = user.Id;
                }

                _agenceCache.Clear();
                var agences = await _context.Agences.ToListAsync();
                foreach (var agence in agences)
                {
                    _agenceCache[agence.Id] = agence.Id;
                }

                _logger.LogInformation("Caches chargés avec {CategoryCount} catégories, {FournitureCount} fournitures, {ImmobilisationCount} immobilisations, {UserCount} utilisateurs, {AgenceCount} agences.",
                    _categorieCache.Count, _fournitureCache.Count, _immobilisationCache.Count, _userCache.Count, _agenceCache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des caches");
            }
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, ImportFileType fileType, ImportConfiguration config = null)
        {
            config ??= new ImportConfiguration();
            var result = new ImportResult();

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);

                if (!ValidateHeaders(worksheet, fileType, result))
                {
                    return result;
                }

                switch (fileType)
                {
                    case ImportFileType.Agences:
                        await ImportAgences(worksheet, config, result);
                        break;
                    case ImportFileType.Users:
                        await ImportUsers(worksheet, config, result);
                        break;
                    case ImportFileType.Categories:
                        await ImportCategories(worksheet, config, result);
                        break;
                    case ImportFileType.Fournitures:
                        await ImportFournitures(worksheet, config, result);
                        break;
                    case ImportFileType.Immobilisations:
                        await ImportImmobilisations(worksheet, config, result);
                        break;
                    case ImportFileType.EntreeFournitures:
                        await ImportEntreeFournitures(worksheet, config, result);
                        break;
                    case ImportFileType.Amortissements:
                        await ImportAmortissements(worksheet, config, result);
                        break;
                    case ImportFileType.UserAgence:
                        await ImportUserAgences(worksheet, config, result);
                        break;
                    case ImportFileType.BienAgence:
                        await ImportBienAgences(worksheet, config, result);
                        break;
                    case ImportFileType.AgenceFourniture:
                        await ImportAgenceFournitures(worksheet, config, result);
                        break;
                }

                result.Success = result.Errors.Count == 0;
                result.Message = result.Success ? "Importation réussie" : "Importation terminée avec des erreurs";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation du fichier {FileType}", fileType);
                result.Success = false;
                result.Message = $"Erreur lors de l'importation : {ex.Message}";
            }

            return result;
        }

        private bool ValidateHeaders(IXLWorksheet worksheet, ImportFileType fileType, ImportResult result)
        {
            var requiredColumns = ExcelColumnMapping.RequiredColumns[fileType];
            var headerRow = worksheet.Row(1);
            var headers = new List<string>();

            var lastCell = headerRow.LastCellUsed();
            if (lastCell == null)
            {
                result.Errors.Add(new ImportError
                {
                    ErrorMessage = "Le fichier est vide ou n'a pas d'en-têtes",
                    FileName = fileType.ToString()
                });
                return false;
            }

            for (int col = 1; col <= lastCell.Address.ColumnNumber; col++)
            {
                headers.Add(headerRow.Cell(col).GetString().ToUpper());
            }

            foreach (var required in requiredColumns)
            {
                if (!headers.Contains(required.ToUpper()))
                {
                    result.Errors.Add(new ImportError
                    {
                        ErrorMessage = $"Colonne requise manquante : {required}",
                        FileName = fileType.ToString()
                    });
                }
            }

            return result.Errors.Count == 0;
        }

        private async Task ImportAgences(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des agences...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les agences.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les agences.");
                return;
            }

            var batch = new List<Agence>();
            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var id = row.Cell(1).GetValue<int>();
                    var nom = row.Cell(2).GetString();

                    if (string.IsNullOrWhiteSpace(nom))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Agences",
                            RowNumber = rowNumber,
                            ErrorMessage = "Le nom de l'agence est requis."
                        });
                        result.Statistics.TotalRecordsSkipped++;
                        rowNumber++;
                        continue;
                    }

                    if (!_agenceCache.ContainsKey(id))
                    {
                        var agence = new Agence
                        {
                            Id = id, // L'ID est fourni dans le fichier Excel
                            Nom = nom
                        };

                        if (!config.ValidateOnly)
                        {
                            batch.Add(agence);

                            if (batch.Count >= config.BatchSize)
                            {
                                await _context.Agences.AddRangeAsync(batch);
                                await _context.SaveChangesAsync();

                                foreach (var a in batch)
                                {
                                    _agenceCache[a.Id] = a.Id;
                                }

                                result.Statistics.TotalRecordsImported += batch.Count;
                                batch.Clear();
                            }
                        }
                    }
                    else if (!config.OverwriteExisting)
                    {
                        result.Statistics.TotalRecordsSkipped++;
                    }
                    else if (config.OverwriteExisting)
                    {
                        var existingAgence = await _context.Agences.FirstOrDefaultAsync(a => a.Id == id);
                        if (existingAgence != null)
                        {
                            existingAgence.Nom = nom;
                            if (!config.ValidateOnly)
                            {
                                _context.Agences.Update(existingAgence);
                                await _context.SaveChangesAsync();
                                result.Statistics.TotalRecordsImported++;
                            }
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "Agences",
                        RowNumber = rowNumber,
                        ErrorMessage = ex.Message
                    });

                    if (config.StopOnError)
                        break;
                }
                rowNumber++;
            }

            // Sauvegarder le dernier batch
            if (batch.Count > 0 && !config.ValidateOnly)
            {
                await _context.Agences.AddRangeAsync(batch);
                await _context.SaveChangesAsync();

                foreach (var a in batch)
                {
                    _agenceCache[a.Id] = a.Id;
                }

                result.Statistics.TotalRecordsImported += batch.Count;
            }

            result.Statistics.ImportedByTable["Agences"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des agences terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }


        private async Task ImportUsers(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des utilisateurs...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les utilisateurs.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les utilisateurs.");
                return;
            }

            var batch = new List<User>();
            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var id = row.Cell(1).GetValue<int>();
                    var nom = row.Cell(2).GetString();
                    var prenom = row.Cell(3).GetString();
                    var email = row.Cell(4).GetString();
                    var motDePasse = row.Cell(5).GetString();
                    var fonction = row.Cell(6).GetString();

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Users",
                            RowNumber = rowNumber,
                            ErrorMessage = "L'email, le nom et le prénom sont requis."
                        });
                        result.Statistics.TotalRecordsSkipped++;
                        rowNumber++;
                        continue;
                    }

                    if (!_userCache.ContainsKey(id))
                    {
                        var user = new User
                        {
                            Id = id, // L'ID est fourni dans le fichier Excel
                            Nom = nom,
                            Prenom = prenom,
                            Email = email,
                            MotDePasse = string.IsNullOrWhiteSpace(motDePasse) ? BCrypt.Net.BCrypt.HashPassword("default") : BCrypt.Net.BCrypt.HashPassword(motDePasse),
                            Fonction = fonction
                        };

                        if (!config.ValidateOnly)
                        {
                            batch.Add(user);

                            if (batch.Count >= config.BatchSize)
                            {
                                await _context.Users.AddRangeAsync(batch);
                                await _context.SaveChangesAsync();

                                foreach (var u in batch)
                                {
                                    _userCache[u.Id] = u.Id;
                                }

                                result.Statistics.TotalRecordsImported += batch.Count;
                                batch.Clear();
                            }
                        }
                    }
                    else if (!config.OverwriteExisting)
                    {
                        result.Statistics.TotalRecordsSkipped++;
                    }
                    else if (config.OverwriteExisting)
                    {
                        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (existingUser != null)
                        {
                            existingUser.Nom = nom;
                            existingUser.Prenom = prenom;
                            existingUser.Email = email;
                            existingUser.MotDePasse = string.IsNullOrWhiteSpace(motDePasse) ? existingUser.MotDePasse : BCrypt.Net.BCrypt.HashPassword(motDePasse);
                            existingUser.Fonction = fonction;

                            if (!config.ValidateOnly)
                            {
                                _context.Users.Update(existingUser);
                                await _context.SaveChangesAsync();
                                result.Statistics.TotalRecordsImported++;
                            }
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "Users",
                        RowNumber = rowNumber,
                        ErrorMessage = ex.Message
                    });

                    if (config.StopOnError)
                        break;
                }
                rowNumber++;
            }

            if (batch.Count > 0 && !config.ValidateOnly)
            {
                await _context.Users.AddRangeAsync(batch);
                await _context.SaveChangesAsync();

                foreach (var u in batch)
                {
                    _userCache[u.Id] = u.Id;
                }

                result.Statistics.TotalRecordsImported += batch.Count;
            }

            result.Statistics.ImportedByTable["Users"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des utilisateurs terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }

        private async Task ImportCategories(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            var range = worksheet.RangeUsed();
            if (range == null) return;
            
            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any()) return;
            
            int rowNumber = 2;
            
            // D'abord importer les catégories sans parent
            foreach (var row in rows)
            {
                var nomCategorieParent = row.Cell(3).GetString();
                if (string.IsNullOrWhiteSpace(nomCategorieParent))
                {
                    await ImportSingleCategory(row, null, config, result, rowNumber);
                }
                rowNumber++;
            }

            rowNumber = 2;
            // Ensuite importer les catégories avec parent
            foreach (var row in rows)
            {
                var nomCategorieParent = row.Cell(3).GetString();
                if (!string.IsNullOrWhiteSpace(nomCategorieParent))
                {
                    if (_categorieCache.TryGetValue(nomCategorieParent, out int parentId))
                    {
                        await ImportSingleCategory(row, parentId, config, result, rowNumber);
                    }
                    else
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Categories",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Catégorie parent '{nomCategorieParent}' introuvable"
                        });
                    }
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["Categories"] = result.Statistics.TotalRecordsImported;
        }

        private async Task ImportSingleCategory(IXLRangeRow row, int? parentId, ImportConfiguration config, ImportResult result, int rowNumber)
        {
            try
            {
                var nomCategorie = row.Cell(1).GetString();
                var duree = row.Cell(2).GetValue<int>();

                if (!_categorieCache.ContainsKey(nomCategorie))
                {
                    var categorie = new Categorie
                    {
                        NomCategorie = nomCategorie,
                        DureeAmortissement = duree,
                        ParentCategorieId = parentId  // Changé de IdParent à ID_PARENT
                    };

                    if (!config.ValidateOnly)
                    {
                        _context.Categories.Add(categorie);
                        await _context.SaveChangesAsync();
                        _categorieCache[nomCategorie] = categorie.IdCategorie;
                        result.Statistics.TotalRecordsImported++;
                    }
                }
                result.Statistics.TotalRecordsProcessed++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    FileName = "Categories",
                    RowNumber = rowNumber,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Implémentations des autres méthodes d'importation (simplifiées pour l'exemple)
        private async Task ImportFournitures(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des fournitures...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les fournitures.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList(); // Ignorer la première ligne (en-têtes)
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les fournitures.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    // Récupérer les valeurs des colonnes
                    var nom = row.Cell(1).GetString();
                    var prixUnitaire = row.Cell(2).GetValue<decimal>();
                    var quantiteRestante = row.Cell(3).GetValue<int>();
                    var nomCategorie = row.Cell(4).GetString();

                    // Valider les champs obligatoires
                    if (string.IsNullOrWhiteSpace(nom))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Fournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "Le nom de la fourniture est requis."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (prixUnitaire < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Fournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "Le prix unitaire doit être positif."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (quantiteRestante < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Fournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "La quantité restante doit être positive."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(nomCategorie))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Fournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "Le nom de la catégorie est requis."
                        });
                        rowNumber++;
                        continue;
                    }

                    // Vérifier si la fourniture existe déjà (par nom, pour éviter les doublons)
                    if (!_fournitureCache.ContainsKey(nom))
                    {
                        var fourniture = new Fourniture
                        {
                            Nom = nom,
                            PrixUnitaire = prixUnitaire,
                            QuantiteRestante = quantiteRestante,
                            Categorie = nomCategorie // Insérer directement la valeur texte
                        };

                        if (!config.ValidateOnly)
                        {
                            _context.Fournitures.Add(fourniture);
                            await _context.SaveChangesAsync();
                            _fournitureCache[nom] = fourniture.Id; // Mettre à jour le cache
                            result.Statistics.TotalRecordsImported++;
                        }
                    }
                    else if (config.OverwriteExisting)
                    {
                        // Mettre à jour la fourniture existante si OverwriteExisting est activé
                        var existingFourniture = await _context.Fournitures
                            .FirstOrDefaultAsync(f => f.Nom == nom);
                        if (existingFourniture != null)
                        {
                            existingFourniture.PrixUnitaire = prixUnitaire;
                            existingFourniture.QuantiteRestante = quantiteRestante;
                            existingFourniture.Categorie = nomCategorie;

                            if (!config.ValidateOnly)
                            {
                                _context.Fournitures.Update(existingFourniture);
                                await _context.SaveChangesAsync();
                                result.Statistics.TotalRecordsImported++;
                            }
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "Fournitures",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de la fourniture : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["Fournitures"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des fournitures terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }


       private async Task ImportImmobilisations(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
{
    _logger.LogInformation("Importation des immobilisations...");

    var range = worksheet.RangeUsed();
    if (range == null)
    {
        _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les immobilisations.");
        return;
    }

    var rows = range.RowsUsed().Skip(1).ToList();
    if (!rows.Any())
    {
        _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les immobilisations.");
        return;
    }

    int rowNumber = 2;

    foreach (var row in rows)
    {
        try
        {
            var nomBien = row.Cell(1).GetString();
            var valeurAcquisition = row.Cell(2).GetValue<decimal>();
            var dateAcquisition = row.Cell(3).GetValue<DateTime?>();
            var nomCategorie = row.Cell(4).GetString();

            // Valider les champs obligatoires
            if (string.IsNullOrWhiteSpace(nomBien))
            {
                result.Errors.Add(new ImportError
                {
                    FileName = "Immobilisations",
                    RowNumber = rowNumber,
                    ErrorMessage = "Le nom de l'immobilisation est requis."
                });
                rowNumber++;
                continue;
            }

            if (valeurAcquisition < 0)
            {
                result.Errors.Add(new ImportError
                {
                    FileName = "Immobilisations",
                    RowNumber = rowNumber,
                    ErrorMessage = "La valeur d'acquisition doit être positive."
                });
                rowNumber++;
                continue;
            }

            int? idCategorie = null;
            if (!string.IsNullOrWhiteSpace(nomCategorie))
            {
                if (!_categorieCache.TryGetValue(nomCategorie, out int catId))
                {
                    if (!config.ValidateOnly)
                    {
                        var newCategorie = new Categorie
                        {
                            NomCategorie = nomCategorie,
                            DureeAmortissement = 1 // Valeur par défaut
                        };
                        _context.Categories.Add(newCategorie);
                        await _context.SaveChangesAsync();
                        _categorieCache[nomCategorie] = newCategorie.IdCategorie;
                        idCategorie = newCategorie.IdCategorie;
                        _logger.LogInformation("Catégorie '{0}' ajoutée automatiquement.", nomCategorie);
                    }
                }
                else
                {
                    idCategorie = catId;
                }
            }

            if (!_immobilisationCache.ContainsKey(nomBien))
            {
                var immobilisation = new Immobilisation
                {
                    NomBien = nomBien,
                    ValeurAcquisition = valeurAcquisition,
                    DateAcquisition = dateAcquisition,
                    IdCategorie = idCategorie
                };

                if (!config.ValidateOnly)
                {
                    _context.Immobilisations.Add(immobilisation);
                    await _context.SaveChangesAsync();
                    _immobilisationCache[nomBien] = immobilisation.IdBien;
                    _immobilisationIdCache[immobilisation.IdBien] = immobilisation.IdBien;
                    result.Statistics.TotalRecordsImported++;
                }
            }
            else if (config.OverwriteExisting)
            {
                var existingImmobilisation = await _context.Immobilisations
                    .FirstOrDefaultAsync(i => i.NomBien == nomBien);
                if (existingImmobilisation != null)
                {
                    existingImmobilisation.ValeurAcquisition = valeurAcquisition;
                    existingImmobilisation.DateAcquisition = dateAcquisition;
                    existingImmobilisation.IdCategorie = idCategorie;

                    if (!config.ValidateOnly)
                    {
                        _context.Immobilisations.Update(existingImmobilisation);
                        await _context.SaveChangesAsync();
                        result.Statistics.TotalRecordsImported++;
                    }
                }
            }

            result.Statistics.TotalRecordsProcessed++;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError
            {
                FileName = "Immobilisations",
                RowNumber = rowNumber,
                ErrorMessage = $"Erreur lors de l'importation de l'immobilisation : {ex.Message}"
            });
        }
        rowNumber++;
    }

    result.Statistics.ImportedByTable["Immobilisations"] = result.Statistics.TotalRecordsImported;
    _logger.LogInformation("Importation des immobilisations terminée. {Imported} enregistrements importés.",
        result.Statistics.TotalRecordsImported);
}

        private async Task ImportEntreeFournitures(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des entrées de fournitures...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les entrées de fournitures.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les entrées de fournitures.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var quantiteEntree = row.Cell(1).GetValue<int>();
                    var dateEntree = row.Cell(2).GetValue<DateTime>();
                    var idFourniture = row.Cell(3).GetValue<int>();

                    if (quantiteEntree <= 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "EntreeFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "La quantité entrée doit être positive."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (dateEntree == default)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "EntreeFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "La date d'entrée est requise."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (!_fournitureIdCache.ContainsKey(idFourniture))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "EntreeFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Fourniture avec ID '{idFourniture}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    var entreeFourniture = new EntreeFourniture
                    {
                        QuantiteEntree = quantiteEntree,
                        DateEntree = dateEntree,
                        FournitureId = idFourniture
                    };

                    if (!config.ValidateOnly)
                    {
                        _context.EntreeFournitures.Add(entreeFourniture);
                        await _context.SaveChangesAsync();
                        result.Statistics.TotalRecordsImported++;
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "EntreeFournitures",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de l'entrée de fourniture : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["EntreeFournitures"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des entrées de fournitures terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }


        private async Task ImportAmortissements(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des amortissements...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les amortissements.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les amortissements.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var annee = row.Cell(1).GetValue<int>();
                    var montant = row.Cell(2).GetValue<decimal>();
                    var valeurResiduelle = row.Cell(3).GetValue<decimal>();
                    var dateCalcul = row.Cell(4).GetValue<DateTime>();
                    var idBien = row.Cell(5).GetValue<int>();

                    if (annee < 1900 || annee > DateTime.Now.Year)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Amortissements",
                            RowNumber = rowNumber,
                            ErrorMessage = "L'année doit être valide."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (montant < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Amortissements",
                            RowNumber = rowNumber,
                            ErrorMessage = "Le montant doit être positif."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (valeurResiduelle < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Amortissements",
                            RowNumber = rowNumber,
                            ErrorMessage = "La valeur résiduelle doit être positive."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (dateCalcul == default)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Amortissements",
                            RowNumber = rowNumber,
                            ErrorMessage = "La date de calcul est requise."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (!_immobilisationIdCache.ContainsKey(idBien))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "Amortissements",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Immobilisation avec ID '{idBien}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    var amortissement = new Amortissement
                    {
                        Annee = annee,
                        Montant = montant,
                        ValeurResiduelle = valeurResiduelle,
                        DateCalcul = dateCalcul,
                        IdBien = idBien
                    };

                    if (!config.ValidateOnly)
                    {
                        _context.Amortissements.Add(amortissement);
                        await _context.SaveChangesAsync();
                        result.Statistics.TotalRecordsImported++;
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "Amortissements",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de l'amortissement : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["Amortissements"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des amortissements terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }


        private async Task ImportUserAgences(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des associations user-agence...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les associations user-agence.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les associations user-agence.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var userId = row.Cell(1).GetValue<int>();
                    var agenceId = row.Cell(2).GetValue<int>();
                    var dateAssociation = row.Cell(3).GetValue<DateTime?>() ?? DateTime.Now;

                    if (!_userCache.ContainsKey(userId))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "UserAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Utilisateur avec ID '{userId}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (!_agenceCache.ContainsKey(agenceId))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "UserAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Agence avec ID '{agenceId}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    var existingUserAgence = await _context.UserAgences
                        .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AgenceId == agenceId);
                    if (existingUserAgence == null)
                    {
                        var userAgence = new UserAgence
                        {
                            UserId = userId,
                            AgenceId = agenceId,
                            DateAssociation = dateAssociation
                        };

                        if (!config.ValidateOnly)
                        {
                            _context.UserAgences.Add(userAgence);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }
                    else if (config.OverwriteExisting)
                    {
                        existingUserAgence.DateAssociation = dateAssociation;
                        if (!config.ValidateOnly)
                        {
                            _context.UserAgences.Update(existingUserAgence);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "UserAgences",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de l'association user-agence : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["UserAgences"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des associations user-agence terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }

        private async Task ImportBienAgences(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des associations bien-agence...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les associations bien-agence.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les associations bien-agence.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var idBien = row.Cell(1).GetValue<int>();
                    var idAgence = row.Cell(2).GetValue<int>();
                    var dateAffectation = row.Cell(3).GetValue<DateTime>();
                    var quantite = row.Cell(4).GetValue<int?>();
                    var quantiteConso = row.Cell(5).GetValue<decimal?>();
                    var fonction = row.Cell(6).GetString();

                    if (!_immobilisationIdCache.ContainsKey(idBien))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "BienAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Immobilisation avec ID '{idBien}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (!_agenceCache.ContainsKey(idAgence))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "BienAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Agence avec ID '{idAgence}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (dateAffectation == default)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "BienAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = "La date d'affectation est requise."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (quantite.HasValue && quantite <= 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "BienAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = "La quantité doit être positive si spécifiée."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (quantiteConso.HasValue && quantiteConso < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "BienAgences",
                            RowNumber = rowNumber,
                            ErrorMessage = "La quantité consommée doit être positive si spécifiée."
                        });
                        rowNumber++;
                        continue;
                    }

                    var existingBienAgence = await _context.BienAgences
                        .FirstOrDefaultAsync(ba => ba.IdBien == idBien && ba.IdAgence == idAgence);
                    if (existingBienAgence == null)
                    {
                        var bienAgence = new BienAgence
                        {
                            IdBien = idBien,
                            IdAgence = idAgence,
                            DateAffectation = dateAffectation,
                            Quantite = quantite,
                            QuantiteConso = quantiteConso,
                            Fonction = string.IsNullOrWhiteSpace(fonction) ? null : fonction
                        };

                        if (!config.ValidateOnly)
                        {
                            _context.BienAgences.Add(bienAgence);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }
                    else if (config.OverwriteExisting)
                    {
                        existingBienAgence.DateAffectation = dateAffectation;
                        existingBienAgence.Quantite = quantite;
                        existingBienAgence.QuantiteConso = quantiteConso;
                        existingBienAgence.Fonction = string.IsNullOrWhiteSpace(fonction) ? null : fonction;

                        if (!config.ValidateOnly)
                        {
                            _context.BienAgences.Update(existingBienAgence);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "BienAgences",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de l'association bien-agence : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["BienAgences"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des associations bien-agence terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }


        private async Task ImportAgenceFournitures(IXLWorksheet worksheet, ImportConfiguration config, ImportResult result)
        {
            _logger.LogInformation("Importation des associations agence-fourniture...");

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Aucune donnée trouvée dans la feuille Excel pour les associations agence-fourniture.");
                return;
            }

            var rows = range.RowsUsed().Skip(1).ToList();
            if (!rows.Any())
            {
                _logger.LogWarning("Aucune ligne de données trouvée dans la feuille Excel pour les associations agence-fourniture.");
                return;
            }

            int rowNumber = 2;

            foreach (var row in rows)
            {
                try
                {
                    var fournitureId = row.Cell(1).GetValue<int>();
                    var agenceId = row.Cell(2).GetValue<int>();
                    var quantite = row.Cell(3).GetValue<int>();
                    var dateAssociation = row.Cell(4).GetValue<DateTime?>() ?? DateTime.Now;
                    var consoMm = row.Cell(5).GetValue<decimal?>();

                    if (!_fournitureIdCache.ContainsKey(fournitureId))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "AgenceFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Fourniture avec ID '{fournitureId}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (!_agenceCache.ContainsKey(agenceId))
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "AgenceFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = $"Agence avec ID '{agenceId}' introuvable."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (quantite <= 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "AgenceFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "La quantité doit être positive."
                        });
                        rowNumber++;
                        continue;
                    }

                    if (consoMm.HasValue && consoMm < 0)
                    {
                        result.Errors.Add(new ImportError
                        {
                            FileName = "AgenceFournitures",
                            RowNumber = rowNumber,
                            ErrorMessage = "La consommation mensuelle doit être positive si spécifiée."
                        });
                        rowNumber++;
                        continue;
                    }

                    var existingAgenceFourniture = await _context.AgenceFournitures
                        .FirstOrDefaultAsync(af => af.FournitureId == fournitureId && af.AgenceId == agenceId);
                    if (existingAgenceFourniture == null)
                    {
                        var agenceFourniture = new AgenceFourniture
                        {
                            FournitureId = fournitureId,
                            AgenceId = agenceId,
                            Quantite = quantite,
                            DateAssociation = dateAssociation,
                            ConsoMm = consoMm
                        };

                        if (!config.ValidateOnly)
                        {
                            _context.AgenceFournitures.Add(agenceFourniture);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }
                    else if (config.OverwriteExisting)
                    {
                        existingAgenceFourniture.Quantite = quantite;
                        existingAgenceFourniture.DateAssociation = dateAssociation;
                        existingAgenceFourniture.ConsoMm = consoMm;

                        if (!config.ValidateOnly)
                        {
                            _context.AgenceFournitures.Update(existingAgenceFourniture);
                            await _context.SaveChangesAsync();
                            result.Statistics.TotalRecordsImported++;
                        }
                    }

                    result.Statistics.TotalRecordsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        FileName = "AgenceFournitures",
                        RowNumber = rowNumber,
                        ErrorMessage = $"Erreur lors de l'importation de l'association agence-fourniture : {ex.Message}"
                    });
                }
                rowNumber++;
            }

            result.Statistics.ImportedByTable["AgenceFournitures"] = result.Statistics.TotalRecordsImported;
            _logger.LogInformation("Importation des associations agence-fourniture terminée. {Imported} enregistrements importés.",
                result.Statistics.TotalRecordsImported);
        }




        public async Task<ImportResult> ImportMultipleFilesAsync(Dictionary<ImportFileType, Stream> files, ImportConfiguration config = null)
        {
            var result = new ImportResult();
            
            // Ordre d'importation pour respecter les contraintes
            var importOrder = new[]
            {
                ImportFileType.Agences,
                ImportFileType.Users,
                ImportFileType.Categories,
                ImportFileType.Fournitures,
                ImportFileType.Immobilisations,
                ImportFileType.EntreeFournitures,
                ImportFileType.Amortissements,
                ImportFileType.UserAgence,
                ImportFileType.BienAgence,
                ImportFileType.AgenceFourniture
            };

            foreach (var fileType in importOrder)
            {
                if (files.TryGetValue(fileType, out var stream))
                {
                    var fileResult = await ImportFromExcelAsync(stream, fileType, config);
                    
                    result.Statistics.TotalRecordsProcessed += fileResult.Statistics.TotalRecordsProcessed;
                    result.Statistics.TotalRecordsImported += fileResult.Statistics.TotalRecordsImported;
                    result.Statistics.TotalRecordsSkipped += fileResult.Statistics.TotalRecordsSkipped;
                    result.Errors.AddRange(fileResult.Errors);

                    if (!fileResult.Success && config?.StopOnError == true)
                    {
                        result.Success = false;
                        result.Message = $"Importation arrêtée à cause d'erreurs dans {fileType}";
                        return result;
                    }
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success ? "Importation complète réussie" : "Importation terminée avec des erreurs";
            
            return result;
        }

        public async Task<bool> ValidateExcelFileAsync(Stream fileStream, ImportFileType fileType)
        {
            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var result = new ImportResult();
                
                return ValidateHeaders(worksheet, fileType, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du fichier");
                return false;
            }
        }

        private DateTime ParseDate(string dateString)
        {
            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            throw new FormatException($"Format de date invalide : {dateString}");
        }
    }
}