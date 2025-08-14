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

namespace GestionFournituresAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var builder = WebApplication.CreateBuilder(args);

            // Configuration CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Configuration des contrôleurs + JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Changé pour ignorer les cycles
                });

            // Connexion à la base de données Oracle
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseOracle(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                ));

            // Enregistrement des services
            builder.Services.AddScoped<AmortissementService>();
            builder.Services.AddScoped<IImmobilisationMappingService, ImmobilisationMappingService>();
            builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
            // Configuration d'AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Authentification avec cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                });

            var app = builder.Build();

            // Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}