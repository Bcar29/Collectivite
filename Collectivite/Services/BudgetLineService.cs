using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
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
            int newMontantPrevu)
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
                    $"✅ Ligne budgétaire mise à jour avec succès.\n" +
                    $"Nomenclature : {line.Nommenclature.Intitule}\n" +
                    $"Ancien montant : {oldMontant:N0} GNF\n" +
                    $"Nouveau montant : {newMontantPrevu:N0} GNF\n" +
                    $"Les montants des parents ont été recalculés.",
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

            if (exerciceService.CurrentExercice == null)
            {
                return new List<BudgetLine>();
            }
            return await context.BudgetLines
                .Include(b => b.Nommenclature)
                .ThenInclude(n => n.Enfants)
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
                    newLine.BudgetPrimitif.MontantTotal = newLine.BudgetPrimitif.MontantRecette;
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
                    .SumAsync(b => (int?)b.MontantPrevu) ?? 0;

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


        public decimal TotalRecetteFonctionnement(List<BudgetLine> lines)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == NatureType.Recette &&
                    bl.Nommenclature.Section == SectionType.Fonctionnement &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(bl => bl.MontantPrevu);
        }

        public decimal TotalDepenseFonctionnement(List<BudgetLine> lines)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == NatureType.Depense &&
                    bl.Nommenclature.Section == SectionType.Fonctionnement &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(bl => bl.MontantPrevu);
        }

        public decimal TotalDepenseReelFonctionnement(List<BudgetLine> lines)
        {
            var prelevement = TotalRecetteFonctionnement(lines) * 0.6m;
            return TotalDepenseFonctionnement(lines) - prelevement;
        }

        public decimal TotalRecetteInvestissement(List<BudgetLine> lines)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == NatureType.Recette &&
                    bl.Nommenclature.Section == SectionType.Investissement &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(bl => bl.MontantPrevu);
        }

        public decimal TotalRecetteReelInvestissement(List<BudgetLine> lines)
        {
            var prelevement = TotalRecetteFonctionnement(lines) * 0.6m;
            return TotalRecetteInvestissement(lines) - prelevement;
        }

        public decimal TotalGeneralRecetteReel(List<BudgetLine> lines)
        {
            return TotalRecetteReelInvestissement(lines) + TotalRecetteFonctionnement(lines);
        }

        public decimal TotalDepenseInvestissement(List<BudgetLine> lines)
        {
            return lines
                .Where(bl =>
                    bl.Nommenclature.Nature == NatureType.Depense &&
                    bl.Nommenclature.Section == SectionType.Investissement &&
                    bl.Nommenclature.ParentId == null
                )
                .Sum(bl => bl.MontantPrevu);
        }

        public decimal TotalGeneralDepenseReel(List<BudgetLine> lines)
        {
            return TotalDepenseReelFonctionnement(lines)
                 - TotalDepenseInvestissement(lines);
        }


    }


}