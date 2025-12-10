using Collectivite.Models;
using Collectivite.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des comptes bancaires
    /// </summary>
    public class CompteBancaireService
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
        /// Récupère tous les comptes bancaires
        /// </summary>
        public async Task<List<CompteBancaire>> GetAllComptesAsync()
        {
            if (!SessionManager.HasPermission("CompteBancaire.View"))
                throw new UnauthorizedAccessException("Permission CompteBancaire.View requise pour consulter les comptes bancaires.");

            using var context = CreateContext();

            return await context.CompteBancaires
                .Include(c => c.Tiers)
                .AsNoTracking()
                .OrderBy(c => c.Tiers.Nom)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un compte bancaire par son ID
        /// </summary>
        public async Task<CompteBancaire?> GetCompteByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("CompteBancaire.View"))
                throw new UnauthorizedAccessException("Permission CompteBancaire.View requise pour consulter ce compte bancaire.");

            using var context = CreateContext();

            return await context.CompteBancaires
                .Include(c => c.Tiers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Récupère les comptes bancaires d'un tiers spécifique
        /// </summary>
        public async Task<List<CompteBancaire>> GetComptesByTiersAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("CompteBancaire.View"))
                throw new UnauthorizedAccessException("Permission CompteBancaire.View requise pour consulter les comptes bancaires.");

            using var context = CreateContext();

            return await context.CompteBancaires
                .Include(c => c.Tiers)
                .Where(c => c.TiersId == tiersId)
                .AsNoTracking()
                .OrderBy(c => c.Banque)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche un compte par IBAN
        /// </summary>
        public async Task<CompteBancaire?> GetCompteByIBANAsync(string iban)
        {
            if (!SessionManager.HasPermission("CompteBancaire.View"))
                throw new UnauthorizedAccessException("Permission CompteBancaire.View requise pour consulter les comptes bancaires.");

            using var context = CreateContext();

            return await context.CompteBancaires
                .Include(c => c.Tiers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IBAN == iban);
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau compte bancaire
        /// </summary>
        public async Task<(bool Success, string Message, CompteBancaire? Compte)> CreateCompteAsync(CompteBancaire compte)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("CompteBancaire.Create"))
                return (false, "Permission CompteBancaire.Create requise pour créer un compte bancaire.", null);

            try
            {
                // Validation
                if (compte.TiersId <= 0)
                {
                    return (false, "Le tiers est obligatoire.", null);
                }

                if (string.IsNullOrWhiteSpace(compte.IBAN))
                {
                    return (false, "Le numéro IBAN est obligatoire.", null);
                }

                if (string.IsNullOrWhiteSpace(compte.Banque))
                {
                    return (false, "Le nom de la banque est obligatoire.", null);
                }

                if (string.IsNullOrWhiteSpace(compte.Pays))
                {
                    return (false, "Le pays est obligatoire.", null);
                }

                // Vérifier si le tiers existe
                var tiersExists = await context.Tiers.AnyAsync(t => t.Id == compte.TiersId);
                if (!tiersExists)
                {
                    return (false, "Le tiers spécifié n'existe pas.", null);
                }

                // Vérifier si l'IBAN existe déjà
                var ibanExists = await context.CompteBancaires
                    .AnyAsync(c => c.IBAN == compte.IBAN);

                if (ibanExists)
                {
                    return (false, "Ce numéro IBAN existe déjà dans la base de données.", null);
                }

                // ✅ CORRECTION : Créer un nouvel objet sans navigation
                var newCompte = new CompteBancaire
                {
                    TiersId = compte.TiersId,
                    IBAN = compte.IBAN.Trim().ToUpper(), // Normaliser l'IBAN
                    BIC = compte.BIC?.Trim().ToUpper() ?? "",
                    Banque = compte.Banque.Trim(),
                    Pays = compte.Pays.Trim()
                };

                context.CompteBancaires.Add(newCompte);
                await context.SaveChangesAsync();

                // Recharger avec le Tiers
                var savedCompte = await GetCompteByIdAsync(newCompte.Id);

                return (true, "Compte bancaire créé avec succès.", savedCompte);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("UNIQUE") || innerMessage.Contains("duplicate"))
                {
                    return (false, "Ce compte bancaire existe déjà (IBAN ou contrainte unique).", null);
                }
                else if (innerMessage.Contains("FOREIGN KEY") || innerMessage.Contains("FK_"))
                {
                    return (false, "Le tiers sélectionné n'existe plus dans la base de données.", null);
                }
                else
                {
                    return (false, $"Erreur de base de données : {innerMessage}", null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.InnerException?.Message ?? ex.Message}", null);
            }
        }

        #endregion

        #region Modification

        /// <summary>
        /// Met à jour un compte bancaire existant
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateCompteAsync(CompteBancaire compte)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("CompteBancaire.Edit"))
                return (false, "Permission CompteBancaire.Edit requise pour modifier un compte bancaire.");

            try
            {
                var existingCompte = await context.CompteBancaires.FindAsync(compte.Id);

                if (existingCompte == null)
                    return (false, "Compte bancaire introuvable.");

                // Validation
                if (string.IsNullOrWhiteSpace(compte.IBAN))
                    return (false, "Le numéro IBAN est obligatoire.");

                if (string.IsNullOrWhiteSpace(compte.Banque))
                    return (false, "Le nom de la banque est obligatoire.");

                if (string.IsNullOrWhiteSpace(compte.Pays))
                    return (false, "Le pays est obligatoire.");

                // Vérifier l'unicité de l'IBAN
                var ibanExists = await context.CompteBancaires
                    .AnyAsync(c => c.IBAN == compte.IBAN && c.Id != compte.Id);

                if (ibanExists)
                    return (false, "Ce numéro IBAN existe déjà.");

                // Mettre à jour
                existingCompte.IBAN = compte.IBAN.Trim().ToUpper();
                existingCompte.BIC = compte.BIC?.Trim().ToUpper() ?? "";
                existingCompte.Banque = compte.Banque.Trim();
                existingCompte.Pays = compte.Pays.Trim();

                await context.SaveChangesAsync();

                return (true, "Compte bancaire modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un compte bancaire
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteCompteAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("CompteBancaire.Delete"))
                return (false, "Permission CompteBancaire.Delete requise pour supprimer un compte bancaire.");

            try
            {
                var compte = await context.CompteBancaires.FindAsync(id);

                if (compte == null)
                    return (false, "Compte bancaire introuvable.");

                context.CompteBancaires.Remove(compte);
                await context.SaveChangesAsync();

                return (true, "Compte bancaire supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valide le format d'un IBAN
        /// </summary>
        public bool ValidateIBAN(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
                return false;

            // Supprimer les espaces
            iban = iban.Replace(" ", "").ToUpper();

            // Vérifier la longueur (entre 15 et 34 caractères)
            if (iban.Length < 15 || iban.Length > 34)
                return false;

            // Vérifier que les 2 premiers caractères sont des lettres
            if (!char.IsLetter(iban[0]) || !char.IsLetter(iban[1]))
                return false;

            return true;
        }

        #endregion
    }
}