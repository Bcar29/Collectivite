using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public class BudgetLineService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODE DE VALIDATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Vérifie si une BudgetLine peut être modifiée ou supprimée
        /// Conditions : Budget non validé + Nomenclature sans enfants
        /// </summary>
        private async Task<(bool CanModify, string ErrorMessage)> CanModifyBudgetLineAsync(int budgetLineId)
        {
            using var context = CreateContext();

            var line = await context.BudgetLines
                .Include(b => b.BudgetPrimitif)
                .Include(b => b.Nommenclature)
                .FirstOrDefaultAsync(b => b.Id == budgetLineId);

            if (line == null)
                return (false, "Ligne budgétaire introuvable.");

            // Vérification 1 : Budget primitif non validé
            if (line.BudgetPrimitif.Status == BudgetPrimitif.Statusbudget.VALIDATED)
                return (false, "❌ Impossible de modifier cette ligne : le budget est déjà validé.");

            // Vérification 2 : Nomenclature sans enfants (feuille)
            var hasChildren = await context.Nommenclatures
                .AnyAsync(n => n.ParentId == line.NommenclatureId);

            if (hasChildren)
                return (false, "❌ Impossible de modifier cette ligne : la nomenclature possède des enfants. Seules les lignes avec des nomenclatures feuilles peuvent être modifiées.");

            return (true, string.Empty);
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODE DE MODIFICATION (UPDATE)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Met à jour le montant d'une ligne budgétaire
        /// Vérifie que le budget n'est pas validé et que la nomenclature n'a pas d'enfants
        /// Puis recalcule les montants des parents
        /// </summary>
        public async Task<(bool Success, string Message, BudgetLine? Line)> UpdateBudgetLineAsync(
            int budgetLineId,
            decimal newMontantPrevu)
        {
            try
            {
                // Validation des permissions de modification
                var (canModify, errorMessage) = await CanModifyBudgetLineAsync(budgetLineId);
                if (!canModify)
                    return (false, errorMessage, null);

                using var context = CreateContext();
                var line = await context.BudgetLines
                    .Include(b => b.Nommenclature)
                    .Include(b => b.BudgetPrimitif)
                    .FirstOrDefaultAsync(b => b.Id == budgetLineId);

                if (line == null)
                    return (false, "❌ Ligne budgétaire introuvable.", null);

                // Validation du montant
                if (newMontantPrevu < 0)
                    return (false, "❌ Le montant prévu ne peut pas être négatif.", null);

                // Sauvegarder l'ancien montant pour le message
                var oldMontant = line.MontantPrevu;

                // Mise à jour des montants
                line.MontantPrevu = newMontantPrevu;
                line.MontantActu = newMontantPrevu;

                await context.SaveChangesAsync();
                var bp = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.Id == line.BudgetPrimitifId);

                if (bp != null)
                {
                    if (line.Nommenclature.Nature == NatureType.Recette)
                    {
                        bp.MontantRecette -= oldMontant;
                        bp.MontantRecette += line.MontantPrevu;
                        //bp.MontantTotal -= oldMontant;
                        //bp.MontantTotal += line.MontantPrevu;

                        if(line.Nommenclature.Section == SectionType.Fonctionnement)
                        {
                            var n662 = await context.BudgetLines
                            .FirstOrDefaultAsync(n => n.Nommenclature.Article == "662" && bp.Id == n.BudgetPrimitifId);

                            var n110 = await context.BudgetLines
                                .FirstOrDefaultAsync(n => n.Nommenclature.Article == "110" && bp.Id == n.BudgetPrimitifId);
                            if (n110 != null)
                            {
                                n110.MontantPrevu -= oldMontant * 0.6m;
                                n110.MontantPrevu += line.MontantPrevu * 0.6m;
                                await context.SaveChangesAsync();

                                await RecalculateAncestorsAsync(
                                    n110.NommenclatureId,
                                    n110.BudgetPrimitifId
                                );
                            }
                            if (n662 != null)
                            {
                                n662.MontantPrevu -= oldMontant * 0.6m;
                                n662.MontantPrevu += line.MontantPrevu * 0.6m;
                                await context.SaveChangesAsync();

                                await RecalculateAncestorsAsync(
                                    n662.NommenclatureId,
                                    n662.BudgetPrimitifId
                                );
                            }
                        }
                        await context.SaveChangesAsync();

                    }
                    else if (line.Nommenclature.Nature == NatureType.Depense)
                    {
                        bp.MontantDepense -= oldMontant;
                        bp.MontantDepense += line.MontantPrevu;
                        await context.SaveChangesAsync();
                    }


                }

                // Recalculer les ancêtres
                await RecalculateAncestorsAsync(line.NommenclatureId, line.BudgetPrimitifId);

                // Recharger la ligne avec les relations
                await context.Entry(line).ReloadAsync();

                return (true,
                    $" Ligne budgétaire mise à jour avec succès.\n" +
                    $"Nomenclature : {line.Nommenclature.Intitule}\n" +
                    $"Ancien montant : {oldMontant:N0} GNF\n" +
                    $"Nouveau montant : {newMontantPrevu:N0} GNF\n",
                    line);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erreur lors de la mise à jour : {ex.Message}", null);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODE DE SUPPRESSION (DELETE)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Supprime une ligne budgétaire
        /// Vérifie que le budget n'est pas validé et que la nomenclature n'a pas d'enfants
        /// Puis recalcule les montants des parents
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteBudgetLineAsync(int budgetLineId)
        {
            try
            {
                // Validation des permissions de suppression
                var (canModify, errorMessage) = await CanModifyBudgetLineAsync(budgetLineId);
                if (!canModify)
                    return (false, errorMessage);

                using var context = CreateContext();
                var line = await context.BudgetLines
                    .Include(b => b.Nommenclature)
                    .Include(b => b.BudgetPrimitif)
                    .FirstOrDefaultAsync(b => b.Id == budgetLineId);

                if (line == null)
                    return (false, "❌ Ligne budgétaire introuvable.");

                // Vérifier s'il y a des engagements ou recensements liés
                var hasEngagements = await context.Engagements
                    .AnyAsync(e => e.BudgetLineId == budgetLineId);

                var hasRecensements = await context.Recensements
                    .AnyAsync(r => r.BudgetLineId == budgetLineId);

                if (hasEngagements || hasRecensements)
                {
                    return (false,
                        "❌ Impossible de supprimer cette ligne budgétaire : " +
                        "elle est liée à des engagements ou des recensements.\n" +
                        "Veuillez d'abord supprimer les documents associés.");
                }

                // Sauvegarder les infos pour le message
                var nomenclatureLibelle = line.Nommenclature.Intitule;
                var montant = line.MontantPrevu;
                var nomenclatureId = line.NommenclatureId;
                var budgetPrimitifId = line.BudgetPrimitifId;

                // Suppression
                context.BudgetLines.Remove(line);
                await context.SaveChangesAsync();

                // Recalculer les ancêtres
                await RecalculateAncestorsAsync(nomenclatureId, budgetPrimitifId);

                return (true,
                    $"✅ Ligne budgétaire supprimée avec succès.\n" +
                    $"Nomenclature : {nomenclatureLibelle}\n" +
                    $"Montant supprimé : {montant:N0} GNF\n" +
                    $"Les montants des parents ont été recalculés.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erreur lors de la suppression : {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODES EXISTANTES (Inchangées)
        // ═══════════════════════════════════════════════════════════

        public async Task<List<BudgetLine>> GetBudgetLinesForBudgetPrimitifAsync(int budgetPrimitifId)
        {
            using var context = CreateContext();

            // ✅ Permettre le chargement même pour un exercice clôturé (affichage en lecture seule)
            // Le blocage des actions se fait au niveau de l'UI (boutons masqués/désactivés)
            return await context.BudgetLines
                .Include(b => b.Nommenclature)
                .ThenInclude(n => n.Enfants)
                .Include(b => b.Remaniements)
                .Where(b => b.BudgetPrimitifId == budgetPrimitifId)
                .ToListAsync();
        }

        // Méthode alternative qui utilise l'exercice courant
        public async Task<List<BudgetLine>> GetBudgetLinesForCurrentExerciceAsync()
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<BudgetLine>();
            }

            using var context = CreateContext();

            // Récupérer le budget primitif pour l'exercice courant
            var budgetPrimitif = await context.BudgetsPrimitifs
                .FirstOrDefaultAsync(b => b.ExerciceId == exerciceService.CurrentExercice.Id);

            if (budgetPrimitif == null)
            {
                return new List<BudgetLine>();
            }

            return await GetBudgetLinesForBudgetPrimitifAsync(budgetPrimitif.Id);
        }

        public async Task<List<BudgetLine>> GetDepenseForEngagement()
        {
            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null || exerciceService.CurrentExercice.EstCloture == true)
            {
                return new List<BudgetLine>();
            }

            // ⛔ Par sûreté, si jamais l'exercice courant est marqué comme clôturé,
            // on ne renvoie aucune ligne de dépense pour empêcher de nouvelles opérations.
            if (exerciceService.CurrentExercice.EstCloture)
            {
                return new List<BudgetLine>();
            }

            return await context.BudgetLines
                .Include(b => b.Nommenclature)
                .ThenInclude(n => n.Enfants)
                .Include(r => r.Remaniements)
                .Where(b => b.BudgetPrimitif.ExerciceId == exerciceService.CurrentExercice.Id && b.BudgetPrimitif.Status == BudgetPrimitif.Statusbudget.VALIDATED && b.Nommenclature.Nature == NatureType.Depense && (b.Nommenclature.Enfants == null || !b.Nommenclature.Enfants.Any()))
                .ToListAsync();
        }

        public async Task<bool> HasChildrenAsync(int nomenclatureId)
        {
            using var context = CreateContext();
            return await context.Nommenclatures.AnyAsync(n => n.ParentId == nomenclatureId);
        }

        public async Task<List<Nommenclature>> GetLeafNomenclaturesNotLinkedAsync(
            int budgetPrimitifId,
            NatureType nature,
            SectionType section)
        {
            using var context = CreateContext();
            var leafs = await context.Nommenclatures
                .Where(n => n.Nature == nature && n.Section == section)
                .Where(n => !context.Nommenclatures.Any(c => c.ParentId == n.Id))
                .ToListAsync();

            var linkedIds = await context.BudgetLines
                .Where(b => b.BudgetPrimitifId == budgetPrimitifId)
                .Select(b => b.NommenclatureId)
                .ToListAsync();

            return leafs.Where(n => !linkedIds.Contains(n.Id)).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODE DE CRÉATION (CREATE)
        // ═══════════════════════════════════════════════════════════

        public async Task<BudgetLine> CreateBudgetLineAsync(
            int budgetPrimitifId,
            int nomenclatureId,
            decimal montantPrevu)
        {
            if (await HasChildrenAsync(nomenclatureId))
                throw new InvalidOperationException(
                    "Impossible de créer une ligne pour une nomenclature ayant des enfants.");
            if (montantPrevu < 0)
                throw new InvalidOperationException("le montant de la prevision ne dois etre que positif");

            using var context = CreateContext();
            var exist = await context.BudgetLines
                .FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId &&
                                          b.NommenclatureId == nomenclatureId);

            if (exist != null)
                throw new InvalidOperationException(
                    "Une ligne budgétaire existe déjà pour cette nomenclature dans le budget.");

            var newLine = new BudgetLine
            {
                BudgetPrimitifId = budgetPrimitifId,
                NommenclatureId = nomenclatureId,
                MontantPrevu = montantPrevu,
                MontantActu = montantPrevu
            };

            context.BudgetLines.Add(newLine);
            await context.SaveChangesAsync();

            await RecalculateAncestorsAsync(nomenclatureId, budgetPrimitifId);

            // Charger les navigations nécessaires
            await context.Entry(newLine).Reference(b => b.Nommenclature).LoadAsync();
            await context.Entry(newLine).Reference(b => b.BudgetPrimitif).LoadAsync();

            if (newLine.Nommenclature.code() != "110" && newLine.Nommenclature.code() != "662")
            {
                if (newLine.Nommenclature.Nature == NatureType.Recette)
                {
                    newLine.BudgetPrimitif.MontantRecette += newLine.MontantPrevu;
                    //newLine.BudgetPrimitif.MontantTotal = newLine.BudgetPrimitif.MontantRecette;
                }
                else if (newLine.Nommenclature.Nature == NatureType.Depense)
                {
                    newLine.BudgetPrimitif.MontantDepense += newLine.MontantPrevu;
                }
            }

            // Sauvegarder les modifications sur le BudgetPrimitif
            await context.SaveChangesAsync();

            //recuperer les 60% de la prevision pour l'affecter au prelèvement 
            if (newLine.Nommenclature.Nature == NatureType.Recette && newLine.Nommenclature.Section == SectionType.Fonctionnement)
            {
                var N110 = await context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Article == "110");
                if (N110 != null)
                {
                    var B110 = await context.BudgetLines
                        .FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId &&
                                              b.NommenclatureId == N110.Id);
                    if (B110 == null)
                    {
                        await CreateBudgetLineAsync(budgetPrimitifId, N110.Id, (newLine.MontantPrevu * (decimal)0.6));
                    }
                    else
                    {
                        B110.MontantPrevu += (newLine.MontantPrevu * (decimal)0.6);
                        await context.SaveChangesAsync();

                    }
                     await RecalculateAncestorsAsync(N110.Id, budgetPrimitifId);
                }
                else
                {
                    MessageBox.Show("N110 est null");
                }

                var N662 = await context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Article == "662");
                if (N662 != null)
                {
                    var B662 = await context.BudgetLines
                        .FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId &&
                                              b.NommenclatureId == N662.Id);
                    if (B662 == null)
                    {
                        await CreateBudgetLineAsync(budgetPrimitifId, N662.Id, (newLine.MontantPrevu * (decimal)0.6));
                    }
                    else
                    {
                        B662.MontantPrevu += (newLine.MontantPrevu * (decimal)0.6);
                        await context.SaveChangesAsync();

                    }
                    await RecalculateAncestorsAsync(N662.Id, budgetPrimitifId);
                }
                else
                {
                    MessageBox.Show("N662 est null");
                }

            }

            return newLine;
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER : RECALCUL DES ANCÊTRES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Recalcule les montants de tous les parents jusqu'à la racine
        /// </summary>
        private async Task RecalculateAncestorsAsync(int childNomenclatureId, int budgetPrimitifId)
        {
            using var context = CreateContext();
            var child = await context.Nommenclatures
                .FirstOrDefaultAsync(n => n.Id == childNomenclatureId);

            if (child == null) return;

            var parentId = child.ParentId;

            while (parentId.HasValue)
            {
                // Récupérer tous les enfants de ce parent
                var childrenIds = await context.Nommenclatures
                    .Where(n => n.ParentId == parentId.Value)
                    .Select(n => n.Id)
                    .ToListAsync();

                // Calculer la somme des montants des enfants ayant une BudgetLine
                var sommeEnfants = await context.BudgetLines
                    .Where(b => b.BudgetPrimitifId == budgetPrimitifId &&
                                childrenIds.Contains(b.NommenclatureId))
                    .SumAsync(b => (decimal?)b.MontantPrevu) ?? 0;

                // Trouver ou créer la BudgetLine du parent
                var parentLine = await context.BudgetLines
                    .FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId &&
                                              b.NommenclatureId == parentId.Value);

                if (parentLine == null)
                {
                    parentLine = new BudgetLine
                    {
                        BudgetPrimitifId = budgetPrimitifId,
                        NommenclatureId = parentId.Value,
                        MontantPrevu = sommeEnfants,
                        MontantActu = sommeEnfants
                    };
                    context.BudgetLines.Add(parentLine);
                }
                else
                {
                    parentLine.MontantPrevu = sommeEnfants;
                    parentLine.MontantActu = sommeEnfants;
                }

                await context.SaveChangesAsync();

                // Monter d'un niveau
                var parent = await context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Id == parentId.Value);

                if (parent == null) break;
                parentId = parent.ParentId;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 NOUVELLES MÉTHODES POUR LA HIÉRARCHIE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Détermine le niveau hiérarchique d'une nomenclature
        /// 0 = Chapitre, 1 = Article, 2 = Paragraphe, 3 = Sous-paragraphe
        /// </summary>
        public int GetNomenclatureLevel(Nommenclature nomenclature)
        {
            if (!string.IsNullOrWhiteSpace(nomenclature.SousParagraphe))
                return 3;
            if (!string.IsNullOrWhiteSpace(nomenclature.Paragraphe))
                return 2;
            if (!string.IsNullOrWhiteSpace(nomenclature.Article))
                return 1;
            if (!string.IsNullOrWhiteSpace(nomenclature.Chapitre))
                return 0;
            return 0;
        }

        /// <summary>
        /// Récupère toutes les nomenclatures avec leur hiérarchie complète
        /// </summary>
        public async Task<List<Nommenclature>> GetNomenclaturesWithHierarchyAsync(
            NatureType nature,
            SectionType section)
        {
            using var context = CreateContext();
            return await context.Nommenclatures
                .Include(n => n.Parent)
                .Include(n => n.Enfants)
                .Where(n => n.Nature == nature && n.Section == section)
                .OrderBy(n => n.Chapitre)
                .ThenBy(n => n.Article)
                .ThenBy(n => n.Paragraphe)
                .ThenBy(n => n.SousParagraphe)
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODES DE SERVICE ULTRA-OPTIMISÉES
        // ═══════════════════════════════════════════════════════════

        private decimal SumBudgetLines(
            List<BudgetLine> lines,
            NatureType nature,
            SectionType section,
            Func<BudgetLine, decimal> selector)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == nature &&
                    bl.Nommenclature.Section == section &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(selector);
        }

        private decimal PrelevementFonctionnement(List<BudgetLine> lines, Func<BudgetLine, decimal> selector)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == NatureType.Recette &&
                    bl.Nommenclature.Section == SectionType.Fonctionnement &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(selector) * 0.6m;
        }

        // ─────────────────────────────────────────────────────────
        // RECETTE FONCTIONNEMENT (3 propriétés seulement)
        // ─────────────────────────────────────────────────────────
        public decimal RecetteFonctionnementPrevu(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Fonctionnement, bl => bl.MontantPrevu);

        public decimal RecetteFonctionnementRealise(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Fonctionnement, bl => bl.MontantRealise);

        public decimal RecetteFonctionnementEntreSortie(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Fonctionnement, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE FONCTIONNEMENT (3 propriétés seulement)
        // ─────────────────────────────────────────────────────────
        public decimal DepenseFonctionnementPrevu(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Fonctionnement, bl => bl.MontantPrevu);

        public decimal DepenseFonctionnementRealise(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Fonctionnement, bl => bl.MontantRealise);

        public decimal DepenseFonctionnementEntreSortie(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Fonctionnement, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE RÉEL FONCTIONNEMENT (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal TotalDepenseReelFonctionnementPrevu(List<BudgetLine> lines)
            => DepenseFonctionnementPrevu(lines) - PrelevementFonctionnement(lines, bl => bl.MontantPrevu);

        public decimal TotalDepenseReelFonctionnementRealise(List<BudgetLine> lines)
            => DepenseFonctionnementRealise(lines) - PrelevementFonctionnement(lines, bl => bl.MontantRealise);

        public decimal TotalDepenseReelFonctionnementEntreSortie(List<BudgetLine> lines)
            => DepenseFonctionnementEntreSortie(lines) - PrelevementFonctionnement(lines, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // RECETTE INVESTISSEMENT (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal RecetteInvestissementPrevu(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Investissement, bl => bl.MontantPrevu);

        public decimal RecetteInvestissementRealise(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Investissement, bl => bl.MontantRealise);

        public decimal RecetteInvestissementEntreSortie(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Investissement, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // RECETTE RÉEL INVESTISSEMENT (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal TotalRecetteReelInvestissementPrevu(List<BudgetLine> lines)
            => RecetteInvestissementPrevu(lines) - PrelevementFonctionnement(lines, bl => bl.MontantPrevu);

        public decimal TotalRecetteReelInvestissementRealise(List<BudgetLine> lines)
            => RecetteInvestissementRealise(lines) - PrelevementFonctionnement(lines, bl => bl.MontantRealise);

        public decimal TotalRecetteReelInvestissementEntreSortie(List<BudgetLine> lines)
            => RecetteInvestissementEntreSortie(lines) - PrelevementFonctionnement(lines, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE INVESTISSEMENT (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal DepenseInvestissementPrevu(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Investissement, bl => bl.MontantPrevu);

        public decimal DepenseInvestissementRealise(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Investissement, bl => bl.MontantRealise);

        public decimal DepenseInvestissementEntreSortie(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Investissement, bl => bl.MontantEntreSortie);

        // ─────────────────────────────────────────────────────────
        // TOTAUX GÉNÉRAUX RECETTES RÉELS (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal TotalGeneralRecetteReelPrevu(List<BudgetLine> lines)
            => RecetteFonctionnementPrevu(lines) + TotalRecetteReelInvestissementPrevu(lines);

        public decimal TotalGeneralRecetteReelRealise(List<BudgetLine> lines)
            => RecetteFonctionnementRealise(lines) + TotalRecetteReelInvestissementRealise(lines);

        public decimal TotalGeneralRecetteReelEntreSortie(List<BudgetLine> lines)
            => RecetteFonctionnementEntreSortie(lines) + TotalRecetteReelInvestissementEntreSortie(lines);

        // ─────────────────────────────────────────────────────────
        // TOTAUX GÉNÉRAUX DÉPENSES RÉELS (3 propriétés)
        // ─────────────────────────────────────────────────────────
        public decimal TotalGeneralDepenseReelPrevu(List<BudgetLine> lines)
            => TotalDepenseReelFonctionnementPrevu(lines) + DepenseInvestissementPrevu(lines);

        public decimal TotalGeneralDepenseReelRealise(List<BudgetLine> lines)
            => TotalDepenseReelFonctionnementRealise(lines) + DepenseInvestissementRealise(lines);

        public decimal TotalGeneralDepenseReelEntreSortie(List<BudgetLine> lines)
            => TotalDepenseReelFonctionnementEntreSortie(lines) + DepenseInvestissementEntreSortie(lines);

        // ─────────────────────────────────────────────────────────
        // RECETTE FONCTIONNEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal RecetteFonctionnementDefinitif(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Fonctionnement, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE FONCTIONNEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal DepenseFonctionnementDefinitif(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Fonctionnement, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE RÉEL FONCTIONNEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal TotalDepenseReelFonctionnementDefinitif(List<BudgetLine> lines)
            => DepenseFonctionnementDefinitif(lines) - PrelevementFonctionnement(lines, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // RECETTE INVESTISSEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal RecetteInvestissementDefinitif(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Recette, SectionType.Investissement, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // RECETTE RÉEL INVESTISSEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal TotalRecetteReelInvestissementDefinitif(List<BudgetLine> lines)
            => RecetteInvestissementDefinitif(lines) - PrelevementFonctionnement(lines, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // DÉPENSE INVESTISSEMENT - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal DepenseInvestissementDefinitif(List<BudgetLine> lines)
            => SumBudgetLines(lines, NatureType.Depense, SectionType.Investissement, bl => bl.MontantDefinitif);

        // ─────────────────────────────────────────────────────────
        // TOTAUX GÉNÉRAUX RECETTES RÉELS - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal TotalGeneralRecetteReelDefinitif(List<BudgetLine> lines)
            => RecetteFonctionnementDefinitif(lines) + TotalRecetteReelInvestissementDefinitif(lines);

        // ─────────────────────────────────────────────────────────
        // TOTAUX GÉNÉRAUX DÉPENSES RÉELS - DEFINITIF
        // ─────────────────────────────────────────────────────────
        public decimal TotalGeneralDepenseReelDefinitif(List<BudgetLine> lines)
            => TotalDepenseReelFonctionnementDefinitif(lines) + DepenseInvestissementDefinitif(lines);

    }


}