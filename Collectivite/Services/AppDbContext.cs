using Collectivite.Models;
using Collectivite.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
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

        public DbSet<Engagement> Engagements { get; set; }
        public DbSet<Facture> Factures { get; set; }
        public DbSet<DetailsFacture> DetailsFactures { get; set; }
        public DbSet<Tiers> Tiers { get; set; }
        public DbSet<DocumentTiers> DocumentTiers { get; set; }
        public DbSet<CompteBancaire> CompteBancaires { get; set; }
        public DbSet<BonCommande> BonCommandes { get; set; }
        public DbSet<DetailBonCommande> DetailsBonCommandes { get; set; }
        public DbSet<Mandat> Mandats { get; set; }
        public DbSet<OrdreRecette> OrdreRecettes { get; set; }
        public DbSet<CompteComptable> CompteComptables { get; set; }
        public DbSet<EcritureComptable> EcritureComptables { get; set; }
        public DbSet<Mouvement> Mouvements { get; set; }
        public DbSet<ExpressionBesoin> ExpressionBesoins { get; set; }
        public DbSet<DetailExpressionBesoin> DetailExpressionBesoins { get; set; }




        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        // Constructeur par défaut pour les migrations
        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString;

                // Tentative de lecture depuis le registre (méthode sécurisée)
                if (RegistryManager.ConfigurationExists())
                {
                    try
                    {
                        var encryptedConnectionString = RegistryManager.GetConnectionString();
                        if (string.IsNullOrWhiteSpace(encryptedConnectionString))
                            throw new InvalidOperationException("La chaîne de connexion chiffrée est vide dans le registre.");

                        // Déchiffrement de la chaîne de connexion
                        connectionString = CryptoHelper.Decrypt(encryptedConnectionString);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Erreur lors de la lecture de la configuration sécurisée. " +
                            "Veuillez reconfigurer le serveur de base de données.", ex);
                    }
                }
                else
                {
                    // Fallback vers appsettings.json pour la compatibilité (développement uniquement)
                    // En production, cette section ne devrait jamais être utilisée
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .Build();

                    connectionString = configuration?.GetConnectionString("DefaultConnection");

                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException(
                            "Aucune configuration de base de données trouvée. " +
                            "Veuillez configurer le serveur de base de données.");
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("La chaîne de connexion est vide.");

                optionsBuilder.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );
                    });
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
                .OnDelete(DeleteBehavior.Cascade);

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
                .HasForeignKey<DetailCommune>(d => d.ExerciceId) 
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetailCommune>()
                .HasIndex(d => d.ExerciceId)
                .IsUnique();



            // ══════════════════════════════════════════════════════════════
            // CONFIGURATION BudgetLine → Remaniement (One-to-Many)
            // ══════════════════════════════════════════════════════════════
            modelBuilder.Entity<Remaniement>()
                .HasOne(r => r.BudgetLine)
                .WithMany(bl => bl.Remaniements)
                .HasForeignKey(bl => bl.IdBudgetLine);

            // ════════════════════════════════════════════════════════
            // 8️⃣ Tiers ↔ CompteBancaire / Factures (1 → N)
            // Un Tiers peut avoir plusieurs comptes et factures.
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
        

            // ════════════════════════════════════════════════════════
            // 9️⃣ Facture ↔ DetailsFacture (1 → N)
            // Une facture contient plusieurs lignes de détails.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailsFacture>()
                .HasOne(df => df.Facture)
                .WithMany(f => f.Details)
                .HasForeignKey(df => df.FactureId)
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

            modelBuilder.Entity<EcritureComptable>()
                .HasOne(ec => ec.Mouvement)
                .WithMany()
                .HasForeignKey(ec => ec.MouvementId)
                .OnDelete(DeleteBehavior.Cascade);

            // COMPTE COMPTABLE
            modelBuilder.Entity<CompteComptable>()
                .HasOne(c => c.ContrePartie)
                .WithMany(c => c.SousComptes)
                .HasForeignKey(c => c.ContrePartieId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // ROLES & PERMISSIONS
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- RELATIONS MOUVEMENT/MANDAT/ORDRE_RECETTE/COMPTECOMPTABLES---
            modelBuilder.Entity<Mouvement>().ToTable("mouvement");
            // Mouvement → CompteComptable (obligatoire)
            modelBuilder.Entity<Mouvement>()
                .HasOne(m => m.CompteComptable)
                .WithMany() // ou .WithMany(c => c.Mouvements)
                .HasForeignKey(m => m.idCompteComptable)
                .OnDelete(DeleteBehavior.Restrict);

            // Mouvement → OrdreRecette (optionnel)
            modelBuilder.Entity<Mouvement>()
                .HasOne(m => m.OrdreRecette)
                .WithMany() // ou .WithMany(o => o.Mouvements)
                .HasForeignKey(m => m.idOrdreRecette)
                .OnDelete(DeleteBehavior.SetNull);

            // Mouvement → Mandat (optionnel)
            modelBuilder.Entity<Mouvement>()
                .HasOne(m => m.Mandat)
                .WithMany() // ou .WithMany(md => md.Mouvements)
                .HasForeignKey(m => m.idMandat)
                .OnDelete(DeleteBehavior.SetNull);


            // ════════════════════════════════════════════════════════
            // 12️⃣ Expression de Besoin ↔ Exercice (N → 1)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<ExpressionBesoin>()
                .HasOne(eb => eb.Exercice)
                .WithMany()
                .HasForeignKey(eb => eb.ExerciceId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // 13️⃣ Expression de Besoin ↔ Détails (1 → N)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailExpressionBesoin>()
                .HasOne(deb => deb.ExpressionBesoin)
                .WithMany(eb => eb.Details)
                .HasForeignKey(deb => deb.ExpressionBesoinId)
                .OnDelete(DeleteBehavior.Cascade);

            // ════════════════════════════════════════════════════════
            // 14️⃣ Détail Expression de Besoin ↔ Nomenclature (N → 1)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<DetailExpressionBesoin>()
                .HasOne(deb => deb.Nommenclature)
                .WithMany()
                .HasForeignKey(deb => deb.NommenclatureId)
                .OnDelete(DeleteBehavior.Restrict);


            // ════════════════════════════════════════════════════════
            // BonCommande ↔ ExpressionBesoin (N → 1)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<BonCommande>()
                .HasOne(bc => bc.ExpressionBesoin)
                .WithMany()
                .HasForeignKey(bc => bc.ExpressionBesoinId)
                .OnDelete(DeleteBehavior.Restrict);

            // ════════════════════════════════════════════════════════
            // BonCommande ↔ Engagements (1 → N)
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Engagement>()
                .HasOne(e => e.BonCommande)
                .WithMany(bc => bc.Engagements)
                .HasForeignKey(e => e.BonCommandeId)
                .OnDelete(DeleteBehavior.SetNull);


            // ════════════════════════════════════════════════════════
            // Index unique sur le numéro
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<BonCommande>()
                .HasIndex(bc => bc.Numero)
                .IsUnique();

            // Ignorer les propriétés calculées
            modelBuilder.Entity<DetailBonCommande>()
                .Ignore(d => d.Total);


        }

    }
}