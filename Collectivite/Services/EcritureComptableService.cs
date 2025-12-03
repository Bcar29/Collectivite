using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class EcritureComptableService
    {
        private readonly AppDbContext _appDbContext;

        public EcritureComptableService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // Récupérer toutes les écritures comptables
        public async Task<List<EcritureComptable>> GetEcrituresComptablesAsync()
        {
            return await _appDbContext.EcritureComptables
                .Include(e => e.CompteDebit)
                .Include(e => e.CompteCredit)
                .Include(e => e.OrdreRecette)
                .Include(e => e.Mandat)
                .OrderByDescending(e => e.DateEcriture)
                .ThenByDescending(e => e.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        // Récupérer une écriture par ID
        public async Task<EcritureComptable?> GetEcritureByIdAsync(int id)
        {
            return await _appDbContext.EcritureComptables
                .Include(e => e.CompteDebit)
                .Include(e => e.CompteCredit)
                .Include(e => e.OrdreRecette)
                .Include(e => e.Mandat)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // Récupérer les écritures par période
        public async Task<List<EcritureComptable>> GetEcrituresByPeriodeAsync(DateOnly dateDebut, DateOnly dateFin)
        {
            return await _appDbContext.EcritureComptables
                .Include(e => e.CompteDebit)
                .Include(e => e.CompteCredit)
                .Include(e => e.OrdreRecette)
                .Include(e => e.Mandat)
                .Where(e => e.DateEcriture >= dateDebut && e.DateEcriture <= dateFin)
                .OrderBy(e => e.DateEcriture)
                .ThenBy(e => e.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        // Récupérer les écritures par compte
        public async Task<List<EcritureComptable>> GetEcrituresByCompteAsync(int compteId)
        {
            return await _appDbContext.EcritureComptables
                .Include(e => e.CompteDebit)
                .Include(e => e.CompteCredit)
                .Include(e => e.OrdreRecette)
                .Include(e => e.Mandat)
                .Where(e => e.CompteDebitId == compteId || e.CompteCreditId == compteId)
                .OrderByDescending(e => e.DateEcriture)
                .ThenByDescending(e => e.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        // Créer une nouvelle écriture
        public async Task<(bool Success, string Message, EcritureComptable? Ecriture)> CreateEcritureAsync(EcritureComptable ecriture)
        {
            try
            {
                // Vérifier que les comptes existent
                var compteDebitExiste = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.Id == ecriture.CompteDebitId);
                if (!compteDebitExiste)
                {
                    return (false, "Le compte de débit n'existe pas.", null);
                }

                var compteCreditExiste = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.Id == ecriture.CompteCreditId);
                if (!compteCreditExiste)
                {
                    return (false, "Le compte de crédit n'existe pas.", null);
                }

                // Vérifier que les comptes sont différents
                if (ecriture.CompteDebitId == ecriture.CompteCreditId)
                {
                    return (false, "Les comptes de débit et de crédit doivent être différents.", null);
                }

                // Vérifier que le montant est positif
                if (ecriture.Montant <= 0)
                {
                    return (false, "Le montant doit être supérieur à zéro.", null);
                }

                _appDbContext.EcritureComptables.Add(ecriture);
                await _appDbContext.SaveChangesAsync();

                // Recharger avec les relations
                var ecritureCreee = await GetEcritureByIdAsync(ecriture.Id);

                return (true, "Écriture créée avec succès.", ecritureCreee);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création de l'écriture : {ex.Message}", null);
            }
        }

        // Mettre à jour une écriture
        public async Task<(bool Success, string Message)> UpdateEcritureAsync(EcritureComptable ecriture)
        {
            try
            {
                var existingEcriture = await _appDbContext.EcritureComptables
                    .FirstOrDefaultAsync(e => e.Id == ecriture.Id);

                if (existingEcriture == null)
                {
                    return (false, "Écriture non trouvée.");
                }

                // Vérifier que les comptes existent
                var compteDebitExiste = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.Id == ecriture.CompteDebitId);
                if (!compteDebitExiste)
                {
                    return (false, "Le compte de débit n'existe pas.");
                }

                var compteCreditExiste = await _appDbContext.CompteComptables
                    .AnyAsync(c => c.Id == ecriture.CompteCreditId);
                if (!compteCreditExiste)
                {
                    return (false, "Le compte de crédit n'existe pas.");
                }

                // Vérifier que les comptes sont différents
                if (ecriture.CompteDebitId == ecriture.CompteCreditId)
                {
                    return (false, "Les comptes de débit et de crédit doivent être différents.");
                }

                // Vérifier que le montant est positif
                if (ecriture.Montant <= 0)
                {
                    return (false, "Le montant doit être supérieur à zéro.");
                }

                existingEcriture.DateEcriture = ecriture.DateEcriture;
                existingEcriture.CompteDebitId = ecriture.CompteDebitId;
                existingEcriture.CompteCreditId = ecriture.CompteCreditId;
                existingEcriture.Montant = ecriture.Montant;
                existingEcriture.OrdreRecetteId = ecriture.OrdreRecetteId;
                existingEcriture.MandatId = ecriture.MandatId;

                await _appDbContext.SaveChangesAsync();

                return (true, "Écriture mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour de l'écriture : {ex.Message}");
            }
        }

        // Supprimer une écriture
        public async Task<(bool Success, string Message)> DeleteEcritureAsync(int id)
        {
            try
            {
                var ecriture = await _appDbContext.EcritureComptables
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (ecriture == null)
                {
                    return (false, "Écriture non trouvée.");
                }

                _appDbContext.EcritureComptables.Remove(ecriture);
                await _appDbContext.SaveChangesAsync();

                return (true, "Écriture supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression de l'écriture : {ex.Message}");
            }
        }

        // Calculer le solde d'un compte sur une période
        public async Task<(decimal SoldeDebiteur, decimal SoldeCrediteur, decimal Solde)> CalculerSoldeCompteAsync(
            int compteId, DateOnly? dateDebut = null, DateOnly? dateFin = null)
        {
            var query = _appDbContext.EcritureComptables.AsQueryable();

            if (dateDebut.HasValue)
                query = query.Where(e => e.DateEcriture >= dateDebut.Value);

            if (dateFin.HasValue)
                query = query.Where(e => e.DateEcriture <= dateFin.Value);

            var ecritures = await query
                .Where(e => e.CompteDebitId == compteId || e.CompteCreditId == compteId)
                .ToListAsync();

            decimal totalDebit = ecritures.Where(e => e.CompteDebitId == compteId).Sum(e => e.Montant);
            decimal totalCredit = ecritures.Where(e => e.CompteCreditId == compteId).Sum(e => e.Montant);
            decimal solde = totalDebit - totalCredit;

            return (totalDebit, totalCredit, solde);
        }

        // Vérifier l'équilibre sur une période
        public async Task<(bool IsEquilibre, decimal TotalDebit, decimal TotalCredit)> VerifierEquilibreAsync(
            DateOnly? dateDebut = null, DateOnly? dateFin = null)
        {
            var query = _appDbContext.EcritureComptables.AsQueryable();

            if (dateDebut.HasValue)
                query = query.Where(e => e.DateEcriture >= dateDebut.Value);

            if (dateFin.HasValue)
                query = query.Where(e => e.DateEcriture <= dateFin.Value);

            var totalMontant = await query.SumAsync(e => e.Montant);
            decimal totalDebit = totalMontant;
            decimal totalCredit = totalMontant;

            return (totalDebit == totalCredit, totalDebit, totalCredit);
        }
    }
}