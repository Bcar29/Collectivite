using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des tiers (fournisseurs, entreprises, etc.)
    /// </summary>
    public class TiersService
    {
        // ✅ CORRECTION : NE PAS stocker le DbContext
        // private readonly AppDbContext _context; ❌ SUPPRIMÉ

        // ✅ Créer un nouveau DbContext pour chaque opération
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les tiers avec leurs comptes bancaires
        /// </summary>
        public async Task<List<Tiers>> GetAllTiersAsync()
        {
            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.CompteBancaires)
                .AsNoTracking()
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un tiers par son ID
        /// </summary>
        public async Task<Tiers?> GetTiersByIdAsync(int id)
        {
            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.CompteBancaires)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Récupère les tiers actifs uniquement
        /// </summary>
        public async Task<List<Tiers>> GetTiersActifsAsync()
        {
            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.CompteBancaires)
                .Where(t => t.IsActif)
                .AsNoTracking()
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les tiers par type
        /// </summary>
        public async Task<List<Tiers>> GetTiersByTypeAsync(TiersType type)
        {
            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.CompteBancaires)
                .Where(t => t.Type == type)
                .AsNoTracking()
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche des tiers par nom
        /// </summary>
        public async Task<List<Tiers>> SearchTiersAsync(string searchTerm)
        {
            using var context = CreateContext();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllTiersAsync();

            return await context.Tiers
                .Include(t => t.CompteBancaires)
                .Where(t => t.Nom.Contains(searchTerm) ||
                           (t.Prenom != null && t.Prenom.Contains(searchTerm)) ||
                           (t.Email != null && t.Email.Contains(searchTerm)))
                .AsNoTracking()
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau tiers
        /// </summary>
        public async Task<(bool Success, string Message, Tiers? Tiers)> CreateTiersAsync(Tiers tiers)
        {
            using var context = CreateContext();

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(tiers.Nom))
                    return (false, "Le nom est obligatoire.", null);

                if (string.IsNullOrWhiteSpace(tiers.Email))
                    return (false, "L'email est obligatoire.", null);

                if (string.IsNullOrWhiteSpace(tiers.Adresse))
                    return (false, "L'adresse est obligatoire.", null);

                // Vérifier si l'email existe déjà
                var emailExists = await context.Tiers
                    .AnyAsync(t => t.Email.ToLower() == tiers.Email.ToLower());

                if (emailExists)
                    return (false, "Cet email est déjà utilisé par un autre tiers.", null);

                // Vérifier le NIF s'il est fourni
                if (!string.IsNullOrWhiteSpace(tiers.Nif))
                {
                    var nifExists = await context.Tiers
                        .AnyAsync(t => t.Nif == tiers.Nif);

                    if (nifExists)
                        return (false, "Ce NIF est déjà utilisé par un autre tiers.", null);
                }

                // ✅ CORRECTION : Créer un nouvel objet sans navigation
                var newTiers = new Tiers
                {
                    Type = tiers.Type,
                    Rccm = tiers.Rccm?.Trim(),
                    Nom = tiers.Nom.Trim(),
                    Prenom = tiers.Prenom?.Trim(),
                    Adresse = tiers.Adresse.Trim(),
                    Nif = tiers.Nif?.Trim(),
                    Email = tiers.Email.Trim().ToLower(),
                    IsActif = tiers.IsActif
                };

                context.Tiers.Add(newTiers);
                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedTiers = await GetTiersByIdAsync(newTiers.Id);

                return (true, $"Tiers '{newTiers.Nom}' créé avec succès.", savedTiers);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("UNIQUE") || innerMessage.Contains("duplicate"))
                {
                    return (false, "Ce tiers existe déjà (email, NIF ou contrainte unique).", null);
                }

                return (false, $"Erreur de base de données : {innerMessage}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création : {ex.Message}", null);
            }
        }

        #endregion

        #region Modification

        /// <summary>
        /// Met à jour un tiers existant
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateTiersAsync(Tiers tiers)
        {
            using var context = CreateContext();

            try
            {
                var existingTiers = await context.Tiers.FindAsync(tiers.Id);

                if (existingTiers == null)
                    return (false, "Tiers introuvable.");

                // Validation
                if (string.IsNullOrWhiteSpace(tiers.Nom))
                    return (false, "Le nom est obligatoire.");

                if (string.IsNullOrWhiteSpace(tiers.Email))
                    return (false, "L'email est obligatoire.");

                if (string.IsNullOrWhiteSpace(tiers.Adresse))
                    return (false, "L'adresse est obligatoire.");

                // Vérifier l'unicité de l'email
                var emailExists = await context.Tiers
                    .AnyAsync(t => t.Email.ToLower() == tiers.Email.ToLower() && t.Id != tiers.Id);

                if (emailExists)
                    return (false, "Cet email est déjà utilisé par un autre tiers.");

                // Vérifier l'unicité du NIF
                if (!string.IsNullOrWhiteSpace(tiers.Nif))
                {
                    var nifExists = await context.Tiers
                        .AnyAsync(t => t.Nif == tiers.Nif && t.Id != tiers.Id);

                    if (nifExists)
                        return (false, "Ce NIF est déjà utilisé par un autre tiers.");
                }

                // Mettre à jour
                existingTiers.Type = tiers.Type;
                existingTiers.Rccm = tiers.Rccm?.Trim();
                existingTiers.Nom = tiers.Nom.Trim();
                existingTiers.Prenom = tiers.Prenom?.Trim();
                existingTiers.Adresse = tiers.Adresse.Trim();
                existingTiers.Nif = tiers.Nif?.Trim();
                existingTiers.Email = tiers.Email.Trim().ToLower();
                existingTiers.IsActif = tiers.IsActif;

                await context.SaveChangesAsync();

                return (true, $"Tiers '{tiers.Nom}' modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}");
            }
        }

        /// <summary>
        /// Active ou désactive un tiers
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleActifAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var tiers = await context.Tiers.FindAsync(id);

                if (tiers == null)
                    return (false, "Tiers introuvable.");

                tiers.IsActif = !tiers.IsActif;
                await context.SaveChangesAsync();

                var status = tiers.IsActif ? "activé" : "désactivé";
                return (true, $"Tiers '{tiers.Nom}' {status} avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un tiers
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteTiersAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var tiers = await context.Tiers
                    .Include(t => t.CompteBancaires)
                    .Include(t => t.Contrats)
                    .Include(t => t.Engagements)
                    .Include(t => t.Factures)
                    .Include(t => t.Recensements)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tiers == null)
                    return (false, "Tiers introuvable.");

                // Vérifier s'il y a des dépendances
                var hasDependencies = (tiers.Contrats?.Any() ?? false) ||
                                     (tiers.Engagements?.Any() ?? false) ||
                                     (tiers.Factures?.Any() ?? false) ||
                                     (tiers.Recensements?.Any() ?? false);

                if (hasDependencies)
                {
                    return (false,
                        "Impossible de supprimer ce tiers car il est lié à des contrats, engagements, factures ou recensements.\n" +
                        "Vous pouvez le désactiver à la place.");
                }

                // Supprimer les comptes bancaires associés
                if (tiers.CompteBancaires?.Any() ?? false)
                {
                    context.CompteBancaires.RemoveRange(tiers.CompteBancaires);
                }

                context.Tiers.Remove(tiers);
                await context.SaveChangesAsync();

                return (true, $"Tiers '{tiers.Nom}' supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient le nombre de tiers par type
        /// </summary>
        public async Task<Dictionary<TiersType, int>> GetTiersCountByTypeAsync()
        {
            using var context = CreateContext();

            var counts = await context.Tiers
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.Type, x => x.Count);
        }

        /// <summary>
        /// Obtient le nombre total de tiers actifs
        /// </summary>
        public async Task<int> GetTiersActifsCountAsync()
        {
            using var context = CreateContext();

            return await context.Tiers.CountAsync(t => t.IsActif);
        }

        #endregion
    }
}