using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Utils
{
    public class SeedPlanComptable
    {
        private readonly AppDbContext _context;

        public SeedPlanComptable(AppDbContext context)
        {
            _context = context;
        }

        // Comptes par défaut (si vous voulez les garder en plus des nomenclatures)
        private static readonly CompteComptable[] defaultComptes =
        {
            new CompteComptable {NumeroCompte = "53" , IntituleCompte = "Banque"},
            new CompteComptable {NumeroCompte = "55" , IntituleCompte = "Caisse"},
            new CompteComptable {NumeroCompte = "61" , IntituleCompte = "Dettes"},
            new CompteComptable {NumeroCompte = "62" , IntituleCompte = "Salaires et traitements"},
            new CompteComptable {NumeroCompte = "63" , IntituleCompte = "Depenses courantes"},
            new CompteComptable {NumeroCompte = "64" , IntituleCompte = "Interventions à caractère economique et social"},
            new CompteComptable {NumeroCompte = "65" , IntituleCompte = "charges Exceptionnelle anterieurs et diverses"},
            new CompteComptable {NumeroCompte = "40" , IntituleCompte = "Fournisseurs et creanciers"},
            new CompteComptable {NumeroCompte = "41" , IntituleCompte = "Debiteurs"},
            new CompteComptable {NumeroCompte = "42" , IntituleCompte = "Personnel"},
            new CompteComptable {NumeroCompte = "43" , IntituleCompte = "Organismes sociaux"},
            new CompteComptable {NumeroCompte = "44" , IntituleCompte = "Etat-collect locales et Orgnaismes internes"},
            new CompteComptable {NumeroCompte = "46" , IntituleCompte = "Débiteurs et créditeurs divers"}, // NOUVEAU
        };

        /// <summary>
        /// Récupère toutes les nomenclatures sans enfants
        /// </summary>
        public async Task<List<Nommenclature>> GetNommenclaturesAsync()
        {
            return await _context.Nommenclatures
                .Where(n => n.Enfants == null || n.Enfants.Count == 0)
                .ToListAsync();
        }

        /// <summary>
        /// Initialise le plan comptable avec les nomenclatures et les comptes par défaut
        /// </summary>
        public async Task SeedCompteComptablesAsync()
        {
            try
            {
                // Vérifier si des comptes existent déjà
                var comptesExistants = await _context.CompteComptables.AnyAsync();
                if (comptesExistants)
                {
                    Console.WriteLine("Des comptes comptables existent déjà. Seed ignoré.");
                    return;
                }

                var comptesACreer = new List<CompteComptable>();

                // ========================================
                // ÉTAPE 1 : Insérer d'abord les comptes par défaut
                // ========================================
                Console.WriteLine("Insertion des comptes par défaut...");
                foreach (var defaultCompte in defaultComptes)
                {
                    comptesACreer.Add(new CompteComptable
                    {
                        NumeroCompte = defaultCompte.NumeroCompte,
                        IntituleCompte = defaultCompte.IntituleCompte,
                        ContrePartieId = null
                    });
                }

                // Sauvegarder les comptes par défaut en base pour obtenir leurs IDs
                await _context.CompteComptables.AddRangeAsync(comptesACreer);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{comptesACreer.Count} comptes par défaut créés.");

                // ========================================
                // ÉTAPE 2 : Récupérer les IDs des comptes de contrepartie
                // ========================================
                var compte40 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "40");
                var compte41 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "41");
                var compte42 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "42");
                var compte43 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "43");
                var compte44 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "44");
                var compte46 = await _context.CompteComptables.FirstOrDefaultAsync(c => c.NumeroCompte == "46");

                // Vérifier que tous les comptes de contrepartie existent
                if (compte40 == null || compte41 == null || compte42 == null ||
                    compte43 == null || compte44 == null)
                {
                    Console.WriteLine("⚠ Attention : Certains comptes de contrepartie sont manquants.");
                }

                // ========================================
                // ÉTAPE 3 : Créer les comptes depuis les nomenclatures avec contreparties
                // ========================================
                Console.WriteLine("Insertion des comptes depuis les nomenclatures...");
                var nomenclatures = await GetNommenclaturesAsync();
                var comptesNomenclature = new List<CompteComptable>();

                foreach (var nomenclature in nomenclatures)
                {
                    var codeNomenclature = nomenclature.CodeNomenclature;

                    // Vérifier qu'on n'a pas déjà un compte avec ce numéro (éviter doublons avec defaults)
                    var compteExiste = await _context.CompteComptables
                        .AnyAsync(c => c.NumeroCompte == codeNomenclature);

                    if (compteExiste)
                    {
                        Console.WriteLine($"  ⊳ Compte {codeNomenclature} existe déjà, ignoré.");
                        continue;
                    }

                    // Déterminer la contrepartie selon les règles
                    int? contrepartieId = DeterminerContrepartie(
                        codeNomenclature,
                        compte40?.Id,
                        compte41?.Id,
                        compte42?.Id,
                        compte43?.Id,
                        compte44?.Id,
                        compte46?.Id
                    );

                    var compte = new CompteComptable
                    {
                        NumeroCompte = codeNomenclature,
                        IntituleCompte = nomenclature.Intitule ?? string.Empty,
                        ContrePartieId = contrepartieId
                    };

                    comptesNomenclature.Add(compte);
                }

                // ========================================
                // ÉTAPE 4 : Sauvegarder les comptes des nomenclatures
                // ========================================
                if (comptesNomenclature.Any())
                {
                    await _context.CompteComptables.AddRangeAsync(comptesNomenclature);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"{comptesNomenclature.Count} comptes depuis les nomenclatures créés.");
                }

                Console.WriteLine($"✓ Total : {comptesACreer.Count + comptesNomenclature.Count} comptes comptables créés avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du seed des comptes comptables : {ex.Message}");
                Console.WriteLine($"Stack trace : {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Détermine l'ID de la contrepartie selon le code nomenclature
        /// </summary>
        /// <param name="codeNomenclature">Code de la nomenclature</param>
        /// <param name="compte40Id">ID du compte 40 (Fournisseurs)</param>
        /// <param name="compte41Id">ID du compte 41 (Débiteurs)</param>
        /// <param name="compte42Id">ID du compte 42 (Personnel)</param>
        /// <param name="compte43Id">ID du compte 43 (Organismes sociaux)</param>
        /// <param name="compte44Id">ID du compte 44 (État)</param>
        /// <param name="compte46Id">ID du compte 46</param>
        /// <returns>L'ID de la contrepartie ou null</returns>
        private int? DeterminerContrepartie(
            string codeNomenclature,
            int? compte40Id,
            int? compte41Id,
            int? compte42Id,
            int? compte43Id,
            int? compte44Id,
            int? compte46Id)
        {
            if (string.IsNullOrWhiteSpace(codeNomenclature))
                return null;

            // Règles de contrepartie :
            // Si commence par 2, 60, 61 ou 63 => Compte 40
            if (codeNomenclature.StartsWith("2") ||
                codeNomenclature.StartsWith("60") ||
                codeNomenclature.StartsWith("61") ||
                codeNomenclature.StartsWith("63") ||
                codeNomenclature.StartsWith("66"))
            {
                return compte40Id;
            }

            // Si commence par 62 => Compte 42
            if (codeNomenclature.StartsWith("62"))
            {
                return compte42Id;
            }

            // Si commence par 64 => Compte 43
            if (codeNomenclature.StartsWith("64"))
            {
                return compte43Id;
            }

            // Si commence par 65 ou 67 => Compte 46
            if (codeNomenclature.StartsWith("65") || codeNomenclature.StartsWith("67"))
            {
                return compte46Id;
            }

            // Si commence par 1 => Compte 44
            if (codeNomenclature.StartsWith("1"))
            {
                return compte44Id;
            }

            // Si commence par 7 => Compte 41
            if (codeNomenclature.StartsWith("7"))
            {
                return compte41Id;
            }

            // Aucune règle ne correspond
            return null;
        }
    }
}