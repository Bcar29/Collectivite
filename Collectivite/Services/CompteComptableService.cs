using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Collectivite.Utils;

namespace Collectivite.Services
{
    public class CompteComptableService
    {
        private readonly AppDbContext _appDbContext;

        public CompteComptableService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // Récupérer tous les comptes comptables, page par page
        public async Task<(List<CompteComptable> Items, int TotalCount)> GetCompteComptablesAsync(int pageNumber = 1, int pageSize = 30, string? search = null)
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            var query = _appDbContext.CompteComptables.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.NumeroCompte.Contains(search) || c.IntituleCompte.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(c => c.ContrePartie)
                .Include(c => c.SousComptes)
                .OrderBy(c => c.NumeroCompte)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Récupérer tous les comptes de contrepartie (liste complète, non paginée — utilisée pour
        // remplir le sélecteur de compte parent dans le formulaire d'ajout/édition)
        public async Task<List<CompteComptable>> GetContrePartie()
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            return await _appDbContext.CompteComptables
                .Where(c => c.ContrePartieId == null)
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();
        }

        // Récupérer les comptes racines (sans contre-partie), page par page — utilisée par la grille
        public async Task<(List<CompteComptable> Items, int TotalCount)> GetComptesRacinesPagedAsync(int pageNumber = 1, int pageSize = 30, string? search = null)
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            var query = _appDbContext.CompteComptables
                .AsNoTracking()
                .Where(c => c.ContrePartieId == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.NumeroCompte.Contains(search) || c.IntituleCompte.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(c => c.ContrePartie)
                .Include(c => c.SousComptes)
                .OrderBy(c => c.NumeroCompte)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Récupérer un compte comptable par ID
        public async Task<CompteComptable?> GetCompteComptableByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            return await _appDbContext.CompteComptables
                .Include(c => c.ContrePartie)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Récupérer un compte comptable par numéro
        public async Task<CompteComptable?> GetCompteComptableByNumeroAsync(string numeroCompte)
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            return await _appDbContext.CompteComptables
                .Include(c => c.ContrePartie)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NumeroCompte == numeroCompte);
        }


        // Récupérer les sous-comptes d'un compte parent, page par page
        public async Task<(List<CompteComptable> Items, int TotalCount)> GetSousComptesAsync(int contrePartieId, int pageNumber = 1, int pageSize = 30, string? search = null)
        {
            if (!SessionManager.HasPermission("CompteComptable.View"))
                throw new UnauthorizedAccessException("Permission CompteComptable.View requise pour consulter les comptes comptables.");

            var query = _appDbContext.CompteComptables
                .AsNoTracking()
                .Where(c => c.ContrePartieId == contrePartieId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.NumeroCompte.Contains(search) || c.IntituleCompte.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(c => c.SousComptes)
                .OrderBy(c => c.NumeroCompte)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Ajouter un compte comptable
        public async Task<(bool Success, string Message, CompteComptable? Compte)> CreateCompteComptable(CompteComptable compte)
        {
            try
            {
                if (!SessionManager.HasPermission("CompteComptable.Create"))
                    return (false, "Permission CompteComptable.Create requise pour créer un compte comptable.", null);

                // Vérifier qu'il n'existe pas un compte avec le même numéro
                var existeNumero = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.NumeroCompte == compte.NumeroCompte);
                if (existeNumero)
                {
                    return (false, $"Le numéro de compte '{compte.NumeroCompte}' existe déjà.", null);
                }

                // Vérifier qu'il n'existe pas un compte avec le même intitulé
                var existeIntitule = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.IntituleCompte == compte.IntituleCompte);
                if (existeIntitule)
                {
                    return (false, $"'{compte.IntituleCompte}' existe déjà.", null);
                }

                // Vérifier si le compte parent existe (si spécifié)
                if (compte.ContrePartieId.HasValue)
                {
                    var parentExiste = await _appDbContext.CompteComptables
                        .AnyAsync(c => c.Id == compte.ContrePartieId.Value);
                    if (!parentExiste)
                    {
                        return (false, $"Le compte parent avec l'ID {compte.ContrePartieId} n'existe pas.", null);
                    }
                }

                _appDbContext.CompteComptables.Add(compte);
                await _appDbContext.SaveChangesAsync();

                return (true, "Compte créé avec succès.", compte);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création du compte comptable : {ex.Message}", null);
            }
        }

        // Mettre à jour un compte comptable
        public async Task<(bool Success, string Message)> UpdateCompteComptable(CompteComptable compte)
        {
            try
            {
                if (!SessionManager.HasPermission("CompteComptable.Edit"))
                    return (false, "Permission CompteComptable.Edit requise pour modifier un compte comptable.");

                var existingCompte = await _appDbContext.CompteComptables
                    .FirstOrDefaultAsync(c => c.Id == compte.Id);

                if (existingCompte == null)
                {
                    return (false, "Compte comptable non trouvé.");
                }

                // Vérifier qu'il n'existe pas déjà un compte avec le même numéro
                var existeNumero = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.NumeroCompte == compte.NumeroCompte && c.Id != compte.Id);
                if (existeNumero)
                {
                    return (false, $"Le numéro de compte '{compte.NumeroCompte}' existe déjà.");
                }

                // Vérifier qu'il n'existe pas déjà un compte avec le même intitulé
                var existeIntitule = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.IntituleCompte == compte.IntituleCompte && c.Id != compte.Id);
                if (existeIntitule)
                {
                    return (false, $"'{compte.IntituleCompte}' existe déjà.");
                }

                // Vérifier si le compte parent existe (si spécifié)
                if (compte.ContrePartieId.HasValue)
                {
                    // Empêcher qu'un compte soit son propre parent
                    if (compte.ContrePartieId == compte.Id)
                    {
                        return (false, "Un compte ne peut pas être son propre parent.");
                    }

                    var parentExiste = await _appDbContext.CompteComptables
                        .AnyAsync(c => c.Id == compte.ContrePartieId.Value);
                    if (!parentExiste)
                    {
                        return (false, $"Le compte parent avec l'ID {compte.ContrePartieId} n'existe pas.");
                    }

                    // Empêcher les références circulaires
                    if (await IsCircularReferenceAsync(compte.Id, compte.ContrePartieId.Value))
                    {
                        return (false, "Cette opération créerait une référence circulaire dans la hiérarchie des comptes.");
                    }
                }

                existingCompte.NumeroCompte = compte.NumeroCompte;
                existingCompte.IntituleCompte = compte.IntituleCompte;
                existingCompte.ContrePartieId = compte.ContrePartieId;

                await _appDbContext.SaveChangesAsync();

                return (true, "Compte comptable mis à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour du compte comptable : {ex.Message}");
            }
        }

        // Supprimer un compte comptable
        public async Task<(bool Success, string Message)> DeleteCompteComptableAsync(int idCompte)
        {
            try
            {
                if (!SessionManager.HasPermission("CompteComptable.Delete"))
                    return (false, "Permission CompteComptable.Delete requise pour supprimer un compte comptable.");

                var existingCompte = await _appDbContext.CompteComptables
                    .Include(c => c.SousComptes)
                    .FirstOrDefaultAsync(c => c.Id == idCompte);

                if (existingCompte == null)
                {
                    return (false, "Compte comptable non trouvé.");
                }

                // Vérifier s'il y a des sous-comptes
                if (existingCompte.SousComptes.Any())
                {
                    return (false, $"Impossible de supprimer le compte '{existingCompte.NumeroCompte}' car il contient des sous-comptes.");
                }

                _appDbContext.CompteComptables.Remove(existingCompte);
                await _appDbContext.SaveChangesAsync();

                return (true, "Compte supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression du compte comptable : {ex.Message}");
            }
        }

        // Méthode privée pour vérifier les références circulaires
        private async Task<bool> IsCircularReferenceAsync(int compteId, int parentId)
        {
            var currentParentId = parentId;

            while (currentParentId != 0)
            {
                if (currentParentId == compteId)
                {
                    return true; // Référence circulaire détectée
                }

                var parent = await _appDbContext.CompteComptables
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == currentParentId);

                if (parent == null || parent.ContrePartieId == null)
                {
                    break;
                }

                currentParentId = parent.ContrePartieId.Value;
            }

            return false;
        }
    }
}