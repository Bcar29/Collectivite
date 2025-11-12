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

        // Recuperer tous les comptes comptables 
        public async Task<List<CompteComptable>> GetCompteComptablesAsync()
        {
            return await _appDbContext.CompteComptables
                .AsNoTracking()  // ✅ Ne pas tracker
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        // ajouter un compte comptable
        public async Task<(bool Succes, string Message, CompteComptable? Commune)> CreateCompteComptable(CompteComptable compte)
        {
            try
            {
                // Verifier qu'il n'existe pas un compte de même intitulé

                var existe = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.IntituleCompte == compte.IntituleCompte);
                if (existe)
                {
                    return (false, $"{compte.IntituleCompte} existe déjà ", null);
                }
                _appDbContext.CompteComptables.Add(compte);
                await _appDbContext.SaveChangesAsync();
                return (true, $"la compte créé avec succès", compte);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la creation du compte comptable: {ex.Message}", null);
            }

        }

        // mettre à jour un compte comptable
        public async Task<(bool Success, string Message)> UpdateCompteComptable(CompteComptable compte)
        {
            try
            {
                var existingCompte = await _appDbContext.CompteComptables
                    .FirstOrDefaultAsync(n => n.Id == compte.Id);
                if (existingCompte == null)
                {
                    return (false, "Compte comptable non trouvé.");
                }
                // Validation : Vérifier qu'il n'existe pas déjà un compte avec le même intitulé
                var existe = await _appDbContext.CompteComptables
                    .AnyAsync(n => n.IntituleCompte == compte.IntituleCompte && n.Id != compte.Id);
                if (existe)
                {
                    return (false, $"{compte.IntituleCompte} existe déjà.");
                }
                existingCompte.IntituleCompte = compte.IntituleCompte;
                existingCompte.NumeroCompte = compte.NumeroCompte;
                
                
                await _appDbContext.SaveChangesAsync();
                return (true, "Compte Comptable mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour de la nommenclature : {ex.Message}");
            }
        }

        // supprimer un compte comptable
        public async Task<(bool Success, string Message)> DeleteCompteComptableAsync(int idCompte)
        {
            try
            {
                var existingCompte = await _appDbContext.CompteComptables
                    .FirstOrDefaultAsync(c => c.Id == idCompte);
                if (existingCompte == null)
                {
                    return (false, "Compte comptable non trouvée.");
                }
                _appDbContext.CompteComptables.Remove(existingCompte);
                await _appDbContext.SaveChangesAsync();
                return (true, "Compte supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression du Compte comptable : {ex.Message}");
            }
        }
    }
}
