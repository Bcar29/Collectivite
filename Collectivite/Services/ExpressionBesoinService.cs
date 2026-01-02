using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class ExpressionBesoinService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public ExpressionBesoinService()
        {
            _context = new AppDbContext();
            _auditService = new AuditService();
        }

        // Récupérer toutes les expressions de besoin
        public async Task<List<ExpressionBesoin>> GetAllExpressionBesoinsAsync()
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
                return new List<ExpressionBesoin>();

            return await _context.ExpressionBesoins
                .Where(e => e.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(e => e.Exercice)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Nommenclature)
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync();
        }

        // Récupérer une expression de besoin par ID
        public async Task<ExpressionBesoin?> GetExpressionBesoinByIdAsync(int id)
        {
            return await _context.ExpressionBesoins
                .Include(e => e.Exercice)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Nommenclature)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // Récupérer les nomenclatures de type dépense (feuilles uniquement)
        public async Task<List<Nommenclature>> GetNommenclaturesAsync()
        {
            return await _context.Nommenclatures
                .Where(n => n.Nature == NatureType.Depense &&
                            (n.Enfants == null || !n.Enfants.Any()))
                .ToListAsync();
        }

        public async Task<string> GenerateNextNumeroAsync()
        {
            var exerciceService = ExerciceService.Instance;
            var year = exerciceService.CurrentExercice?.GetAnnee() ?? DateTime.Now.Year;
            var prefix = $"EB-{year}-";

            var expressions = await _context.ExpressionBesoins
                .Where(e => e.Numero.StartsWith(prefix))
                .OrderByDescending(e => e.Numero)
                .ToListAsync();

            if (!expressions.Any())
                return $"{prefix}0001";

            var lastNumero = expressions.First().Numero;
            var lastSequence = lastNumero.Split('-').Last();

            return int.TryParse(lastSequence, out int seq)
                ? $"{prefix}{(seq + 1):D4}"
                : $"{prefix}0001";
        }

        // ═════════════════════════════════════════════
        // CREATE
        // ═════════════════════════════════════════════
        public async Task<(bool success, string message, ExpressionBesoin? expressionBesoin)>
            CreateExpressionBesoinAsync(ExpressionBesoin expressionBesoin, List<DetailExpressionBesoin> details)
        {
            try
            {
                if (await _context.ExpressionBesoins.AnyAsync(e => e.Numero == expressionBesoin.Numero))
                    return (false, "Ce numéro existe déjà.", null);

                if (details == null || details.Count == 0)
                    return (false, "Veuillez ajouter au moins un détail.", null);

                _context.ExpressionBesoins.Add(expressionBesoin);
                await _context.SaveChangesAsync();

                foreach (var detail in details)
                {
                    detail.ExpressionBesoinId = expressionBesoin.Id;
                    _context.DetailExpressionBesoins.Add(detail);
                }

                await _context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Création Expression de Besoin",
                    $"Création EB N° {expressionBesoin.Numero}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "Expression de besoin créée avec succès.", expressionBesoin);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création : {ex.Message}", null);
            }
        }

        // ═════════════════════════════════════════════
        // UPDATE
        // ═════════════════════════════════════════════
        public async Task<(bool success, string message, ExpressionBesoin? expression)>
            UpdateExpressionBesoinAsync(ExpressionBesoin expressionBesoin, List<DetailExpressionBesoin> details)
        {
            try
            {
                var existing = await _context.ExpressionBesoins
                    .Include(e => e.Details)
                    .FirstOrDefaultAsync(e => e.Id == expressionBesoin.Id);

                if (existing == null)
                    return (false, "Expression de besoin introuvable.", null);

                existing.Numero = expressionBesoin.Numero;
                existing.DateCreation = expressionBesoin.DateCreation;
                existing.ExerciceId = expressionBesoin.ExerciceId;

                _context.DetailExpressionBesoins.RemoveRange(existing.Details);

                foreach (var detail in details)
                {
                    detail.ExpressionBesoinId = existing.Id;
                    _context.DetailExpressionBesoins.Add(detail);
                }

                await _context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Modification Expression de Besoin",
                    $"Modification EB N° {existing.Numero}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "Expression de besoin modifiée avec succès.", existing);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}", null);
            }
        }

        // ═════════════════════════════════════════════
        // DELETE
        // ═════════════════════════════════════════════
        public async Task<(bool success, string message)> DeleteExpressionBesoinAsync(int id)
        {
            try
            {
                var expression = await _context.ExpressionBesoins
                    .Include(e => e.Details)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expression == null)
                    return (false, "Expression de besoin introuvable.");

                _context.DetailExpressionBesoins.RemoveRange(expression.Details);
                _context.ExpressionBesoins.Remove(expression);

                await _context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Suppression Expression de Besoin",
                    $"Suppression EB N° {expression.Numero}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "Expression de besoin supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }
    }
}
