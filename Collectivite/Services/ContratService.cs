using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Services
{
    public class ContratService
    {
        private readonly AppDbContext _appDbContext;
        public ContratService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        //recuper tous les exercices
        public async Task<List<Exercice>> GetAllExercie()
        {
            return await _appDbContext.Exercices
                .OrderByDescending(e => e.DateFin)
                .ToListAsync();
        }
        // Recuperer tous les contrats 
        public async Task<List<Contrats>> GetAllContratsAsync()
        {
            return await _appDbContext.Contrats
                .AsNoTracking()  // ✅ Ne pas tracker
                .Include(e => e.Exercice)
                .ToListAsync();
        }

        // ajouter un contrat
        public async Task<(bool Succes, string Message, Contrats? contrats)> CreateContratAsync(Contrats contrats)
        {
            try
            {
                var existe = await _appDbContext.Contrats
                    .AnyAsync(c => c.NumeroContrat == contrats.NumeroContrat);
                if (existe)
                {
                    return (false, $"{contrats.NumeroContrat} existe déjà ", null);
                }
                _appDbContext.Contrats.Add(contrats);
                await _appDbContext.SaveChangesAsync();
                return (true, $"le Contrat {contrats.NumeroContrat} ajoute avec succes", contrats);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la creation du contrat: {ex.Message}", null);
            }

        }

        // mettre à jour un contrat
        public async Task<(bool Succes, string Message)> UpdateContratsAsync(Contrats contrats)
        {
            try
            {
                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 1 : DÉTACHER TOUTES LES ENTITÉS TRACKÉES
                // ══════════════════════════════════════════════════════════
                var trackedEntries = _appDbContext.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Detached)
                    .ToList();

                foreach (var entry in trackedEntries)
                {
                    entry.State = EntityState.Detached;
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 2 : CHARGER L'ENTITÉ EXISTANTE
                // ══════════════════════════════════════════════════════════
                var existingContrat = await _appDbContext.Contrats
                    .FirstOrDefaultAsync(c => c.Id == contrats.Id);

                if (existingContrat == null)
                {
                    return (false, "Contrat non trouvé");
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 3 : VÉRIFIER L'UNICITÉ DU NUMERO CONTRAT
                // ══════════════════════════════════════════════════════════
                var existe = await _appDbContext.Contrats
                    .AnyAsync(c => c.NumeroContrat == contrats.NumeroContrat && c.Id != contrats.Id);

                if (existe)
                {
                    return (false, $"Ce contrat {contrats.NumeroContrat} existe déjà");
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 4 : METTRE À JOUR LES PROPRIÉTÉS
                // ══════════════════════════════════════════════════════════
                existingContrat.NumeroContrat = contrats.NumeroContrat;
                existingContrat.DateSignature = contrats.DateSignature;
                existingContrat.DateEcheance = contrats.DateEcheance;
                existingContrat.TiersId = contrats.TiersId;
                existingContrat.Tiers = contrats.Tiers;
                existingContrat.MontantContrat = contrats.MontantContrat;
                existingContrat.FichierJoin = contrats.FichierJoin;
                existingContrat.ExerciceId = contrats.ExerciceId;
                existingContrat.Exercice = contrats.Exercice;


                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 5 : SAUVEGARDER
                // ══════════════════════════════════════════════════════════
                await _appDbContext.SaveChangesAsync();

                return (true, "Contrat mis à jour avec succès");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour du contrat : {ex.Message}");
            }
        }

        //supprimer un contrat
        public async Task<(bool Succes, string Message)> DeleteContratAsync(int contratId)
        {
            try
            {
                var existingContrat = await _appDbContext.Contrats
                    .FirstOrDefaultAsync(c => c.Id == contratId);
                if (existingContrat == null)
                {
                    return (false, "Contrat non trouvé.");
                }
                _appDbContext.Contrats.Remove(existingContrat);
                await _appDbContext.SaveChangesAsync();
                return (true, "Contrat supprimé avec succès");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression du contrat : {ex.Message}");
            }
        }
    }
}
