using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestionFournituresAPI.Models.Import;
using GestionFournituresAPI.Services;

namespace GestionFournituresAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Ajoutez votre authentification si nécessaire
    public class ImportController : ControllerBase
    {
        private readonly IExcelImportService _importService;
        private readonly ILogger<ImportController> _logger;

        public ImportController(IExcelImportService importService, ILogger<ImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        /// <summary>
        /// Importe un fichier Excel unique
        /// </summary>
        [HttpPost("single")]
        [ProducesResponseType(typeof(ImportResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ImportSingleFile(
            [FromForm] IFormFile file,
            [FromForm] ImportFileType fileType,
            [FromForm] bool validateOnly = false,
            [FromForm] bool overwriteExisting = false)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier fourni" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Le fichier doit être au format Excel (.xlsx)" });
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var config = new ImportConfiguration
                {
                    ValidateOnly = validateOnly,
                    OverwriteExisting = overwriteExisting,
                    BatchSize = 100
                };

                var result = await _importService.ImportFromExcelAsync(stream, fileType, config);

                if (result.Success)
                {
                    _logger.LogInformation("Fichier {FileName} importé avec succès. {Imported} enregistrements importés",
                        file.FileName, result.Statistics.TotalRecordsImported);
                }
                else
                {
                    _logger.LogWarning("Importation du fichier {FileName} terminée avec {ErrorCount} erreurs",
                        file.FileName, result.Errors.Count);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation du fichier {FileName}", file.FileName);
                return StatusCode(500, new { message = "Erreur lors de l'importation", error = ex.Message });
            }
        }

        /// <summary>
        /// Importe plusieurs fichiers Excel en respectant l'ordre des dépendances
        /// </summary>
        [HttpPost("multiple")]
        [ProducesResponseType(typeof(ImportResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ImportMultipleFiles(
            [FromForm] List<IFormFile> files,
            [FromForm] bool validateOnly = false,
            [FromForm] bool overwriteExisting = false,
            [FromForm] bool stopOnError = true)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "Aucun fichier fourni" });
            }

            var fileStreams = new Dictionary<ImportFileType, Stream>();

            try
            {
                // Mapper les fichiers aux types d'importation
                foreach (var file in files)
                {
                    var fileType = DetermineFileType(file.FileName);
                    if (fileType.HasValue)
                    {
                        var stream = new MemoryStream();
                        await file.CopyToAsync(stream);
                        stream.Position = 0;
                        fileStreams[fileType.Value] = stream;
                    }
                    else
                    {
                        _logger.LogWarning("Type de fichier non reconnu : {FileName}", file.FileName);
                    }
                }

                var config = new ImportConfiguration
                {
                    ValidateOnly = validateOnly,
                    OverwriteExisting = overwriteExisting,
                    StopOnError = stopOnError,
                    BatchSize = 100
                };

                var result = await _importService.ImportMultipleFilesAsync(fileStreams, config);

                if (result.Success)
                {
                    _logger.LogInformation("Importation multiple réussie. {Imported} enregistrements importés au total",
                        result.Statistics.TotalRecordsImported);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation multiple");
                return StatusCode(500, new { message = "Erreur lors de l'importation", error = ex.Message });
            }
            finally
            {
                // Nettoyer les streams
                foreach (var stream in fileStreams.Values)
                {
                    stream?.Dispose();
                }
            }
        }

        /// <summary>
        /// Valide un fichier Excel sans l'importer
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ValidateFile(
            [FromForm] IFormFile file,
            [FromForm] ImportFileType fileType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier fourni" });
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var isValid = await _importService.ValidateExcelFileAsync(stream, fileType);

                return Ok(new
                {
                    isValid,
                    fileName = file.FileName,
                    fileType = fileType.ToString(),
                    message = isValid ? "Le fichier est valide" : "Le fichier contient des erreurs de format"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du fichier {FileName}", file.FileName);
                return StatusCode(500, new { message = "Erreur lors de la validation", error = ex.Message });
            }
        }

        /// <summary>
        /// Télécharge un template Excel vide pour un type donné
        /// </summary>
        [HttpGet("template/{fileType}")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public IActionResult DownloadTemplate(ImportFileType fileType)
        {
            try
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(fileType.ToString());

                // Ajouter les en-têtes selon le type
                var headers = ExcelColumnMapping.RequiredColumns[fileType];
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Ajouter quelques lignes d'exemple selon le type
                AddSampleData(worksheet, fileType);

                // Ajuster la largeur des colonnes
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Template_{fileType}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du template pour {FileType}", fileType);
                return StatusCode(500, new { message = "Erreur lors de la génération du template" });
            }
        }
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("ImportController is working!");
        }


        /// <summary>
        /// Retourne les types de fichiers supportés et leurs colonnes requises
        /// </summary>
        [HttpGet("file-types")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetSupportedFileTypes()
        {
            var fileTypes = Enum.GetValues<ImportFileType>()
                .Select(ft => new
                {
                    name = ft.ToString(),
                    value = (int)ft,
                    requiredColumns = ExcelColumnMapping.RequiredColumns[ft],
                    description = GetFileTypeDescription(ft)
                })
                .ToList();

            return Ok(new
            {
                fileTypes,
                importOrder = new[]
                {
                    "Agences", "Users", "Categories", "Fournitures", 
                    "Immobilisations", "EntreeFournitures", "Amortissements",
                    "UserAgence", "BienAgence", "AgenceFourniture", "UserFourniture"
                }
            });
        }

        private ImportFileType? DetermineFileType(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName).ToLower();
            
            return name switch
            {
                "agences" => ImportFileType.Agences,
                "users" or "utilisateurs" => ImportFileType.Users,
                "categories" => ImportFileType.Categories,
                "fournitures" => ImportFileType.Fournitures,
                "immobilisations" => ImportFileType.Immobilisations,
                "entreefournitures" or "entree_fournitures" => ImportFileType.EntreeFournitures,
                "amortissements" => ImportFileType.Amortissements,
                "useragence" or "user_agence" => ImportFileType.UserAgence,
                "bienagence" or "bien_agence" => ImportFileType.BienAgence,
                "agencefourniture" or "agence_fourniture" => ImportFileType.AgenceFourniture,
               
                _ => null
            };
        }

        private void AddSampleData(IXLWorksheet worksheet, ImportFileType fileType)
        {
            switch (fileType)
            {
                case ImportFileType.Agences:
            worksheet.Cell(1, 1).Value = "Id";
            worksheet.Cell(1, 2).Value = "Nom";
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = "Agence Paris";
            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = "Agence Lyon";
            break;

        case ImportFileType.Users:
            worksheet.Cell(1, 1).Value = "Id";
            worksheet.Cell(1, 2).Value = "Nom";
            worksheet.Cell(1, 3).Value = "Prenom";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "MotDePasse";
            worksheet.Cell(1, 6).Value = "Fonction";
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = "Dupont";
            worksheet.Cell(2, 3).Value = "Jean";
            worksheet.Cell(2, 4).Value = "jean.dupont@example.com";
            worksheet.Cell(2, 5).Value = "password123";
            worksheet.Cell(2, 6).Value = "Manager";
            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = "Martin";
            worksheet.Cell(3, 3).Value = "Sophie";
            worksheet.Cell(3, 4).Value = "sophie.martin@example.com";
            worksheet.Cell(3, 5).Value = "password456";
            worksheet.Cell(3, 6).Value = "Employé";
            break;

                case ImportFileType.Categories:
                    worksheet.Cell(2, 1).Value = "Matériel informatique";
                    worksheet.Cell(2, 2).Value = "3";
                    worksheet.Cell(2, 3).Value = "";
                    worksheet.Cell(3, 1).Value = "Ordinateur portable";
                    worksheet.Cell(3, 2).Value = "3";
                    worksheet.Cell(3, 3).Value = "Matériel informatique";
                    break;

                case ImportFileType.Fournitures:
                    worksheet.Cell(2, 1).Value = "Stylo bleu";
                    worksheet.Cell(2, 2).Value = 10;
                    worksheet.Cell(2, 3).Value = 100;
                    worksheet.Cell(2, 4).Value = "Matériel de bureau";
                    worksheet.Cell(3, 1).Value = "Cahier A4";
                    worksheet.Cell(3, 2).Value = 25;
                    worksheet.Cell(3, 3).Value = 50;
                    worksheet.Cell(3, 4).Value = "Matériel de bureau";
                    break;

                case ImportFileType.Immobilisations:
            worksheet.Cell(1, 1).Value = "NomBien";
            worksheet.Cell(1, 2).Value = "ValeurAcquisition";
            worksheet.Cell(1, 3).Value = "DateAcquisition";
            worksheet.Cell(1, 4).Value = "Categorie";
            worksheet.Cell(2, 1).Value = "Ordinateur portable";
            worksheet.Cell(2, 2).Value = 1500;
            worksheet.Cell(2, 3).Value = "2023-01-15";
            worksheet.Cell(2, 4).Value = "Matériel Informatique";
            worksheet.Cell(3, 1).Value = "Véhicule utilitaire";
            worksheet.Cell(3, 2).Value = 25000;
            worksheet.Cell(3, 3).Value = "2022-06-10";
            worksheet.Cell(3, 4).Value = "Véhicules";
            break;

        case ImportFileType.EntreeFournitures:
            worksheet.Cell(1, 1).Value = "QuantiteEntree";
            worksheet.Cell(1, 2).Value = "DateEntree";
            worksheet.Cell(1, 3).Value = "FournitureId";
            worksheet.Cell(2, 1).Value = 50;
            worksheet.Cell(2, 2).Value = "2023-03-20";
            worksheet.Cell(2, 3).Value = 1;
            worksheet.Cell(3, 1).Value = 100;
            worksheet.Cell(3, 2).Value = "2023-04-10";
            worksheet.Cell(3, 3).Value = 2;
            break;

        case ImportFileType.Amortissements:
            worksheet.Cell(1, 1).Value = "Annee";
            worksheet.Cell(1, 2).Value = "Montant";
            worksheet.Cell(1, 3).Value = "ValeurResiduelle";
            worksheet.Cell(1, 4).Value = "DateCalcul";
            worksheet.Cell(1, 5).Value = "IdBien";
            worksheet.Cell(2, 1).Value = 2023;
            worksheet.Cell(2, 2).Value = 500;
            worksheet.Cell(2, 3).Value = 1000;
            worksheet.Cell(2, 4).Value = "2023-12-31";
            worksheet.Cell(2, 5).Value = 1;
            worksheet.Cell(3, 1).Value = 2023;
            worksheet.Cell(3, 2).Value = 1000;
            worksheet.Cell(3, 3).Value = 1500;
            worksheet.Cell(3, 4).Value = "2023-12-31";
            worksheet.Cell(3, 5).Value = 2;
            break;

        case ImportFileType.UserAgence:
            worksheet.Cell(1, 1).Value = "UserId";
            worksheet.Cell(1, 2).Value = "AgenceId";
            worksheet.Cell(1, 3).Value = "DateAssociation";
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = 1;
            worksheet.Cell(2, 3).Value = "2023-01-01";
            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = 2;
            worksheet.Cell(3, 3).Value = "2023-02-01";
            break;

        case ImportFileType.BienAgence:
            worksheet.Cell(1, 1).Value = "IdBien";
            worksheet.Cell(1, 2).Value = "IdAgence";
            worksheet.Cell(1, 3).Value = "DateAffectation";
            worksheet.Cell(1, 4).Value = "Quantite";
            worksheet.Cell(1, 5).Value = "QuantiteConso";
            worksheet.Cell(1, 6).Value = "Fonction";
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = 1;
            worksheet.Cell(2, 3).Value = "2023-01-01";
            worksheet.Cell(2, 4).Value = 1;
            worksheet.Cell(2, 5).Value = 0.5;
            worksheet.Cell(2, 6).Value = "Usage bureautique";
            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = 2;
            worksheet.Cell(3, 3).Value = "2023-02-01";
            worksheet.Cell(3, 4).Value = 2;
            worksheet.Cell(3, 5).Value = 1.0;
            worksheet.Cell(3, 6).Value = "Transport";
            break;

        case ImportFileType.AgenceFourniture:
            worksheet.Cell(1, 1).Value = "FournitureId";
            worksheet.Cell(1, 2).Value = "AgenceId";
            worksheet.Cell(1, 3).Value = "Quantite";
            worksheet.Cell(1, 4).Value = "DateAssociation";
            worksheet.Cell(1, 5).Value = "ConsoMm";
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = 1;
            worksheet.Cell(2, 3).Value = 50;
            worksheet.Cell(2, 4).Value = "2023-01-01";
            worksheet.Cell(2, 5).Value = 10.5;
            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = 2;
            worksheet.Cell(3, 3).Value = 100;
            worksheet.Cell(3, 4).Value = "2023-02-01";
            worksheet.Cell(3, 5).Value = 20.0;
            break;
    
            default:
            throw new ArgumentException("Type de fichier non pris en charge.");
    
                // Ajouter d'autres exemples selon les besoins
            }
        }

        private string GetFileTypeDescription(ImportFileType fileType)
        {
            return fileType switch
            {
                ImportFileType.Agences => "Liste des agences",
                ImportFileType.Users => "Liste des utilisateurs",
                ImportFileType.Categories => "Catégories d'immobilisations",
                ImportFileType.Fournitures => "Liste des fournitures",
                ImportFileType.Immobilisations => "Immobilisations",
                ImportFileType.EntreeFournitures => "Entrées de fournitures",
                ImportFileType.Amortissements => "Tableau d'amortissement",
                ImportFileType.UserAgence => "Association utilisateurs-agences",
                ImportFileType.BienAgence => "Affectation des biens aux agences",
                ImportFileType.AgenceFourniture => "Fournitures par agence",
               
                _ => ""
            };
        }
    }
}