using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GestionFournituresAPI.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using GestionFournituresAPI.Services;
using AutoMapper;
using GestionFournituresAPI.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;

namespace GestionFournituresAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // CONFIGURATION ORACLE IMPORTANTE
            // ==========================================
            
            // Configuration Oracle de base
            OracleConfiguration.TnsAdmin = null;
            // OracleConfiguration.OracleDataSources.Clear(); // Retiré car pas supporté
            
            // Configuration des timeouts Oracle
            OracleConfiguration.CommandTimeout = 120; // 2 minutes
            OracleConfiguration.FetchSize = 65536;
            OracleConfiguration.StatementCacheSize = 25;

            // ==========================================
            // CONFIGURATION LOGGING (pour déboguer)
            // ==========================================
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
            }

            // ==========================================
            // CONFIGURATION CORS
            // ==========================================
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",  // React
                            "http://localhost:5173",  // Vite
                            "http://localhost:5000"   // API elle-même
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // Important pour les cookies
                });
            });

            // ==========================================
            // CONFIGURATION DES CONTRÔLEURS + JSON
            // ==========================================
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Garde les noms de propriétés tels quels
                });

            // ==========================================
            // CONFIGURATION ENTITY FRAMEWORK + ORACLE
            // ==========================================
            
            // Récupération et correction de la connection string
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // IMPORTANT: Corriger le caractère spécial dans la connection string
            if (connectionString != null && connectionString.Contains("ORCLùPDB1"))
            {
                connectionString = connectionString.Replace("ORCLùPDB1", "ORCLPDB1");
                Console.WriteLine("⚠️ Connection string corrigée: ORCLùPDB1 -> ORCLPDB1");
            }
            
            // Ajout de paramètres de pooling et timeout si non présents
            if (connectionString != null && !connectionString.Contains("Connection Timeout"))
            {
                connectionString += ";Connection Timeout=120;Pooling=true;Min Pool Size=0;Max Pool Size=100";
            }
            
            // Log de la connection string (masquée)
            var maskedConnStr = connectionString?.Replace("Manager1$", "***");
            Console.WriteLine($"Connection String utilisée: {maskedConnStr}");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseOracle(connectionString, oracleOptions =>
                {
                    oracleOptions.CommandTimeout(120); // Timeout de 2 minutes
                    // oracleOptions.UseOracleSQLCompatibility("11"); // Retiré si pas supporté
                });
                
                // Active les logs détaillés en développement
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            // Configuration des timeouts HTTP
            builder.Services.AddHttpClient();

            // ==========================================
            // ENREGISTREMENT DES SERVICES
            // ==========================================
            builder.Services.AddScoped<AmortissementService>();
            builder.Services.AddScoped<IImmobilisationMappingService, ImmobilisationMappingService>();
            builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
            
            // Configuration d'AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ==========================================
            // AUTHENTIFICATION AVEC COOKIES
            // ==========================================
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax; // Changé de Strict à Lax pour CORS
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                });

            var app = builder.Build();

            // Déclarer logger à un niveau supérieur
            ILogger<Program> logger = null;

            // ==========================================
            // TEST DE CONNEXION AU DÉMARRAGE
            // ==========================================
            using (var scope = app.Services.CreateScope())
            {
                logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                try
                {
                    logger.LogInformation("========================================");
                    logger.LogInformation("Test de connexion à Oracle au démarrage...");
                    logger.LogInformation($"Heure: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    
                    // Test direct avec OracleConnection
                    try
                    {
                        using var testConnection = new OracleConnection(connectionString);
                        logger.LogInformation("Ouverture de la connexion Oracle...");
                        testConnection.Open();
                        logger.LogInformation("✅ Connexion Oracle directe réussie!");
                        
                        using var cmd = testConnection.CreateCommand();
                        cmd.CommandText = "SELECT USER FROM DUAL";
                        var user = cmd.ExecuteScalar();
                        logger.LogInformation($"✅ Utilisateur connecté: {user}");
                        
                        testConnection.Close();
                    }
                    catch (OracleException oex)
                    {
                        logger.LogError($"❌ Erreur Oracle: {oex.Message}");
                        logger.LogError($"   Code: {oex.Number}");
                    }
                    
                    // Test avec Entity Framework
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var canConnect = await context.Database.CanConnectAsync(cts.Token);
                    
                    if (canConnect)
                    {
                        logger.LogInformation("✅ Connexion Entity Framework réussie!");
                        
                        // Test de requête simple
                        try
                        {
                            var userCount = await context.Users.CountAsync(cts.Token);
                            logger.LogInformation($"✅ Nombre d'utilisateurs dans la base: {userCount}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"⚠️ Impossible de compter les utilisateurs: {ex.Message}");
                        }
                    }
                    else
                    {
                        logger.LogError("❌ Impossible de se connecter via Entity Framework");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Erreur lors de la connexion à Oracle");
                    logger.LogError($"Type d'erreur: {ex.GetType().Name}");
                    logger.LogError($"Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    // NE PAS arrêter l'application, juste logger l'erreur
                }
                finally
                {
                    logger.LogInformation("========================================");
                }
            }

            // ==========================================
            // CONFIGURATION DU PIPELINE
            // ==========================================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            // L'ordre est IMPORTANT !
            app.UseCors(); // CORS doit être avant Authentication
            
            // app.UseHttpsRedirection(); // Commenté si vous travaillez en HTTP
            
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            
            // ==========================================
            // ENDPOINT DE TEST
            // ==========================================
            app.MapGet("/api/test-connection", async (ApplicationDbContext context, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("Test de connexion via endpoint /api/test-connection");
                    
                    // Test direct Oracle
                    var directTestResult = "";
                    try
                    {
                        using var conn = new OracleConnection(connectionString);
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 'OK' FROM DUAL";
                        directTestResult = cmd.ExecuteScalar()?.ToString() ?? "NULL";
                        conn.Close();
                    }
                    catch (Exception ex)
                    {
                        directTestResult = $"Erreur: {ex.Message}";
                    }
                    
                    // Test EF Core
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var canConnect = await context.Database.CanConnectAsync(cts.Token);
                    
                    if (canConnect)
                    {
                        var userCount = await context.Users.CountAsync(cts.Token);
                        return Results.Ok(new 
                        { 
                            status = "Success",
                            message = "Connexion Oracle OK",
                            directTest = directTestResult,
                            userCount = userCount,
                            timestamp = DateTime.Now
                        });
                    }
                    
                    return Results.Ok(new 
                    { 
                        status = "Partial",
                        message = "EF Core ne peut pas se connecter",
                        directTest = directTestResult,
                        timestamp = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur dans test-connection");
                    return Results.Problem($"Error: {ex.Message}");
                }
            });
            
            app.MapControllers();

            // Utilisation de la même instance de logger
            logger.LogInformation($"Application démarrée sur {builder.Configuration["Urls"] ?? "http://localhost:5000"}");
            
            app.Run();
        }
    }
}