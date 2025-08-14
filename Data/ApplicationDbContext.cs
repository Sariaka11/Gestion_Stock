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
        public DbSet<EntreeFourniture> EntreeFournitures { get; set; } = null!;
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

            modelBuilder.Entity<Agence>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd()
                      .HasAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");
            });

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
                .HasKey(ua => ua.Id);

            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAgences)
                .HasForeignKey(ua => ua.UserId);

            modelBuilder.Entity<UserAgence>()
                .HasOne(ua => ua.Agence)
                .WithMany(a => a.UserAgences)
                .HasForeignKey(ua => ua.AgenceId);

            // Configuration pour Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("NOTIFICATIONS", "SYSTEM");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .HasColumnName("ID")
                      .ValueGeneratedOnAdd()
                      .HasAnnotation("Oracle:UseSequenceName", "NOTIFICATIONS_CUSTOM_SEQ");
                entity.Property(e => e.UserId).HasColumnName("USER_ID");
                entity.Property(e => e.AgenceId).HasColumnName("AGENCE_ID");
                entity.Property(e => e.FournitureId).HasColumnName("FOURNITURE_ID");
                entity.Property(e => e.BienId).HasColumnName("BIEN_ID");
                entity.Property(e => e.Titre).HasColumnName("TITRE");
                entity.Property(e => e.Corps).HasColumnName("CORPS");
                entity.Property(e => e.DateDemande).HasColumnName("DATE_DEMANDE");
                entity.Property(e => e.Statut).HasColumnName("STATUT");
                entity.Ignore(e => e.UserName);

                entity.HasOne(n => n.Agence)
                      .WithMany(a => a.Notifications)
                      .HasForeignKey(n => n.AgenceId);

                entity.HasOne(n => n.Fourniture)
                      .WithMany(f => f.Notifications)
                      .HasForeignKey(n => n.FournitureId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(n => n.Immobilisation)
                      .WithMany(i => i.Notifications)
                      .HasForeignKey(n => n.BienId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Relation entre Immobilisation et Categorie
            modelBuilder.Entity<Immobilisation>()
                .HasOne(i => i.Categorie)
                .WithMany(c => c.Immobilisations)
                .HasForeignKey(i => i.IdCategorie)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration pour utiliser le trigger Oracle
            modelBuilder.Entity<Amortissement>()
                .Property(e => e.IdAmortissement)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID_AMORTISSEMENT");

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

            modelBuilder.Entity<Immobilisation>(entity =>
            {
                entity.ToTable("IMMOBILISATIONS", "SYSTEM");
                entity.Property(e => e.IdBien)
                      .HasColumnName("ID_BIEN")
                      .HasDefaultValueSql("SYSTEM.IMMOBILISATIONS_SEQ.NEXTVAL");
            });
        }
    }
}