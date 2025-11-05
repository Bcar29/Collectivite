using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BudgetLineService 
    {
        private readonly AppDbContext _appDbContext;
        public BudgetLineService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<BudgetLine>> GetBudgetLinesForBudgetPrimitifAsync(int budgetPrimitifId)
        {
            return await _appDbContext.BudgetLines
                .Include(b => b.Nommenclature)
                .Where(b => b.BudgetPrimitifId == budgetPrimitifId)
                .ToListAsync();
        }

        public async Task<bool> HasChildrenAsync(int nomenclatureId)
        {
            return await _appDbContext.Nommenclatures.AnyAsync(n => n.ParentId == nomenclatureId);
        }

        // récupère les nomenclatures feuilles (sans enfants) non liées à un BudgetLine pour ce budget et filtrées par nature/section
        public async Task<List<Nommenclature>> GetLeafNomenclaturesNotLinkedAsync(int budgetPrimitifId, NatureType nature, SectionType section)
        {
            var leafs = await _appDbContext.Nommenclatures
                .Where(n => n.Nature == nature && n.Section == section)
                .Where(n => !_appDbContext.Nommenclatures.Any(c => c.ParentId == n.Id)) // pas d'enfants = feuille
                .ToListAsync();

            // Exclure celles déjà liées dans ce budget primitif
            var linkedIds = await _appDbContext.BudgetLines
                .Where(b => b.BudgetPrimitifId == budgetPrimitifId)
                .Select(b => b.NommenclatureId)
                .ToListAsync();

            return leafs.Where(n => !linkedIds.Contains(n.Id)).ToList();
        }

        // Crée la ligne pour la nomenclature demandée (doit être feuille), et assure qu'il existe des BudgetLine pour tous les ancêtres.
        // Puis recalcule les montants des ancêtres.
        public async Task<BudgetLine> CreateBudgetLineAsync(int budgetPrimitifId, int nomenclatureId, int montantPrevu)
        {
            // Bloquer si la nomenclature a des enfants
            if (await HasChildrenAsync(nomenclatureId))
                throw new InvalidOperationException("Impossible de créer une ligne pour une nomenclature ayant des enfants.");

            // Si une ligne existe déjà pour cette nomenclature et ce budget -> empêcher ou mettre à jour (ici on jette)
            var exist = await _appDbContext.BudgetLines.FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId && b.NommenclatureId == nomenclatureId);
            if (exist != null)
                throw new InvalidOperationException("Une ligne budgétaire existe déjà pour cette nomenclature dans le budget.");

            // Créer la ligne feuille
            var newLine = new BudgetLine
            {
                BudgetPrimitifId = budgetPrimitifId,
                NommenclatureId = nomenclatureId,
                MontantPrevu = montantPrevu,
                MontantActu = montantPrevu
            };

            _appDbContext.BudgetLines.Add(newLine);
            await _appDbContext.SaveChangesAsync();

            // Recalculer tous les ancêtres jusqu'à la racine
            await RecalculateAncestorsAsync(nomenclatureId, budgetPrimitifId);

            // recharger avec navigation
            await _appDbContext.Entry(newLine).Reference(b => b.Nommenclature).LoadAsync();
            return newLine;
        }

        // Met à jour la ligne, puis recalcule les montants des parents
        public async Task<BudgetLine> UpdateBudgetLineAsync(int budgetLineId, int newMontantPrevu)
        {
            var line = await _appDbContext.BudgetLines.Include(b => b.Nommenclature).FirstOrDefaultAsync(b => b.Id == budgetLineId);
            if (line == null) throw new KeyNotFoundException("Ligne budgétaire introuvable.");

            line.MontantPrevu = newMontantPrevu;
            // Optionnel: mettre à jour MontantActu aussi (ici on fait égalité)
            line.MontantActu = newMontantPrevu;

            await _appDbContext.SaveChangesAsync();

            // Recalculate ancestors
            await RecalculateAncestorsAsync(line.NommenclatureId, line.BudgetPrimitifId);

            return line;
        }

        public async Task DeleteBudgetLineAsync(int budgetLineId)
        {
            var line = await _appDbContext.BudgetLines.Include(b => b.Nommenclature).FirstOrDefaultAsync(b => b.Id == budgetLineId);
            if (line == null) return;

            // Avant suppression, on supprime et recalcule ancêtres
            var nomenclatureId = line.NommenclatureId;
            var budgetPrimitifId = line.BudgetPrimitifId;

            _appDbContext.BudgetLines.Remove(line);
            await _appDbContext.SaveChangesAsync();

            await RecalculateAncestorsAsync(nomenclatureId, budgetPrimitifId);
        }

        // --- Helpers ---
        // Recalcule pour chaque parent la somme des MontantPrevu de ses enfants (pour ce même BudgetPrimitif).
        private async Task RecalculateAncestorsAsync(int childNomenclatureId, int budgetPrimitifId)
        {
            // Récupérer l'ancêtre immédiat
            var child = await _appDbContext.Nommenclatures.FirstOrDefaultAsync(n => n.Id == childNomenclatureId);
            if (child == null) return;

            var parentId = child.ParentId;
            while (parentId.HasValue)
            {
                // Pour ce parent, calculer la somme des MontantPrevu des enfants (pour ce budget primitif)
                var childrenIds = await _appDbContext.Nommenclatures
                    .Where(n => n.ParentId == parentId.Value)
                    .Select(n => n.Id)
                    .ToListAsync();

                // Somme des montants des enfants (seulement ceux ayant une BudgetLine) pour ce budget primitif
                var sommeEnfants = await _appDbContext.BudgetLines
                    .Where(b => b.BudgetPrimitifId == budgetPrimitifId && childrenIds.Contains(b.NommenclatureId))
                    .SumAsync(b => (int?)b.MontantPrevu) ?? 0;

                // Trouver la BudgetLine du parent pour ce budget primitif, sinon la créer (avec le montant = sommeEnfants)
                var parentLine = await _appDbContext.BudgetLines.FirstOrDefaultAsync(b => b.BudgetPrimitifId == budgetPrimitifId && b.NommenclatureId == parentId.Value);
                if (parentLine == null)
                {
                    parentLine = new BudgetLine
                    {
                        BudgetPrimitifId = budgetPrimitifId,
                        NommenclatureId = parentId.Value,
                        MontantPrevu = sommeEnfants,
                        MontantActu = sommeEnfants
                    };
                    _appDbContext.BudgetLines.Add(parentLine);
                }
                else
                {
                    parentLine.MontantPrevu = sommeEnfants;
                    parentLine.MontantActu = sommeEnfants;
                }

                await _appDbContext.SaveChangesAsync();

                // Monter d'un niveau
                var parent = await _appDbContext.Nommenclatures.FirstOrDefaultAsync(n => n.Id == parentId.Value);
                if (parent == null) break;
                parentId = parent.ParentId;
            }
        }
    }
}
