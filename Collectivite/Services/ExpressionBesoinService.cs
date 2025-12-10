using Collectivite.Models;
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

        public ExpressionBesoinService()
        {
            _context = new AppDbContext();
        }

        // Récupérer toutes les expressions de besoin
        public async Task<List<ExpressionBesoin>> GetAllExpressionBesoinsAsync()
        {
            return await _context.ExpressionBesoins
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

        //récupérer les nomenclatures de type dépense (feuilles uniquement)
        public async Task<List<Nommenclature>> GetNommenclaturesAsync()
        {
            return await _context.Nommenclatures
                .Where(n => n.Nature == NatureType.Depense && (n.Enfants == null || !n.Enfants.Any()))
                .ToListAsync();
        }

        // Créer une nouvelle expression de besoin
        public async Task<(bool success, string message, ExpressionBesoin? expressionBesoin)> CreateExpressionBesoinAsync(
            ExpressionBesoin expressionBesoin,
            List<DetailExpressionBesoin> details)
        {
            try
            {
                // Vérifier que le numéro n'existe pas déjà
                var exists = await _context.ExpressionBesoins
                    .AnyAsync(e => e.Numero == expressionBesoin.Numero);

                if (exists)
                {
                    return (false, "Ce numéro d'expression de besoin existe déjà.", null);
                }

                // Vérifier qu'il y a au moins un détail
                if (details == null || details.Count == 0)
                {
                    return (false, "Veuillez ajouter au moins un détail.", null);
                }

                // Valider les détails
                foreach (var detail in details)
                {
                    if (string.IsNullOrWhiteSpace(detail.Designation))
                    {
                        return (false, "Toutes les lignes doivent avoir une désignation.", null);
                    }

                    if (detail.Quantite <= 0)
                    {
                        return (false, "La quantité doit être supérieure à 0.", null);
                    }

                    if (detail.NommenclatureId <= 0)
                    {
                        return (false, "Veuillez sélectionner une nomenclature pour chaque ligne.", null);
                    }
                }

                // Créer l'expression de besoin
                _context.ExpressionBesoins.Add(expressionBesoin);
                await _context.SaveChangesAsync();

                // Ajouter les détails
                foreach (var detail in details)
                {
                    detail.ExpressionBesoinId = expressionBesoin.Id;
                    _context.DetailExpressionBesoins.Add(detail);
                }

                await _context.SaveChangesAsync();

                return (true, "Expression de besoin créée avec succès.", expressionBesoin);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création : {ex.Message}", null);
            }
        }

        // Mettre à jour une expression de besoin
        public async Task<(bool success, string message)> UpdateExpressionBesoinAsync(
            ExpressionBesoin expressionBesoin,
            List<DetailExpressionBesoin> details)
        {
            try
            {
                var existing = await _context.ExpressionBesoins
                    .Include(e => e.Details)
                    .FirstOrDefaultAsync(e => e.Id == expressionBesoin.Id);

                if (existing == null)
                {
                    return (false, "Expression de besoin introuvable.");
                }

                // Vérifier que le numéro n'est pas utilisé par une autre expression
                var duplicateNumero = await _context.ExpressionBesoins
                    .AnyAsync(e => e.Numero == expressionBesoin.Numero && e.Id != expressionBesoin.Id);

                if (duplicateNumero)
                {
                    return (false, "Ce numéro est déjà utilisé par une autre expression de besoin.");
                }

                // Vérifier qu'il y a au moins un détail
                if (details == null || details.Count == 0)
                {
                    return (false, "Veuillez ajouter au moins un détail.");
                }

                // Valider les détails
                foreach (var detail in details)
                {
                    if (string.IsNullOrWhiteSpace(detail.Designation))
                    {
                        return (false, "Toutes les lignes doivent avoir une désignation.");
                    }

                    if (detail.Quantite <= 0)
                    {
                        return (false, "La quantité doit être supérieure à 0.");
                    }

                    if (detail.NommenclatureId <= 0)
                    {
                        return (false, "Veuillez sélectionner une nomenclature pour chaque ligne.");
                    }
                }

                // Mettre à jour l'expression de besoin
                existing.Numero = expressionBesoin.Numero;
                existing.DateCreation = expressionBesoin.DateCreation;
                existing.ExerciceId = expressionBesoin.ExerciceId;

                // Supprimer les anciens détails
                _context.DetailExpressionBesoins.RemoveRange(existing.Details);

                // Ajouter les nouveaux détails
                foreach (var detail in details)
                {
                    detail.ExpressionBesoinId = existing.Id;
                    _context.DetailExpressionBesoins.Add(detail);
                }

                await _context.SaveChangesAsync();

                return (true, "Expression de besoin modifiée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}");
            }
        }

        // Supprimer une expression de besoin
        public async Task<(bool success, string message)> DeleteExpressionBesoinAsync(int id)
        {
            try
            {
                var expressionBesoin = await _context.ExpressionBesoins
                    .Include(e => e.Details)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expressionBesoin == null)
                {
                    return (false, "Expression de besoin introuvable.");
                }

                // Supprimer les détails
                _context.DetailExpressionBesoins.RemoveRange(expressionBesoin.Details);

                // Supprimer l'expression de besoin
                _context.ExpressionBesoins.Remove(expressionBesoin);

                await _context.SaveChangesAsync();

                return (true, "Expression de besoin supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }
    }
}