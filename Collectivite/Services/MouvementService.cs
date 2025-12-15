
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
    /// Service pour la gestion des mouvements (paiements de mandats et encaissements d'ordres de recette)
    /// </summary>
    public class MouvementService : IMouvementService
    {
        private readonly AppDbContext _context;
        private readonly EcritureComptableMouvementHelper _ecritureHelper;

        public MouvementService(AppDbContext context)
        {
            _context = context;
            _ecritureHelper = new EcritureComptableMouvementHelper(context);
        }

        #region MANDATS

        /// <summary>
        /// Récupère les mandats non totalement payés
        /// </summary>
        public async Task<List<MandatPaiementDTO>> GetMandatsNonPayesAsync()
        {
            var mandats = await _context.Mandats
                .Where(m => m.Etat == Mandat.EtatMandat.Validé)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();

            var result = new List<MandatPaiementDTO>();

            foreach (var mandat in mandats)
            {
                // Calculer le montant déjà payé
                var montantPaye = await _context.Mouvements
                    .Where(mv => mv.idMandat == mandat.Id)
                    .SumAsync(mv => mv.Montant);

                var dto = new MandatPaiementDTO
                {
                    Id = mandat.Id,
                    NumeroMandat = mandat.NumeroMandat,
                    Bordereau = mandat.Bordereau,
                    DateEmission = mandat.DateEmission,
                    Objet = mandat.Objet,
                    Motif = mandat.Objet,
                    Beneficiaire = mandat.Engagement?.Tiers?.Nom
                        ?? mandat.Engagement?.Tiers?.RaisonSociale
                        ?? "Tiers non défini",
                    MontantBrut = mandat.MontantBrut,
                    MontantNet = mandat.MontantNet,
                    MontantPaye = montantPaye,
                    Etat = mandat.Etat
                };

                // Ne garder que les mandats non totalement payés
                if (!dto.EstTotalementPaye)
                {
                    result.Add(dto);
                }
            }

            return result;
        }

        /// <summary>
        /// Récupère un mandat avec son état de paiement
        /// </summary>
        public async Task<MandatPaiementDTO?> GetMandatPaiementAsync(int mandatId)
        {
            var mandat = await _context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .FirstOrDefaultAsync(m => m.Id == mandatId);

            if (mandat == null) return null;

            var montantPaye = await _context.Mouvements
                .Where(mv => mv.idMandat == mandatId)
                .SumAsync(mv => mv.Montant);

            return new MandatPaiementDTO
            {
                Id = mandat.Id,
                NumeroMandat = mandat.NumeroMandat,
                Bordereau = mandat.Bordereau,
                DateEmission = mandat.DateEmission,
                Objet = mandat.Objet,
                Motif = mandat.Objet,
                Beneficiaire = mandat.Engagement?.Tiers?.Nom
                    ?? mandat.Engagement?.Tiers?.RaisonSociale
                    ?? "Tiers non défini",
                MontantBrut = mandat.MontantBrut,
                MontantNet = mandat.MontantNet,
                MontantPaye = montantPaye,
                Etat = mandat.Etat
            };
        }

        /// <summary>
        /// Récupère l'historique des paiements d'un mandat
        /// </summary>
        public async Task<List<MouvementHistoriqueDTO>> GetHistoriquePaiementsMandatAsync(int mandatId)
        {
            var mouvements = await _context.Mouvements
                .Include(m => m.CompteComptable)
                .Where(m => m.idMandat == mandatId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            return mouvements.Select(m => new MouvementHistoriqueDTO
            {
                Id = m.id,
                Date = m.Date,
                Montant = m.Montant,
                ModeReglement = DeterminerModeReglement(m),
                Reference = m.RefVirement ?? m.RefChèque ?? "-",
                CompteComptable = m.CompteComptable?.NumeroCompte + " - " + m.CompteComptable?.IntituleCompte
            }).ToList();
        }

        /// <summary>
        /// Effectue un paiement sur un mandat
        /// </summary>
        public async Task<(bool Success, string Message, Mouvement? Mouvement)> PayerMandatAsync(MouvementCreationDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Vérifier que le mandat existe
                if (!dto.IdMandat.HasValue)
                {
                    return (false, "L'identifiant du mandat est requis.", null);
                }

                var mandat = await _context.Mandats
                    .Include(m => m.Engagement)
                    .FirstOrDefaultAsync(m => m.Id == dto.IdMandat.Value);

                if (mandat == null)
                {
                    return (false, "Le mandat spécifié n'existe pas.", null);
                }

                // 2. Vérifier le montant restant
                var montantDejaPaye = await _context.Mouvements
                    .Where(mv => mv.idMandat == dto.IdMandat.Value)
                    .SumAsync(mv => mv.Montant);

                var montantRestant = mandat.MontantNet - montantDejaPaye;

                if (dto.Montant <= 0)
                {
                    return (false, "Le montant doit être supérieur à zéro.", null);
                }

                if (dto.Montant > montantRestant)
                {
                    return (false, $"Le montant saisi ({dto.Montant:N0} GNF) dépasse le montant restant à payer ({montantRestant:N0} GNF).", null);
                }

                // 3. Récupérer le compte de trésorerie
                var compteTresorerie = await _ecritureHelper.GetCompteTresorerieAsync(dto.ModeReglement);

                // 4. Créer le mouvement
                var mouvement = new Mouvement
                {
                    Date = dto.Date,
                    Montant = dto.Montant,
                    idCompteComptable = compteTresorerie.Id,
                    idMandat = dto.IdMandat.Value,
                    RefVirement = dto.ModeReglement == ModeReglement.Virement ? dto.RefVirement : null,
                    NumBanqueBenef = dto.ModeReglement == ModeReglement.Virement ? dto.NumBanqueBenef : null,
                    RefChèque = dto.ModeReglement == ModeReglement.Cheque ? dto.RefCheque : null,
                    FichierJoint = dto.FichierJoint,
                    FileName = dto.FileName
                };

                _context.Mouvements.Add(mouvement);
                await _context.SaveChangesAsync();

                // 5. Générer l'écriture comptable
                var ecriture = await _ecritureHelper.GenererEcriturePaiementMandatAsync(
                    mouvement, mandat, dto.ModeReglement);

                _context.EcritureComptables.Add(ecriture);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true, $"Paiement de {dto.Montant:N0} GNF enregistré avec succès sur le mandat {mandat.NumeroMandat}.", mouvement);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors du paiement : {ex.Message}", null);
            }
        }

        #endregion

        #region ORDRES DE RECETTE

        /// <summary>
        /// Récupère les ordres de recette non totalement encaissés
        /// </summary>
        public async Task<List<OrdreRecetteEncaissementDTO>> GetOrdresRecetteNonEncaissesAsync()
        {
            var ordres = await _context.OrdreRecettes
                .Where(o => o.Etat == OrdreRecette.EtatOdre.Validé)
                .Include(o => o.Tiers)
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();

            var result = new List<OrdreRecetteEncaissementDTO>();

            foreach (var ordre in ordres)
            {
                // Calculer le montant déjà encaissé
                var montantEncaisse = await _context.Mouvements
                    .Where(mv => mv.idOrdreRecette == ordre.Id)
                    .SumAsync(mv => mv.Montant);

                var dto = new OrdreRecetteEncaissementDTO
                {
                    Id = ordre.Id,
                    NumeroOrdre = ordre.NumeroOrdre,
                    DateOrdre = ordre.DateOrdre,
                    Motifs = ordre.Motifs,
                    Debiteur = ordre.Tiers?.Nom ?? "Non spécifié",
                    MontantOrdre = ordre.MontantOrdre,
                    MontantEncaisse = montantEncaisse
                };

                // Ne garder que les ordres non totalement encaissés
                if (!dto.EstTotalementEncaisse)
                {
                    result.Add(dto);
                }
            }

            return result;
        }

        /// <summary>
        /// Récupère un ordre de recette avec son état d'encaissement
        /// </summary>
        public async Task<OrdreRecetteEncaissementDTO?> GetOrdreRecetteEncaissementAsync(int ordreRecetteId)
        {
            var ordre = await _context.OrdreRecettes
                .Include(o => o.Tiers)
                .FirstOrDefaultAsync(o => o.Id == ordreRecetteId);

            if (ordre == null) return null;

            var montantEncaisse = await _context.Mouvements
                .Where(mv => mv.idOrdreRecette == ordreRecetteId)
                .SumAsync(mv => mv.Montant);

            return new OrdreRecetteEncaissementDTO
            {
                Id = ordre.Id,
                NumeroOrdre = ordre.NumeroOrdre,
                DateOrdre = ordre.DateOrdre,
                Motifs = ordre.Motifs,
                Debiteur = ordre.Tiers?.Nom ?? "Non spécifié",
                MontantOrdre = ordre.MontantOrdre,
                MontantEncaisse = montantEncaisse
            };
        }

        /// <summary>
        /// Récupère l'historique des encaissements d'un ordre de recette
        /// </summary>
        public async Task<List<MouvementHistoriqueDTO>> GetHistoriqueEncaissementsOrdreRecetteAsync(int ordreRecetteId)
        {
            var mouvements = await _context.Mouvements
                .Include(m => m.CompteComptable)
                .Where(m => m.idOrdreRecette == ordreRecetteId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            return mouvements.Select(m => new MouvementHistoriqueDTO
            {
                Id = m.id,
                Date = m.Date,
                Montant = m.Montant,
                ModeReglement = DeterminerModeReglement(m),
                Reference = m.RefVirement ?? m.RefChèque ?? "-",
                CompteComptable = m.CompteComptable?.NumeroCompte + " - " + m.CompteComptable?.IntituleCompte
            }).ToList();
        }

        /// <summary>
        /// Effectue un encaissement sur un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message, Mouvement? Mouvement)> EncaisserOrdreRecetteAsync(MouvementCreationDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Vérifier que l'ordre de recette existe
                if (!dto.IdOrdreRecette.HasValue)
                {
                    return (false, "L'identifiant de l'ordre de recette est requis.", null);
                }

                var ordreRecette = await _context.OrdreRecettes
                    .Include(o => o.Tiers)
                    .FirstOrDefaultAsync(o => o.Id == dto.IdOrdreRecette.Value);

                if (ordreRecette == null)
                {
                    return (false, "L'ordre de recette spécifié n'existe pas.", null);
                }

                // 2. Vérifier le montant restant
                var montantDejaEncaisse = await _context.Mouvements
                    .Where(mv => mv.idOrdreRecette == dto.IdOrdreRecette.Value)
                    .SumAsync(mv => mv.Montant);

                var montantRestant = ordreRecette.MontantOrdre - montantDejaEncaisse;

                if (dto.Montant <= 0)
                {
                    return (false, "Le montant doit être supérieur à zéro.", null);
                }

                if (dto.Montant > montantRestant)
                {
                    return (false, $"Le montant saisi ({dto.Montant:N0} GNF) dépasse le montant restant à encaisser ({montantRestant:N0} GNF).", null);
                }

                // 3. Récupérer le compte de trésorerie
                var compteTresorerie = await _ecritureHelper.GetCompteTresorerieAsync(dto.ModeReglement);

                // 4. Créer le mouvement
                var mouvement = new Mouvement
                {
                    Date = dto.Date,
                    Montant = dto.Montant,
                    idCompteComptable = compteTresorerie.Id,
                    idOrdreRecette = dto.IdOrdreRecette.Value,
                    RefVirement = dto.ModeReglement == ModeReglement.Virement ? dto.RefVirement : null,
                    NumBanqueBenef = dto.ModeReglement == ModeReglement.Virement ? dto.NumBanqueBenef : null,
                    RefChèque = dto.ModeReglement == ModeReglement.Cheque ? dto.RefCheque : null,
                    FichierJoint = dto.FichierJoint,
                    FileName = dto.FileName
                };

                _context.Mouvements.Add(mouvement);
                await _context.SaveChangesAsync();

                // 5. Générer l'écriture comptable
                var ecriture = await _ecritureHelper.GenererEcritureEncaissementOrdreRecetteAsync(
                    mouvement, ordreRecette, dto.ModeReglement);

                _context.EcritureComptables.Add(ecriture);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true, $"Encaissement de {dto.Montant:N0} GNF enregistré avec succès sur l'ordre de recette {ordreRecette.NumeroOrdre}.", mouvement);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de l'encaissement : {ex.Message}", null);
            }
        }

        #endregion

        #region UTILITAIRES

        /// <summary>
        /// Récupère un mouvement par son ID
        /// </summary>
        public async Task<Mouvement?> GetMouvementByIdAsync(int id)
        {
            return await _context.Mouvements
                .Include(m => m.CompteComptable)
                .Include(m => m.Mandat)
                .Include(m => m.OrdreRecette)
                .FirstOrDefaultAsync(m => m.id == id);
        }

        /// <summary>
        /// Supprime un mouvement (avec annulation de l'écriture comptable associée)
        /// </summary>
        public async Task<(bool Success, string Message)> SupprimerMouvementAsync(int mouvementId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var mouvement = await _context.Mouvements
                    .FirstOrDefaultAsync(m => m.id == mouvementId);

                if (mouvement == null)
                {
                    return (false, "Le mouvement spécifié n'existe pas.");
                }

                // Supprimer le mouvement
                _context.Mouvements.Remove(mouvement);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true, "Mouvement supprimé avec succès.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère les comptes de trésorerie (53 et 55)
        /// </summary>
        public async Task<List<CompteComptable>> GetComptesTresorerieAsync()
        {
            return await _context.CompteComptables
                .Where(c => c.NumeroCompte.StartsWith("53") || c.NumeroCompte.StartsWith("55"))
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();
        }

        /// <summary>
        /// Détermine le mode de règlement à partir d'un mouvement
        /// </summary>
        private string DeterminerModeReglement(Mouvement mouvement)
        {
            if (!string.IsNullOrEmpty(mouvement.RefVirement))
                return "Virement";
            if (!string.IsNullOrEmpty(mouvement.RefChèque))
                return "Chèque";
            return "Espèces";
        }

        #endregion
    }
}