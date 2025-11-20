using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Services
{
    public class CompteComptableService
    {
        private readonly AppDbContext _appDbContext;

        public CompteComptableService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // Récupérer tous les comptes comptables 
        public async Task<List<CompteComptable>> GetCompteComptablesAsync()
        {
            return await _appDbContext.CompteComptables
                .Include(c => c.CompteParent)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();
        }

        // Récupérer un compte comptable par ID
        public async Task<CompteComptable?> GetCompteComptableByIdAsync(int id)
        {
            return await _appDbContext.CompteComptables
                .Include(c => c.CompteParent)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Récupérer un compte comptable par numéro
        public async Task<CompteComptable?> GetCompteComptableByNumeroAsync(string numeroCompte)
        {
            return await _appDbContext.CompteComptables
                .Include(c => c.CompteParent)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NumeroCompte == numeroCompte);
        }

        // Récupérer les comptes racines (sans parent)
        public async Task<List<CompteComptable>> GetComptesRacinesAsync()
        {
            return await _appDbContext.CompteComptables
                .Where(c => c.CompteParentId == null)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();
        }

        // Récupérer les sous-comptes d'un compte parent
        public async Task<List<CompteComptable>> GetSousComptesAsync(int compteParentId)
        {
            return await _appDbContext.CompteComptables
                .Where(c => c.CompteParentId == compteParentId)
                .Include(c => c.SousComptes)
                .AsNoTracking()
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();
        }

        // Ajouter un compte comptable
        public async Task<(bool Success, string Message, CompteComptable? Compte)> CreateCompteComptable(CompteComptable compte)
        {
            try
            {
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
                if (compte.CompteParentId.HasValue)
                {
                    var parentExiste = await _appDbContext.CompteComptables
                        .AnyAsync(c => c.Id == compte.CompteParentId.Value);
                    if (!parentExiste)
                    {
                        return (false, $"Le compte parent avec l'ID {compte.CompteParentId} n'existe pas.", null);
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
                if (compte.CompteParentId.HasValue)
                {
                    // Empêcher qu'un compte soit son propre parent
                    if (compte.CompteParentId == compte.Id)
                    {
                        return (false, "Un compte ne peut pas être son propre parent.");
                    }

                    var parentExiste = await _appDbContext.CompteComptables
                        .AnyAsync(c => c.Id == compte.CompteParentId.Value);
                    if (!parentExiste)
                    {
                        return (false, $"Le compte parent avec l'ID {compte.CompteParentId} n'existe pas.");
                    }

                    // Empêcher les références circulaires
                    if (await IsCircularReferenceAsync(compte.Id, compte.CompteParentId.Value))
                    {
                        return (false, "Cette opération créerait une référence circulaire dans la hiérarchie des comptes.");
                    }
                }

                existingCompte.NumeroCompte = compte.NumeroCompte;
                existingCompte.IntituleCompte = compte.IntituleCompte;
                existingCompte.CompteParentId = compte.CompteParentId;

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

                if (parent == null || parent.CompteParentId == null)
                {
                    break;
                }

                currentParentId = parent.CompteParentId.Value;
            }

            return false;
        }
    }
}