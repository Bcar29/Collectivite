using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows.Media.Imaging;

namespace Collectivite.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<Commune> Communes { get; set; }
        public DbSet<Exercice> Exercices { get; set; }
        public DbSet<BudgetPrimitif> BudgetsPrimitifs { get; set; }
        public DbSet<BudgetLine> BudgetLines { get; set; }
        public DbSet<Nommenclature> Nommenclatures { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DetailCommune> DetailCommunes { get; set; }

        // Constructeur par défaut pour les migrations
        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Lire la configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Utiliser Pomelo pour MySQL
                optionsBuilder.UseMySql(connectionString,
                    ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1️ Relation Commune ↔ BudgetPrimitif (1 → N)
            //modelBuilder.Entity<BudgetPrimitif>()
            //    .HasOne(b => b.Commune)
            //    .WithMany(c => c.BudgetsPrimitifs)
            //    .HasForeignKey(b => b.CommuneId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //1️ Relation Commune ↔ DetailCommune(1 → N)
            modelBuilder.Entity<DetailCommune>()
                .HasOne(d => d.Commune)
                .WithMany(c => c.DetailCommunes)
                .HasForeignKey(b => b.IdCommune)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BudgetPrimitif>()
                .HasOne(b => b.Exercice)
                .WithOne(e => e.BudgetPrimitif)
                .HasForeignKey<BudgetPrimitif>(e => e.ExerciceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index unique pour garantir la relation One-to-One
            modelBuilder.Entity<BudgetPrimitif>()
                .HasIndex(e => e.ExerciceId)
                .IsUnique();


            // 3️ Relation BudgetPrimitif ↔ BudgetLine (1 → N)
            modelBuilder.Entity<BudgetLine>()
                .HasOne(bl => bl.BudgetPrimitif)
                .WithMany(bp => bp.BudgetLines)
                .HasForeignKey(bl => bl.BudgetPrimitifId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4️ Relation Nommenclature auto-référente (Parent ↔ Enfants)
            modelBuilder.Entity<Nommenclature>()
                .HasOne(n => n.Parent)
                .WithMany(n => n.Enfants)
                .HasForeignKey(n => n.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5️ Relation BudgetLine ↔ Nommenclature (1 → N)
            modelBuilder.Entity<BudgetLine>()
                .HasOne(bl => bl.Nommenclature)
                .WithMany()
                .HasForeignKey(bl => bl.NommenclatureId)
                .OnDelete(DeleteBehavior.Restrict);

            // 6 Relation BudgetLine ↔ Nommenclature (1 → N)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Commune)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CommuneId)
                .OnDelete(DeleteBehavior.Restrict);

            // ══════════════════════════════════════════════════════════════
            // CONFIGURATION EXERCICE → DETAILCOMMUNE (One-to-One)
            // ══════════════════════════════════════════════════════════════
            modelBuilder.Entity<Exercice>()
                .HasOne(e => e.DetailCommune)           // Exercice a UN DetailCommune
                .WithOne(d => d.Exercice)                // DetailCommune a UN Exercice
                .HasForeignKey<Exercice>(e => e.IdDetailCommune)  // Clé étrangère dans Exercice
                .OnDelete(DeleteBehavior.Restrict);      // Empêche la suppression en cascade

            // Index unique pour garantir la relation One-to-One
            modelBuilder.Entity<Exercice>()
                .HasIndex(e => e.IdDetailCommune)
                .IsUnique();
        }
    }
}