using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des tiers (Contribuables, Fournisseurs, Salariés)
    /// </summary>
    public class TiersService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les tiers avec leurs documents
        /// </summary>
        public async Task<List<Tiers>> GetAllTiersAsync()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise pour consulter les tiers.");

            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.Documents)
                .Include(t => t.CompteBancaires)
                .AsNoTracking()
                .OrderBy(t => t.Nom ?? t.RaisonSociale)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un tiers par son ID
        /// </summary>
        public async Task<Tiers?> GetTiersByIdAsync(int id)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.Documents)
                .Include(t => t.CompteBancaires)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Récupère les tiers actifs uniquement
        /// </summary>
        public async Task<List<Tiers>> GetTiersActifsAsync()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.Documents)
                .Include(t => t.CompteBancaires)
                .Where(t => t.IsActif)
                .AsNoTracking()
                .OrderBy(t => t.Nom ?? t.RaisonSociale)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les tiers par type
        /// </summary>
        public async Task<List<Tiers>> GetTiersByTypeAsync(TiersType type)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.Documents)
                .Include(t => t.CompteBancaires)
                .Where(t => t.Type == type)
                .AsNoTracking()
                .OrderBy(t => t.Nom ?? t.RaisonSociale)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les tiers par catégorie juridique
        /// </summary>
        public async Task<List<Tiers>> GetTiersByCategorieAsync(CategorieJuridique categorie)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            return await context.Tiers
                .Include(t => t.Documents)
                .Where(t => t.Categorie == categorie)
                .AsNoTracking()
                .OrderBy(t => t.Nom ?? t.RaisonSociale)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche des tiers par nom, raison sociale ou email
        /// </summary>
        public async Task<List<Tiers>> SearchTiersAsync(string searchTerm)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllTiersAsync();

            searchTerm = searchTerm.ToLower();

            return await context.Tiers
                .Include(t => t.Documents)
                .Include(t => t.CompteBancaires)
                .Where(t =>
                    (t.Nom != null && t.Nom.ToLower().Contains(searchTerm)) ||
                    (t.Prenom != null && t.Prenom.ToLower().Contains(searchTerm)) ||
                    (t.RaisonSociale != null && t.RaisonSociale.ToLower().Contains(searchTerm)) ||
                    (t.Email != null && t.Email.ToLower().Contains(searchTerm)) ||
                    (t.Nif != null && t.Nif.Contains(searchTerm)) ||
                    (t.Rccm != null && t.Rccm.Contains(searchTerm)))
                .AsNoTracking()
                .OrderBy(t => t.Nom ?? t.RaisonSociale)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau tiers avec validation conditionnelle
        /// </summary>
        public async Task<(bool Success, string Message, Tiers? Tiers)> CreateTiersAsync(Tiers tiers)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.Create"))
                return (false, "Permission Tiers.Create requise pour créer un tiers.", null);

            using var context = CreateContext();

            try
            {
                // ═══════════════════════════════════════════════════════════
                // VALIDATIONS COMMUNES
                // ═══════════════════════════════════════════════════════════

                if (string.IsNullOrWhiteSpace(tiers.Email))
                    return (false, "L'email est obligatoire.", null);

                // Vérifier si l'email existe déjà
                var emailExists = await context.Tiers
                    .AnyAsync(t => t.Email.ToLower() == tiers.Email.ToLower());

                if (emailExists)
                    return (false, "Cet email est déjà utilisé par un autre tiers.", null);

                // ═══════════════════════════════════════════════════════════
                // VALIDATIONS CONDITIONNELLES - PERSONNE PHYSIQUE
                // ═══════════════════════════════════════════════════════════

                if (tiers.Categorie == CategorieJuridique.PersonnePhysique)
                {
                    if (string.IsNullOrWhiteSpace(tiers.Nom))
                        return (false, "Le nom est obligatoire pour une personne physique.", null);

                    if (string.IsNullOrWhiteSpace(tiers.Prenom))
                        return (false, "Le prénom est obligatoire pour une personne physique.", null);

                    // Nettoyer les champs de personne morale
                    tiers.RaisonSociale = null;
                    tiers.Rccm = null;
                    tiers.Nif = null;
                    tiers.NumeroTva = null;
                }

                // ═══════════════════════════════════════════════════════════
                // VALIDATIONS CONDITIONNELLES - PERSONNE MORALE
                // ═══════════════════════════════════════════════════════════

                if (tiers.Categorie == CategorieJuridique.PersonneMorale)
                {
                    if (string.IsNullOrWhiteSpace(tiers.RaisonSociale))
                        return (false, "La raison sociale est obligatoire pour une personne morale.", null);

                    if (string.IsNullOrWhiteSpace(tiers.Rccm))
                        return (false, "Le RCCM est obligatoire pour une personne morale.", null);

                    if (string.IsNullOrWhiteSpace(tiers.Nif))
                        return (false, "Le NIF est obligatoire pour une personne morale.", null);

                    // Vérifier l'unicité du RCCM
                    var rccmExists = await context.Tiers
                        .AnyAsync(t => t.Rccm == tiers.Rccm);

                    if (rccmExists)
                        return (false, "Ce RCCM est déjà utilisé par un autre tiers.", null);

                    // Vérifier l'unicité du NIF
                    var nifExists = await context.Tiers
                        .AnyAsync(t => t.Nif == tiers.Nif);

                    if (nifExists)
                        return (false, "Ce NIF est déjà utilisé par un autre tiers.", null);

                    // Nettoyer les champs de personne physique
                    tiers.Nom = null;
                    tiers.Prenom = null;
                    tiers.NumeroPieceIdentite = null;
                    tiers.TypePieceIdentite = null;
                }

                // ═══════════════════════════════════════════════════════════
                // CRÉATION DU TIERS
                // ═══════════════════════════════════════════════════════════

                var newTiers = new Tiers
                {
                    Type = tiers.Type,
                    Categorie = tiers.Categorie,
                    Email = tiers.Email.Trim().ToLower(),
                    Telephone = tiers.Telephone?.Trim(),
                    Adresse = tiers.Adresse?.Trim(),
                    IsActif = tiers.IsActif,
                    DateCreation = DateTime.Now,

                    // Personne Physique
                    Nom = tiers.Nom?.Trim(),
                    Prenom = tiers.Prenom?.Trim(),
                    NumeroPieceIdentite = tiers.NumeroPieceIdentite?.Trim(),
                    TypePieceIdentite = tiers.TypePieceIdentite?.Trim(),

                    // Personne Morale
                    RaisonSociale = tiers.RaisonSociale?.Trim(),
                    Rccm = tiers.Rccm?.Trim(),
                    Nif = tiers.Nif?.Trim(),
                    NumeroTva = tiers.NumeroTva?.Trim(),
                    SecteurActivite = tiers.SecteurActivite?.Trim()
                };

                context.Tiers.Add(newTiers);
                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedTiers = await GetTiersByIdAsync(newTiers.Id);

                var nomComplet = savedTiers?.NomComplet ?? "Tiers";
                return (true, $"Tiers '{nomComplet}' créé avec succès.", savedTiers);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("UNIQUE") || innerMessage.Contains("duplicate"))
                {
                    return (false, "Ce tiers existe déjà (contrainte d'unicité violée).", null);
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
        /// Met à jour un tiers existant avec validation conditionnelle
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateTiersAsync(Tiers tiers)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.Edit"))
                return (false, "Permission Tiers.Edit requise pour modifier un tiers.");

            using var context = CreateContext();

            try
            {
                var existingTiers = await context.Tiers.FindAsync(tiers.Id);

                if (existingTiers == null)
                    return (false, "Tiers introuvable.");

                // ═══════════════════════════════════════════════════════════
                // VALIDATIONS COMMUNES
                // ═══════════════════════════════════════════════════════════

                if (string.IsNullOrWhiteSpace(tiers.Email))
                    return (false, "L'email est obligatoire.");

                // Vérifier l'unicité de l'email
                var emailExists = await context.Tiers
                    .AnyAsync(t => t.Email.ToLower() == tiers.Email.ToLower() && t.Id != tiers.Id);

                if (emailExists)
                    return (false, "Cet email est déjà utilisé par un autre tiers.");

                // ═══════════════════════════════════════════════════════════
                // VALIDATIONS CONDITIONNELLES
                // ═══════════════════════════════════════════════════════════

                if (tiers.Categorie == CategorieJuridique.PersonnePhysique)
                {
                    if (string.IsNullOrWhiteSpace(tiers.Nom))
                        return (false, "Le nom est obligatoire pour une personne physique.");

                    if (string.IsNullOrWhiteSpace(tiers.Prenom))
                        return (false, "Le prénom est obligatoire pour une personne physique.");
                }

                if (tiers.Categorie == CategorieJuridique.PersonneMorale)
                {
                    if (string.IsNullOrWhiteSpace(tiers.RaisonSociale))
                        return (false, "La raison sociale est obligatoire pour une personne morale.");

                    if (string.IsNullOrWhiteSpace(tiers.Rccm))
                        return (false, "Le RCCM est obligatoire pour une personne morale.");

                    if (string.IsNullOrWhiteSpace(tiers.Nif))
                        return (false, "Le NIF est obligatoire pour une personne morale.");

                    // Vérifier l'unicité du RCCM
                    var rccmExists = await context.Tiers
                        .AnyAsync(t => t.Rccm == tiers.Rccm && t.Id != tiers.Id);

                    if (rccmExists)
                        return (false, "Ce RCCM est déjà utilisé par un autre tiers.");

                    // Vérifier l'unicité du NIF
                    var nifExists = await context.Tiers
                        .AnyAsync(t => t.Nif == tiers.Nif && t.Id != tiers.Id);

                    if (nifExists)
                        return (false, "Ce NIF est déjà utilisé par un autre tiers.");
                }

                // ═══════════════════════════════════════════════════════════
                // MISE À JOUR
                // ═══════════════════════════════════════════════════════════

                existingTiers.Type = tiers.Type;
                existingTiers.Categorie = tiers.Categorie;
                existingTiers.Email = tiers.Email.Trim().ToLower();
                existingTiers.Telephone = tiers.Telephone?.Trim();
                existingTiers.Adresse = tiers.Adresse?.Trim();
                existingTiers.IsActif = tiers.IsActif;

                // Personne Physique
                existingTiers.Nom = tiers.Nom?.Trim();
                existingTiers.Prenom = tiers.Prenom?.Trim();
                existingTiers.NumeroPieceIdentite = tiers.NumeroPieceIdentite?.Trim();
                existingTiers.TypePieceIdentite = tiers.TypePieceIdentite?.Trim();

                // Personne Morale
                existingTiers.RaisonSociale = tiers.RaisonSociale?.Trim();
                existingTiers.Rccm = tiers.Rccm?.Trim();
                existingTiers.Nif = tiers.Nif?.Trim();
                existingTiers.NumeroTva = tiers.NumeroTva?.Trim();
                existingTiers.SecteurActivite = tiers.SecteurActivite?.Trim();

                await context.SaveChangesAsync();

                return (true, $"Tiers '{tiers.NomComplet}' modifié avec succès.");
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
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.Edit"))
                return (false, "Permission Tiers.Edit requise pour activer/désactiver un tiers.");

            using var context = CreateContext();

            try
            {
                var tiers = await context.Tiers.FindAsync(id);

                if (tiers == null)
                    return (false, "Tiers introuvable.");

                tiers.IsActif = !tiers.IsActif;
                await context.SaveChangesAsync();

                var status = tiers.IsActif ? "activé" : "désactivé";
                return (true, $"Tiers '{tiers.NomComplet}' {status} avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un tiers et ses documents associés
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteTiersAsync(int id)
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.Delete"))
                return (false, "Permission Tiers.Delete requise pour supprimer un tiers.");

            using var context = CreateContext();

            try
            {
                var tiers = await context.Tiers
                    .Include(t => t.Documents)
                    .Include(t => t.CompteBancaires)
                    .Include(t => t.Engagements)
                    .Include(t => t.Factures)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tiers == null)
                    return (false, "Tiers introuvable.");

                // Vérifier s'il y a des dépendances
                var hasDependencies = (tiers.Engagements?.Any() ?? false) ||
                                     (tiers.Factures?.Any() ?? false);

                if (hasDependencies)
                {
                    return (false,
                        "Impossible de supprimer ce tiers car il est lié à des engagements ou factures.\n" +
                        "Vous pouvez le désactiver à la place.");
                }

                // Supprimer les documents associés (fichiers physiques)
                if (tiers.Documents?.Any() ?? false)
                {
                    foreach (var doc in tiers.Documents)
                    {
                        try
                        {
                            if (System.IO.File.Exists(doc.CheminFichier))
                            {
                                System.IO.File.Delete(doc.CheminFichier);
                            }
                        }
                        catch
                        {
                            // Ignorer les erreurs de suppression de fichiers
                        }
                    }

                    context.DocumentTiers.RemoveRange(tiers.Documents);
                }

                // Supprimer les comptes bancaires associés
                if (tiers.CompteBancaires?.Any() ?? false)
                {
                    context.CompteBancaires.RemoveRange(tiers.CompteBancaires);
                }

                context.Tiers.Remove(tiers);
                await context.SaveChangesAsync();

                return (true, $"Tiers '{tiers.NomComplet}' supprimé avec succès.");
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
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            var counts = await context.Tiers
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.Type, x => x.Count);
        }

        /// <summary>
        /// Obtient le nombre de tiers par catégorie juridique
        /// </summary>
        public async Task<Dictionary<CategorieJuridique, int>> GetTiersCountByCategorieAsync()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            var counts = await context.Tiers
                .GroupBy(t => t.Categorie)
                .Select(g => new { Categorie = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.Categorie, x => x.Count);
        }

        /// <summary>
        /// Obtient le nombre total de tiers actifs
        /// </summary>
        public async Task<int> GetTiersActifsCountAsync()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!SessionManager.HasPermission("Tiers.View"))
                throw new UnauthorizedAccessException("Permission Tiers.View requise.");

            using var context = CreateContext();

            return await context.Tiers.CountAsync(t => t.IsActif);
        }

        #endregion
    }
}