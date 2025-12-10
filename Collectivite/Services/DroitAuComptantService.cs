
using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des Droits au Comptant
    /// </summary>
    public class DroitAuComptantService
    {
        private static AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Lecture

        /// <summary>
        /// Récupère la liste des droits au comptant (ordres de recette avec mouvements)
        /// </summary>
        public async Task<List<DroitAuComptantDTO>> GetDroitsAuComptantAsync()
        {
            using var context = CreateContext();

            var exerciceId = ExerciceService.Instance.CurrentExercice?.Id ?? 0;

            // Récupérer les écritures où MouvementId ET OrdreRecetteId sont renseignés
            var ecritures = await context.EcritureComptables
                .Where(ec => ec.MouvementId != null && ec.OrdreRecetteId != null)
                .Include(ec => ec.OrdreRecette)
                    .ThenInclude(o => o!.BudgetLine)
                        .ThenInclude(b => b!.Nommenclature)
                .Include(ec => ec.OrdreRecette)
                    .ThenInclude(o => o!.Tiers)
                .Include(ec => ec.Mouvement)
                .Where(ec => ec.OrdreRecette!.ExerciceId == exerciceId)
                .ToListAsync();

            // Grouper par OrdreRecetteId pour éviter les doublons
            var droits = ecritures
                .GroupBy(ec => ec.OrdreRecetteId)
                .Select(g =>
                {
                    var ecriture = g.First();
                    var ordre = ecriture.OrdreRecette!;
                    var mouvement = ecriture.Mouvement;
                    var budgetLine = ordre.BudgetLine;
                    var nomenclature = budgetLine?.Nommenclature;

                    return new DroitAuComptantDTO
                    {
                        OrdreRecetteId = ordre.Id,
                        NumeroOrdre = ordre.NumeroOrdre,
                        DateOrdre = DateOnly.FromDateTime(ordre.DateOrdre),
                        Imputation = nomenclature != null
                            ? $"{nomenclature.CodeNomenclature} - {nomenclature.Intitule}"
                            : "Non spécifié",
                        Debiteur = ordre.Tiers?.Nom ?? "Non spécifié",
                        MontantOrdre = ordre.MontantOrdre,
                        MontantEncaisse = mouvement?.Montant ?? ordre.MontantOrdre,
                        ModeReglement = DeterminerModeReglement(mouvement),
                        MouvementId = mouvement?.id,

                        // Informations supplémentaires pour la modification
                        BudgetLineId = ordre.BudgetLineId,
                        TiersId = ordre.TiersId,
                        Comptable = ordre.Comptable,
                        Motifs = ordre.Motifs,
                        RefVirement = mouvement?.RefVirement,
                        NumBanqueBenef = mouvement?.NumBanqueBenef,
                        RefCheque = mouvement?.RefChèque
                    };
                })
                .OrderByDescending(d => d.DateOrdre)
                .ThenByDescending(d => d.NumeroOrdre)
                .ToList();

            return droits;
        }

        /// <summary>
        /// Récupère les imputations disponibles (lignes budgétaires de type Recette)
        /// </summary>
        public async Task<List<ImputationDTO>> GetImputationsAsync()
        {
            using var context = CreateContext();

            var exerciceId = ExerciceService.Instance.CurrentExercice?.Id ?? 0;

            // Récupérer les BudgetLines de type Recette validées
            var budgetLines = await context.BudgetLines
                .Where(b => b.BudgetPrimitif.ExerciceId == exerciceId)
                .Where(b => b.BudgetPrimitif.Status == BudgetPrimitif.Statusbudget.VALIDATED)
                .Where(b => b.Nommenclature.Nature == NatureType.Recette)
                .Include(b => b.Nommenclature)
                .ToListAsync();

            // Filtrer les nomenclatures sans enfants (feuilles)
            var allNomenclatureIds = budgetLines.Select(b => b.NommenclatureId).ToList();
            var nomenclaturesWithChildren = await context.Nommenclatures
                .Where(n => n.ParentId != null && allNomenclatureIds.Contains(n.ParentId.Value))
                .Select(n => n.ParentId!.Value)
                .Distinct()
                .ToListAsync();

            var lignesSansEnfants = budgetLines
                .Where(b => !nomenclaturesWithChildren.Contains(b.NommenclatureId))
                .ToList();

            var imputations = new List<ImputationDTO>();

            foreach (var ligne in lignesSansEnfants)
            {
                var nomenclature = ligne.Nommenclature;
                if (nomenclature == null) continue;

                // Calculer le total des mouvements pour cette imputation
                var totalMouvement = await context.Mouvements
                    .Where(m => m.OrdreRecette != null && m.OrdreRecette.BudgetLineId == ligne.Id)
                    .SumAsync(m => (decimal?)m.Montant) ?? 0;

                // Récupérer le compte comptable associé
                var compteComptable = await context.CompteComptables
                    .FirstOrDefaultAsync(c => c.NumeroCompte == nomenclature.CodeNomenclature);

                imputations.Add(new ImputationDTO
                {
                    BudgetLineId = ligne.Id,
                    NumeroCompte = nomenclature.CodeNomenclature,
                    Libelle = nomenclature.Intitule,
                    TotalMouvement = totalMouvement,
                    CompteComptableId = compteComptable?.Id ?? 0
                });
            }

            return imputations.OrderBy(i => i.NumeroCompte).ToList();
        }

        /// <summary>
        /// Récupère la liste des tiers
        /// </summary>
        public async Task<List<Tiers>> GetTiersListAsync()
        {
            using var context = CreateContext();

            return await context.Tiers
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée une nouvelle opération de droit au comptant
        /// </summary>
        public async Task<(bool Success, string Message)> CreerOperationAsync(DroitAuComptantCreationDTO dto)
        {
            using var context = CreateContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Validations
                if (dto.BudgetLineId <= 0)
                    return (false, "L'imputation est obligatoire.");

                if (dto.Montant <= 0)
                    return (false, "Le montant doit être supérieur à zéro.");

                if (string.IsNullOrWhiteSpace(dto.Comptable))
                    return (false, "Le nom du comptable est obligatoire.");

                // Récupérer les entités nécessaires
                var exercice = ExerciceService.Instance.CurrentExercice;
                var commune = Properties.Settings.Default.CommuneId;

                if (exercice == null)
                    return (false, "Aucun exercice en cours.");

                var budgetLine = await context.BudgetLines
                    .Include(b => b.Nommenclature)
                    .FirstOrDefaultAsync(b => b.Id == dto.BudgetLineId);

                if (budgetLine == null)
                    return (false, "Ligne budgétaire introuvable.");

                // Récupérer le compte comptable de l'imputation
                var compteImputation = await context.CompteComptables
                    .FirstOrDefaultAsync(c => c.NumeroCompte == budgetLine.Nommenclature!.CodeNomenclature);

                if (compteImputation == null)
                    return (false, $"Compte comptable introuvable pour l'imputation {budgetLine.Nommenclature!.CodeNomenclature}.");

                // Récupérer le compte de trésorerie
                var compteTresorerie = await GetCompteTresorerieAsync(context, dto.ModeReglement);
                if (compteTresorerie == null)
                    return (false, "Compte de trésorerie introuvable.");

                // Générer le numéro d'ordre
                var numeroOrdre = await GenererNumeroOrdreAsync(context, exercice.Id);

                // 1. Créer l'OrdreRecette
                var ordreRecette = new OrdreRecette
                {
                    NumeroOrdre = numeroOrdre,
                    DateOrdre = dto.DateOrdre.ToDateTime(TimeOnly.MaxValue),
                    MontantOrdre = dto.Montant,
                    MontantOrdreLettre = ConvertirEnLettres(dto.Montant),
                    Motifs = dto.Motifs,
                    Comptable = dto.Comptable,
                    BudgetLineId = dto.BudgetLineId,
                    TiersId = dto.TiersId,
                    ExerciceId = exercice.Id,
                    CommuneId = commune
                };

                context.OrdreRecettes.Add(ordreRecette);
                await context.SaveChangesAsync();

                // 2. Créer le Mouvement
                var mouvement = new Mouvement
                {
                    Date = dto.DateOrdre,
                    Montant = dto.Montant,
                    idCompteComptable = compteTresorerie.Id,
                    idOrdreRecette = ordreRecette.Id,
                    RefVirement = dto.ModeReglement == ModeReglement.Virement ? dto.RefVirement : null,
                    NumBanqueBenef = dto.ModeReglement == ModeReglement.Virement ? dto.NumBanqueBenef : null,
                    RefChèque = dto.ModeReglement == ModeReglement.Cheque ? dto.RefCheque : null
                };

                context.Mouvements.Add(mouvement);
                await context.SaveChangesAsync();

                // 3. Créer l'EcritureComptable
                var ecritureComptable = new EcritureComptable
                {
                    DateEcriture = dto.DateOrdre,
                    CompteDebitId = compteTresorerie.Id,      // On débite la trésorerie (on reçoit l'argent)
                    CompteCreditId = compteImputation.Id,     // On crédite le compte de produit
                    Montant = dto.Montant,
                    OrdreRecetteId = ordreRecette.Id,
                    MouvementId = mouvement.id,
                };

                context.EcritureComptables.Add(ecritureComptable);

                // 4. Mettre à jour le MontantRealise de la BudgetLine
                budgetLine.MontantRealise += dto.Montant;

                await context.SaveChangesAsync();

                // 5. Recalculer la hiérarchie
                await OrdreRecetteService.RecalculateRealisation(
                    context,
                    budgetLine.NommenclatureId,
                    budgetLine.BudgetPrimitifId);

                await transaction.CommitAsync();

                return (true, $"Opération créée avec succès.\nN° Ordre: {numeroOrdre}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la création: {ex.Message}");
            }
        }

        #endregion

        #region Modification

        /// <summary>
        /// Modifie une opération de droit au comptant existante
        /// </summary>
        public async Task<(bool Success, string Message)> ModifierOperationAsync(DroitAuComptantModificationDTO dto)
        {
            using var context = CreateContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Validations
                if (dto.Montant <= 0)
                    return (false, "Le montant doit être supérieur à zéro.");

                if (string.IsNullOrWhiteSpace(dto.Comptable))
                    return (false, "Le nom du comptable est obligatoire.");

                // 1. Récupérer l'OrdreRecette
                var ordreRecette = await context.OrdreRecettes
                    .Include(o => o.BudgetLine)
                        .ThenInclude(b => b!.Nommenclature)
                    .FirstOrDefaultAsync(o => o.Id == dto.OrdreRecetteId);

                if (ordreRecette == null)
                    return (false, "Ordre de recette introuvable.");

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(ordreRecette.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                    return (false, "Impossible de modifier une opération sur un exercice clôturé.");

                // Mémoriser l'ancien montant pour la mise à jour du MontantRealise
                decimal ancienMontant = ordreRecette.MontantOrdre;
                decimal differenceMontant = dto.Montant - ancienMontant;

                // 2. Mettre à jour l'OrdreRecette
                ordreRecette.DateOrdre = dto.DateOrdre.ToDateTime(TimeOnly.MinValue) ;
                ordreRecette.MontantOrdre = dto.Montant;
                ordreRecette.MontantOrdreLettre = ConvertirEnLettres(dto.Montant);
                ordreRecette.Comptable = dto.Comptable;
                ordreRecette.Motifs = dto.Motifs;
                ordreRecette.TiersId = dto.TiersId;

                // 3. Récupérer et mettre à jour le Mouvement
                if (dto.MouvementId.HasValue)
                {
                    var mouvement = await context.Mouvements.FindAsync(dto.MouvementId.Value);
                    if (mouvement != null)
                    {
                        // Récupérer le compte de trésorerie selon le nouveau mode
                        var compteTresorerie = await GetCompteTresorerieAsync(context, dto.ModeReglement);
                        if (compteTresorerie == null)
                            return (false, "Compte de trésorerie introuvable.");

                        mouvement.Date = dto.DateOrdre;
                        mouvement.Montant = dto.Montant;
                        mouvement.idCompteComptable = compteTresorerie.Id;

                        // Mettre à jour les références selon le mode
                        mouvement.RefVirement = dto.ModeReglement == ModeReglement.Virement ? dto.RefVirement : null;
                        mouvement.NumBanqueBenef = dto.ModeReglement == ModeReglement.Virement ? dto.NumBanqueBenef : null;
                        mouvement.RefChèque = dto.ModeReglement == ModeReglement.Cheque ? dto.RefCheque : null;

                        // 4. Mettre à jour l'EcritureComptable
                        var ecriture = await context.EcritureComptables
                            .FirstOrDefaultAsync(ec => ec.MouvementId == mouvement.id);

                        if (ecriture != null)
                        {
                            ecriture.DateEcriture = dto.DateOrdre;
                            ecriture.Montant = dto.Montant;
                            ecriture.CompteDebitId = compteTresorerie.Id;
                        }
                    }
                }

                // 5. Mettre à jour le MontantRealise de la BudgetLine
                if (ordreRecette.BudgetLine != null && differenceMontant != 0)
                {
                    ordreRecette.BudgetLine.MontantRealise += differenceMontant;
                    if (ordreRecette.BudgetLine.MontantRealise < 0)
                        ordreRecette.BudgetLine.MontantRealise = 0;
                }

                await context.SaveChangesAsync();

                // 6. Recalculer la hiérarchie si le montant a changé
                if (ordreRecette.BudgetLine != null && differenceMontant != 0)
                {
                    await OrdreRecetteService.RecalculateRealisation(
                        context,
                        ordreRecette.BudgetLine.NommenclatureId,
                        ordreRecette.BudgetLine.BudgetPrimitifId);
                }

                await transaction.CommitAsync();

                return (true, $"Opération modifiée avec succès.\nN° Ordre: {ordreRecette.NumeroOrdre}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la modification: {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime une opération de droit au comptant
        /// </summary>
        public async Task<(bool Success, string Message)> SupprimerOperationAsync(int ordreRecetteId, int? mouvementId)
        {
            using var context = CreateContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Récupérer l'ordre de recette
                var ordreRecette = await context.OrdreRecettes
                    .Include(o => o.BudgetLine)
                    .FirstOrDefaultAsync(o => o.Id == ordreRecetteId);

                if (ordreRecette == null)
                    return (false, "Ordre de recette introuvable.");

                // 2. Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(ordreRecette.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                    return (false, "Impossible de supprimer une opération sur un exercice clôturé.");

                // Mémoriser les infos pour le recalcul
                int? nomenclatureId = ordreRecette.BudgetLine?.NommenclatureId;
                int? budgetPrimitifId = ordreRecette.BudgetLine?.BudgetPrimitifId;
                decimal montant = ordreRecette.MontantOrdre;

                // 3. Supprimer les écritures comptables liées
                var ecrituresComptables = await context.EcritureComptables
                    .Where(ec => ec.OrdreRecetteId == ordreRecetteId || ec.MouvementId == mouvementId)
                    .ToListAsync();

                if (ecrituresComptables.Any())
                {
                    context.EcritureComptables.RemoveRange(ecrituresComptables);
                }

                // 4. Supprimer le mouvement
                if (mouvementId.HasValue)
                {
                    var mouvement = await context.Mouvements.FindAsync(mouvementId.Value);
                    if (mouvement != null)
                    {
                        context.Mouvements.Remove(mouvement);
                    }
                }

                // 5. Mettre à jour le montant réalisé de la ligne budgétaire
                if (ordreRecette.BudgetLine != null)
                {
                    ordreRecette.BudgetLine.MontantRealise -= montant;
                    if (ordreRecette.BudgetLine.MontantRealise < 0)
                        ordreRecette.BudgetLine.MontantRealise = 0;
                }

                // 6. Supprimer l'ordre de recette
                context.OrdreRecettes.Remove(ordreRecette);

                await context.SaveChangesAsync();

                // 7. Recalculer la hiérarchie
                if (nomenclatureId.HasValue && budgetPrimitifId.HasValue)
                {
                    await OrdreRecetteService.RecalculateRealisation(
                        context,
                        nomenclatureId.Value,
                        budgetPrimitifId.Value);
                }

                await transaction.CommitAsync();

                return (true, "Opération supprimée avec succès.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la suppression: {ex.Message}");
            }
        }

        #endregion

        #region Utilitaires

        /// <summary>
        /// Récupère le compte de trésorerie selon le mode de règlement
        /// </summary>
        private async Task<CompteComptable?> GetCompteTresorerieAsync(AppDbContext context, ModeReglement modeReglement)
        {
            string numeroCompte = modeReglement switch
            {
                ModeReglement.Espece => "53",    // Caisse
                ModeReglement.Virement => "55",  // Banque
                ModeReglement.Cheque => "55",    // Banque
                _ => "53"
            };

            return await context.CompteComptables
                .Where(c => c.NumeroCompte.StartsWith(numeroCompte))
                .OrderBy(c => c.NumeroCompte.Length)
                .ThenBy(c => c.NumeroCompte)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Génère le prochain numéro d'ordre
        /// </summary>
        private async Task<string> GenererNumeroOrdreAsync(AppDbContext context, int exerciceId)
        {
            var count = await context.OrdreRecettes
                .Where(o => o.ExerciceId == exerciceId)
                .CountAsync();

            return $"DAC-{DateTime.Now:yyyy}-{(count + 1):D4}";
        }

        /// <summary>
        /// Détermine le mode de règlement à partir d'un mouvement
        /// </summary>
        private static string DeterminerModeReglement(Mouvement? mouvement)
        {
            if (mouvement == null) return "Non défini";
            if (!string.IsNullOrEmpty(mouvement.RefVirement)) return "Virement";
            if (!string.IsNullOrEmpty(mouvement.RefChèque)) return "Chèque";
            return "Espèces";
        }

        /// <summary>
        /// Convertit un montant en lettres
        /// </summary>
        private string ConvertirEnLettres(decimal montant)
        {
            return $"Arrêté à la somme de {montant:N0} francs guinéens";
        }

        #endregion
    }
}