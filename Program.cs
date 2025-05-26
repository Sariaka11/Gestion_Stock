using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GestionFournituresAPI.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using GestionFournituresAPI.Services;

namespace GestionFournituresAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Configuration CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // ✅ Configuration des contrôleurs + JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // ✅ Connexion à la base de données Oracle
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseOracle(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                ));

            // ✅ Enregistrement des services
            builder.Services.AddScoped<AmortissementService>();

            // ✅ Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ✅ Authentification avec cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.LoginPath = "/login"; // facultatif
                    options.AccessDeniedPath = "/access-denied"; // facultatif
                });

            var app = builder.Build();

            // ✅ Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication(); // doit venir avant Authorization
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
