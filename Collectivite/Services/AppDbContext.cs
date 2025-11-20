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

        public DbSet<Remaniement> Remaniements { get; set; }

        public DbSet<Contrats> Contrats { get; set; }
        public DbSet<Engagement> Engagements { get; set; }
        public DbSet<Facture> Factures { get; set; }
        public DbSet<DetailsFacture> DetailsFactures { get; set; }
        public DbSet<Tiers> Tiers { get; set; }
        public DbSet<CompteBancaire> CompteBancaires { get; set; }
        public DbSet<BonCommande> BonCommandes { get; set; }
        public DbSet<DetailBonCommande> DetailsBonCommandes { get; set; }
        public DbSet<Mandat> Mandats { get; set; }
        public DbSet<Recensement>  Recensements { get; set; }
        public DbSet<OrdreRecette> OrdreRecettes { get; set; }
        public DbSet<CompteComptable> CompteComptables { get; set; }
        public DbSet<EcritureComptable> EcritureComptables { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

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
            // ════════════════════════════════════════════════════════
            // 1️⃣ Relation Commune ↔ DetailCommune (1 → N)
            // Une commune possède plusieurs détails.
            // La suppression d’une commune entraîne la suppression de ses détails.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailCommune>()
                .HasOne(d => d.Commune)
                .WithMany(c => c.DetailCommunes)
                .HasForeignKey(d => d.IdCommune)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 2️⃣ Relation Exercice ↔ BudgetPrimitif (1 ↔ 1)
            // Chaque exercice est lié à un seul budget primitif.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<BudgetPrimitif>()
                .HasOne(b => b.Exercice)
                .WithOne(e => e.BudgetPrimitif)
                .HasForeignKey<BudgetPrimitif>(b => b.ExerciceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BudgetPrimitif>()
                .HasIndex(b => b.ExerciceId)
                .IsUnique();

            // ════════════════════════════════════════════════════════
            // 3️⃣ Relation BudgetPrimitif ↔ BudgetLine (1 → N)
            // Un budget primitif contient plusieurs lignes budgétaires.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<BudgetLine>()
                .HasOne(bl => bl.BudgetPrimitif)
                .WithMany(bp => bp.BudgetLines)
                .HasForeignKey(bl => bl.BudgetPrimitifId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 4️⃣ Relation Nomenclature auto-référente (Parent ↔ Enfants)
            // Une nomenclature peut avoir une sous-nomenclature.
            // Suppression restreinte pour éviter la cascade infinie.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Nommenclature>()
                .HasOne(n => n.Parent)
                .WithMany(n => n.Enfants)
                .HasForeignKey(n => n.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 5️⃣ Relation BudgetLine ↔ Nomenclature (1 → N)
            // Chaque ligne budgétaire appartient à une nomenclature.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<BudgetLine>()
                .HasOne(bl => bl.Nommenclature)
                .WithMany()
                .HasForeignKey(bl => bl.NommenclatureId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 6️⃣ Relation User ↔ Commune (1 → N)
            // Une commune peut avoir plusieurs utilisateurs.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<User>()
                .HasOne(u => u.Commune)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CommuneId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 7️⃣ Relation Exercice ↔ DetailCommune (1 ↔ 1)
            // L’exercice est lié à un seul détail de commune.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Exercice>()
                .HasOne(e => e.DetailCommune)
                .WithOne(d => d.Exercice)
                .HasForeignKey<Exercice>(e => e.IdDetailCommune)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exercice>()
                .HasIndex(e => e.IdDetailCommune)
                .IsUnique();



            // ══════════════════════════════════════════════════════════════
            // CONFIGURATION BudgetLine → Remaniement (One-to-Many)
            // ══════════════════════════════════════════════════════════════
            modelBuilder.Entity<Remaniement>()
                .HasOne(r => r.BudgetLine)
                .WithMany(bl => bl.Remaniements)
                .HasForeignKey(bl => bl.IdBudgetLine);

            // ════════════════════════════════════════════════════════
            // 8️⃣ Tiers ↔ CompteBancaire / Contrats / Factures (1 → N)
            // Un Tiers peut avoir plusieurs comptes, contrats et factures.
            // ════════════════════════════════════════════════════════
            // Configuration Tiers
            modelBuilder.Entity<Tiers>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Nif);

                entity.HasMany(e => e.CompteBancaires)
                    .WithOne(e => e.Tiers)
                    .HasForeignKey(e => e.TiersId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration CompteBancaire
            modelBuilder.Entity<CompteBancaire>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IBAN).IsUnique();

                entity.HasOne(e => e.Tiers)
                    .WithMany(e => e.CompteBancaires)
                    .HasForeignKey(e => e.TiersId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        

        modelBuilder.Entity<Contrats>()
                .HasOne(c => c.Tiers)
                .WithMany(t => t.Contrats)
                .HasForeignKey(c => c.TiersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contrats>()
                .HasOne(c => c.Exercice)
                .WithMany(e => e.Contrats)
                .HasForeignKey(c => c.ExerciceId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 9️⃣ Facture ↔ DetailsFacture (1 → N)
            // Une facture contient plusieurs lignes de détails.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailsFacture>()
                .HasOne(df => df.Facture)
                .WithMany(f => f.Details)
                .HasForeignKey(df => df.FactureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Facture>()
                .HasOne(f => f.Contrats)
                .WithMany(c => c.Factures)
                .HasForeignKey(f => f.ContratId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 🔟 Engagements : multiples relations
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Engagement>()
                .HasOne(e => e.Exercice)
                .WithMany(ex => ex.Engagements)
                .HasForeignKey(e => e.ExerciceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Engagement>()
                .HasOne(e => e.Commune)
                .WithMany(c => c.Engagements)
                .HasForeignKey(e => e.CommuneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Engagement>()
                .HasOne(e => e.BudgetLine)
                .WithMany(bl => bl.Engagements)
                .HasForeignKey(e => e.BudgetLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Engagement>()
                .HasOne(e => e.Tiers)
                .WithMany(t => t.Engagements)
                .HasForeignKey(e => e.TiersId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 11️⃣ Bon de commande ↔ Détails (1 → N)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailBonCommande>()
                .HasOne(dbc => dbc.BonCommande)
                .WithMany(bc => bc.Details)
                .HasForeignKey(dbc => dbc.BonCommandeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 12️⃣ Mandat ↔ Engagement (1 → N)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Mandat>()
                .HasOne(m => m.Engagement)
                .WithOne(e => e.Mandat)
                .HasForeignKey<Mandat>(m => m.EngagementId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 13️⃣ Recensement ↔ (BudgetLine, Exercice, Commune, Tiers)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Recensement>()
                .HasOne(r => r.BudgetLine)
                .WithMany(bl => bl.Recensements)
                .HasForeignKey(r => r.BudgetLineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recensement>()
                .HasOne(r => r.Exercice)
                .WithMany(e => e.Recensements)
                .HasForeignKey(r => r.ExerciceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recensement>()
                .HasOne(r => r.Commune)
                .WithMany(c => c.Recensements)
                .HasForeignKey(r => r.CommuneId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recensement>()
                .HasOne(r => r.Tiers)
                .WithMany(t => t.Recensements)
                .HasForeignKey(r => r.TiersId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 14️⃣ OrdreRecette ↔ (BudgetLine, Exercice, Commune, Tiers)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<OrdreRecette>()
                .HasOne(or => or.BudgetLine)
                .WithMany()
                .HasForeignKey(or => or.BudgetLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdreRecette>()
                .HasOne(or => or.Exercice)
                .WithMany()
                .HasForeignKey(or => or.ExerciceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdreRecette>()
                .HasOne(or => or.Commune)
                .WithMany()
                .HasForeignKey(or => or.CommuneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdreRecette>()
                .HasOne(or => or.Tiers)
                .WithMany()
                .HasForeignKey(or => or.TiersId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 15️⃣ EcritureComptable ↔ Comptes / Mandats / OrdreRecette
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<EcritureComptable>()
                .HasOne(ec => ec.CompteDebit)
                .WithMany()
                .HasForeignKey(ec => ec.CompteDebitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EcritureComptable>()
                .HasOne(ec => ec.CompteCredit)
                .WithMany()
                .HasForeignKey(ec => ec.CompteCreditId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EcritureComptable>()
                .HasOne(ec => ec.OrdreRecette)
                .WithMany(or => or.EcritureComptables)
                .HasForeignKey(ec => ec.OrdreRecetteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EcritureComptable>()
                .HasOne(ec => ec.Mandat)
                .WithMany(m => m.EcritureComptables)
                .HasForeignKey(ec => ec.MandatId)
                .OnDelete(DeleteBehavior.Cascade);

            // COMPTE COMPTABLE
            modelBuilder.Entity<CompteComptable>()
                .HasOne(c => c.CompteParent)
                .WithMany(c => c.SousComptes)
                .HasForeignKey(c => c.CompteParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}