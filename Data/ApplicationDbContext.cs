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

        // Tables existantes
        public DbSet<Agence> Agences { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Fourniture> Fournitures { get; set; } = null!;
        public DbSet<AgenceFourniture> AgenceFournitures { get; set; } = null!;
        public DbSet<UserAgence> UserAgences { get; set; } = null!;
        public DbSet<UserFourniture> UserFournitures { get; set; } = null!;
        public DbSet<EntreeFourniture> EntreeFournitures { get; set; } = null!;

        // Nouvelles tables pour la gestion de l'immobilier
        public DbSet<Categorie> Categories { get; set; } = null!;
        public DbSet<Immobilisation> Immobilisations { get; set; } = null!;
        public DbSet<Amortissement> Amortissements { get; set; } = null!;
        public DbSet<BienAgence> BienAgences { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des contraintes d'unicité existantes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Agence>()
                .HasIndex(a => a.Numero)
                .IsUnique();

            // Configuration de la relation many-to-many entre Agence et Fourniture
            modelBuilder.Entity<AgenceFourniture>()
                .HasKey(af => af.Id);

            modelBuilder.Entity<AgenceFourniture>()
                .HasOne(af => af.Agence)
                .WithMany(a => a.AgenceFournitures)
                .HasForeignKey(af => af.AgenceId);

            modelBuilder.Entity<AgenceFourniture>()
                .HasOne(af => af.Fourniture)
                .WithMany(f => f.AgenceFournitures)
                .HasForeignKey(af => af.FournitureId);

            // Configuration de la relation many-to-many entre User et Agence
            modelBuilder.Entity<UserAgence>()
                .HasKey(ua => new { ua.UserId, ua.AgenceId });

            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAgences)
                .HasForeignKey(ua => ua.UserId);

            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.Agence)
                .WithMany(a => a.UserAgences)
                .HasForeignKey(ua => ua.AgenceId);

            // Configuration de la relation many-to-many entre User et Fourniture
            modelBuilder.Entity<UserFourniture>()
                .HasKey(uf => new { uf.UserId, uf.FournitureId });

            modelBuilder.Entity<UserFourniture>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.UserFournitures)
                .HasForeignKey(uf => uf.UserId);

            modelBuilder.Entity<UserFourniture>()
                .HasOne(uf => uf.Fourniture)
                .WithMany(f => f.UserFournitures)
                .HasForeignKey(uf => uf.FournitureId);

            // Relation entre Immobilisation et Categorie
            modelBuilder.Entity<Immobilisation>()
                .HasOne(i => i.Categorie)
                .WithMany(c => c.Immobilisations)
                .HasForeignKey(i => i.IdCategorie)
                .OnDelete(DeleteBehavior.SetNull);

            // Relation entre Amortissement et Immobilisation
            modelBuilder.Entity<Amortissement>()
                .HasOne(a => a.Immobilisation)
                .WithMany(i => i.Amortissements)
                .HasForeignKey(a => a.IdBien);

            // Contrainte d'unicité pour Amortissement
            modelBuilder.Entity<Amortissement>()
                .HasIndex(a => new { a.IdBien, a.Annee })
                .IsUnique();

            // Clé primaire composite pour BienAgence
            modelBuilder.Entity<BienAgence>()
                .HasKey(ba => new { ba.IdAgence, ba.IdBien });

            // Relations pour BienAgence
            modelBuilder.Entity<BienAgence>()
                .HasOne(ba => ba.Agence)
                .WithMany(a => a.BienAgences)
                .HasForeignKey(ba => ba.IdAgence);

            modelBuilder.Entity<BienAgence>()
                .HasOne(ba => ba.Immobilisation)
                .WithMany(i => i.BienAgences)
                .HasForeignKey(ba => ba.IdBien);

            modelBuilder.Entity<Notification>(entity =>
                {
                    // Spécifier explicitement le nom de la table en majuscules
                    entity.ToTable("NOTIFICATIONS");
                    
                    // Mapper explicitement les colonnes
                    entity.Property(e => e.Id).HasColumnName("ID");
                    entity.Property(e => e.UserId).HasColumnName("USER_ID");
                    entity.Property(e => e.AgenceId).HasColumnName("AGENCE_ID");
                    entity.Property(e => e.FournitureId).HasColumnName("FOURNITURE_ID");
                    entity.Property(e => e.BienId).HasColumnName("BIEN_ID");
                    entity.Property(e => e.Titre).HasColumnName("TITRE");
                    entity.Property(e => e.Corps).HasColumnName("CORPS");
                    entity.Property(e => e.DateDemande).HasColumnName("DATE_DEMANDE");
                    entity.Property(e => e.Statut).HasColumnName("STATUT");
                    
                    // Ignorer UserName s'il n'est pas dans la DB
                    entity.Ignore(e => e.UserName);
                });

            // Configuration pour Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Agence)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.AgenceId);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Fourniture)
                .WithMany(f => f.Notifications)
                .HasForeignKey(n => n.FournitureId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Immobilisation)
                .WithMany(i => i.Notifications)
                .HasForeignKey(n => n.BienId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration pour gérer les valeurs NULL dans Oracle
            modelBuilder.Entity<Immobilisation>()
                .Property(i => i.IdCategorie)
                .IsRequired(false);

            modelBuilder.Entity<Immobilisation>()
                .Property(i => i.Quantite)
                .IsRequired(false);

            modelBuilder.Entity<Immobilisation>()
                .Property(i => i.DateAcquisition)
                .IsRequired(false);

            modelBuilder.Entity<Immobilisation>()
                .Property(i => i.Statut)
                .IsRequired(false);
        }
    }
}