using GestionFournituresAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Agence> Agences { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Fourniture> Fournitures { get; set; } = null!;
        public DbSet<AgenceFourniture> AgenceFournitures { get; set; } = null!;
        public DbSet<UserAgence> UserAgences { get; set; } = null!;
        public DbSet<UserFourniture> UserFournitures { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des contraintes d'unicité
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Agence>()
                .HasIndex(a => a.Numero)
                .IsUnique();

            // Configuration de la relation many-to-many entre Agence et Fourniture
            modelBuilder.Entity<AgenceFourniture>()
                .HasOne(af => af.Agence)
                .WithMany(a => a.AgenceFournitures)
                .HasForeignKey(af => af.AgenceId);

            modelBuilder.Entity<AgenceFourniture>()
                .HasOne(af => af.Fourniture)
                .WithMany(f => f.AgenceFournitures)
                .HasForeignKey(af => af.FournitureId);

            // Configuration de la relation one-to-one entre User et Agence
            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.User)
                .WithOne(u => u.UserAgence)
                .HasForeignKey<UserAgence>(ua => ua.UserId);

            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.Agence)
                .WithMany(a => a.UserAgences)
                .HasForeignKey(ua => ua.AgenceId);

            // Configuration de la relation many-to-many entre User et Fourniture
            modelBuilder.Entity<UserFourniture>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.UserFournitures)
                .HasForeignKey(uf => uf.UserId);

            modelBuilder.Entity<UserFourniture>()
                .HasOne(uf => uf.Fourniture)
                .WithMany(f => f.UserFournitures)
                .HasForeignKey(uf => uf.FournitureId);
        }
    }
}