using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Collectivite.Models;
using Collectivite.Services;

namespace Collectivite.Tests
{
    /// <summary>
    /// Classe de test pour diagnostiquer rapidement le problème de chargement
    /// À UTILISER TEMPORAIREMENT POUR LE DÉBOGAGE
    /// </summary>
    public static class TestRapideMouvement
    {
        /// <summary>
        /// Test complet du système de mouvements
        /// Appelez cette méthode depuis un bouton ou au chargement
        /// </summary>
        public static async Task<string> DiagnosticCompletAsync()
        {
            var rapport = new System.Text.StringBuilder();
            rapport.AppendLine("╔════════════════════════════════════════════════╗");
            rapport.AppendLine("║      DIAGNOSTIC SYSTÈME MOUVEMENTS            ║");
            rapport.AppendLine("╚════════════════════════════════════════════════╝");
            rapport.AppendLine();

            try
            {
                // ═══════════════════════════════════════
                // TEST 1 : Connexion à la base de données
                // ═══════════════════════════════════════
                rapport.AppendLine("TEST 1 : Connexion à la base de données");
                rapport.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                AppDbContext context = null;
                try
                {
                    context = new AppDbContext();
                    var canConnect = await context.Database.CanConnectAsync();

                    if (canConnect)
                    {
                        rapport.AppendLine("✓ Connexion réussie");
                    }
                    else
                    {
                        rapport.AppendLine("✗ Connexion échouée");
                        return rapport.ToString();
                    }
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ ERREUR : {ex.Message}");
                    return rapport.ToString();
                }
                rapport.AppendLine();

                // ═══════════════════════════════════════
                // TEST 2 : Vérification des tables
                // ═══════════════════════════════════════
                rapport.AppendLine("TEST 2 : Vérification des tables");
                rapport.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                try
                {
                    var nbMandats = await context.Mandats.CountAsync();
                    rapport.AppendLine($"✓ Table Mandats : {nbMandats} enregistrements");
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Table Mandats : ERREUR - {ex.Message}");
                }

                try
                {
                    var nbOrdres = await context.OrdreRecettes.CountAsync();
                    rapport.AppendLine($"✓ Table OrdreRecettes : {nbOrdres} enregistrements");
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Table OrdreRecettes : ERREUR - {ex.Message}");
                }

                try
                {
                    var nbMouvements = await context.Mouvements.CountAsync();
                    rapport.AppendLine($"✓ Table Mouvements : {nbMouvements} enregistrements");
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Table Mouvements : ERREUR - {ex.Message}");
                }

                try
                {
                    var nbTiers = await context.Tiers.CountAsync();
                    rapport.AppendLine($"✓ Table Tiers : {nbTiers} enregistrements");
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Table Tiers : ERREUR - {ex.Message}");
                }

                try
                {
                    var nbEngagements = await context.Engagements.CountAsync();
                    rapport.AppendLine($"✓ Table Engagements : {nbEngagements} enregistrements");
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Table Engagements : ERREUR - {ex.Message}");
                }
                rapport.AppendLine();

                // ═══════════════════════════════════════
                // TEST 3 : Test des relations
                // ═══════════════════════════════════════
                rapport.AppendLine("TEST 3 : Test des relations");
                rapport.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                try
                {
                    var mandatAvecRelations = await context.Mandats
                        .Include(m => m.Engagement)
                            .ThenInclude(e => e.Tiers)
                        .FirstOrDefaultAsync();

                    if (mandatAvecRelations != null)
                    {
                        rapport.AppendLine($"✓ Mandat chargé : {mandatAvecRelations.NumeroMandat}");
                        rapport.AppendLine($"  - Engagement : {(mandatAvecRelations.Engagement != null ? "OK" : "NULL")}");

                        if (mandatAvecRelations.Engagement != null)
                        {
                            rapport.AppendLine($"  - Tiers : {(mandatAvecRelations.Engagement.Tiers != null ? "OK" : "NULL")}");

                            if (mandatAvecRelations.Engagement.Tiers != null)
                            {
                                // Essayer différentes propriétés pour le nom
                                var nomTiers = mandatAvecRelations.Engagement.Tiers.NomComplet ??
                                               mandatAvecRelations.Engagement.Tiers.RaisonSociale ??
                                               "Propriété nom introuvable";
                                rapport.AppendLine($"  - Nom Tiers : {nomTiers}");
                            }
                        }
                    }
                    else
                    {
                        rapport.AppendLine("⚠ Aucun mandat trouvé pour tester les relations");
                    }
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Erreur test relations : {ex.Message}");
                }
                rapport.AppendLine();

                // ═══════════════════════════════════════
                // TEST 4 : Test du service MouvementService
                // ═══════════════════════════════════════
                rapport.AppendLine("TEST 4 : Service MouvementService");
                rapport.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                try
                {
                    var service = new MouvementService(context);
                    rapport.AppendLine("✓ Service créé");

                    // Test GetMandatsNonPayesAsync
                    var mandatsNonPayes = await service.GetMandatsNonPayesAsync();
                    rapport.AppendLine($"✓ GetMandatsNonPayesAsync : {mandatsNonPayes.Count} mandats");

                    if (mandatsNonPayes.Count > 0)
                    {
                        foreach (var m in mandatsNonPayes.Take(3))
                        {
                            rapport.AppendLine($"  - {m.NumeroMandat} : {m.MontantRestant:N0} GNF (Bénéficiaire: {m.Beneficiaire})");
                        }
                        if (mandatsNonPayes.Count > 3)
                        {
                            rapport.AppendLine($"  ... et {mandatsNonPayes.Count - 3} autres");
                        }
                    }

                    // Test GetOrdresRecetteNonEncaissesAsync
                    var ordresNonEncaisses = await service.GetOrdresRecetteNonEncaissesAsync();
                    rapport.AppendLine($"✓ GetOrdresRecetteNonEncaissesAsync : {ordresNonEncaisses.Count} ordres");

                    if (ordresNonEncaisses.Count > 0)
                    {
                        foreach (var o in ordresNonEncaisses.Take(3))
                        {
                            rapport.AppendLine($"  - {o.NumeroOrdre} : {o.MontantRestant:N0} GNF (Débiteur: {o.Debiteur})");
                        }
                        if (ordresNonEncaisses.Count > 3)
                        {
                            rapport.AppendLine($"  ... et {ordresNonEncaisses.Count - 3} autres");
                        }
                    }
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Erreur service : {ex.Message}");
                    rapport.AppendLine($"  StackTrace : {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        rapport.AppendLine($"  InnerException : {ex.InnerException.Message}");
                    }
                }
                rapport.AppendLine();

                // ═══════════════════════════════════════
                // TEST 5 : Calcul manuel des soldes
                // ═══════════════════════════════════════
                rapport.AppendLine("TEST 5 : Calcul manuel des soldes");
                rapport.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                try
                {
                    var mandatsAvecSolde = await context.Mandats
                        .Select(m => new
                        {
                            m.Id,
                            m.NumeroMandat,
                            m.MontantNet,
                            MontantPaye = context.Mouvements
                                .Where(mv => mv.idMandat == m.Id)
                                .Sum(mv => (decimal?)mv.Montant) ?? 0
                        })
                        .Where(x => x.MontantNet - x.MontantPaye > 0)
                        .ToListAsync();

                    rapport.AppendLine($"✓ Mandats avec solde > 0 : {mandatsAvecSolde.Count}");

                    foreach (var m in mandatsAvecSolde.Take(3))
                    {
                        rapport.AppendLine($"  - {m.NumeroMandat} : Net={m.MontantNet:N0}, Payé={m.MontantPaye:N0}, Restant={m.MontantNet - m.MontantPaye:N0}");
                    }
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Erreur calcul mandats : {ex.Message}");
                }

                try
                {
                    var ordresAvecSolde = await context.OrdreRecettes
                        .Select(o => new
                        {
                            o.Id,
                            o.NumeroOrdre,
                            o.MontantOrdre,
                            MontantEncaisse = context.Mouvements
                                .Where(mv => mv.idOrdreRecette == o.Id)
                                .Sum(mv => (decimal?)mv.Montant) ?? 0
                        })
                        .Where(x => x.MontantOrdre - x.MontantEncaisse > 0)
                        .ToListAsync();

                    rapport.AppendLine($"✓ Ordres avec solde > 0 : {ordresAvecSolde.Count}");

                    foreach (var o in ordresAvecSolde.Take(3))
                    {
                        rapport.AppendLine($"  - {o.NumeroOrdre} : Ordre={o.MontantOrdre:N0}, Encaissé={o.MontantEncaisse:N0}, Restant={o.MontantOrdre - o.MontantEncaisse:N0}");
                    }
                }
                catch (Exception ex)
                {
                    rapport.AppendLine($"✗ Erreur calcul ordres : {ex.Message}");
                }
                rapport.AppendLine();

                // ═══════════════════════════════════════
                // CONCLUSION
                // ═══════════════════════════════════════
                rapport.AppendLine("╔════════════════════════════════════════════════╗");
                rapport.AppendLine("║              DIAGNOSTIC TERMINÉ                ║");
                rapport.AppendLine("╚════════════════════════════════════════════════╝");

                context?.Dispose();
            }
            catch (Exception ex)
            {
                rapport.AppendLine();
                rapport.AppendLine("╔════════════════════════════════════════════════╗");
                rapport.AppendLine("║           ERREUR CRITIQUE                      ║");
                rapport.AppendLine("╚════════════════════════════════════════════════╝");
                rapport.AppendLine($"Message : {ex.Message}");
                rapport.AppendLine($"StackTrace : {ex.StackTrace}");
            }

            return rapport.ToString();
        }

        /// <summary>
        /// Affiche le diagnostic dans une MessageBox
        /// </summary>
        public static async Task AfficherDiagnosticAsync()
        {
            var resultat = await DiagnosticCompletAsync();

            // Écrire dans la console Debug
            Debug.WriteLine(resultat);

            // Afficher dans une MessageBox
            MessageBox.Show(resultat, "Diagnostic Système Mouvements",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Sauvegarde le diagnostic dans un fichier
        /// </summary>
        public static async Task SauvegarderDiagnosticAsync(string cheminFichier = null)
        {
            if (string.IsNullOrEmpty(cheminFichier))
            {
                cheminFichier = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Diagnostic_Mouvements_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                );
            }

            var resultat = await DiagnosticCompletAsync();

            await System.IO.File.WriteAllTextAsync(cheminFichier, resultat);

            MessageBox.Show($"Diagnostic sauvegardé dans :\n{cheminFichier}",
                "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);

            // Ouvrir le fichier
            Process.Start(new ProcessStartInfo(cheminFichier) { UseShellExecute = true });
        }
    }
}