using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectivite.Utils;

namespace Collectivite.Services
{
    public class RemaniementService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les remaniements avec leurs relations
        /// </summary>
        public async Task<List<Remaniement>> GetAllRemaniementsAsync()
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            // ✅ CORRECTION : NE PAS charger BudgetLine.Remaniements (cycle)
            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .AsNoTracking()
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les lignes budgétaires appartenant à un budget primitif validé
        /// (avec leurs remaniements pour le calcul des totaux)
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesForValidatedBudgetAsync()
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les lignes budgétaires pour remaniements.");

            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<BudgetLine>();
            }

            var budgetPrimitif = await context.BudgetsPrimitifs
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Status == BudgetPrimitif.Statusbudget.VALIDATED &&
                                           bp.ExerciceId == exerciceService.CurrentExercice.Id);

            if (budgetPrimitif == null)
            {
                return new List<BudgetLine>();
            }

            var existing = await context.BudgetLines
                .Where(bl => bl.BudgetPrimitifId == budgetPrimitif.Id)
                .Include(bl => bl.Nommenclature)
                .ThenInclude(n => n.Enfants)
                .Include(bl => bl.Remaniements)
                .Include(bl => bl.BudgetPrimitif)
                .AsNoTracking()
                .ToListAsync();

            var existingByNomenclatureId = existing.ToDictionary(bl => bl.NommenclatureId);

            var allNomenclatures = await context.Nommenclatures
                .Include(n => n.Enfants)
                .AsNoTracking()
                .ToListAsync();

            // 🆕 Fabriquer une BudgetLine virtuelle (Id = 0) pour toute nomenclature (chapitre,
            // article, paragraphe ou feuille) qui n'a pas encore de ligne dans ce budget - même
            // pattern que BudgetLineService.GetFullBudgetLinesAsync (mode Tableau du Budget
            // Primitif). Nécessaire pour : (1) afficher la hiérarchie complète même si une
            // feuille n'a jamais été budgétée, (2) permettre de sélectionner cette feuille pour
            // créer un remaniement "ex nihilo" (ligne totalement nouvelle).
            var result = new List<BudgetLine>(allNomenclatures.Count);
            foreach (var n in allNomenclatures)
            {
                if (existingByNomenclatureId.TryGetValue(n.Id, out var bl))
                {
                    bl.Nommenclature = n;
                    result.Add(bl);
                }
                else
                {
                    result.Add(new BudgetLine
                    {
                        Id = 0,
                        BudgetPrimitifId = budgetPrimitif.Id,
                        NommenclatureId = n.Id,
                        Nommenclature = n,
                        BudgetPrimitif = budgetPrimitif,
                        MontantPrevu = 0,
                        MontantActu = 0
                    });
                }
            }

            return result
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ThenBy(bl => bl.Nommenclature.SousParagraphe)
                .ToList();
        }

        /// <summary>
        /// Récupère un remaniement par son ID
        /// </summary>
        public async Task<Remaniement?> GetRemaniementByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            // ✅ CORRECTION : NE PAS charger BudgetLine.Remaniements (cycle)
            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Récupère les remaniements par ligne budgétaire
        /// </summary>
        public async Task<List<Remaniement>> GetRemaniementsByBudgetLineAsync(int budgetLineId)
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(r => r.IdBudgetLine == budgetLineId)
                .AsNoTracking()
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les lignes budgétaires sans enfants (feuilles de l'arbre)
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesSansEnfantsAsync()
        {
            using var context = CreateContext();

            // Si un exercice courant existe et qu'il est clôturé, on ne permet plus
            // de charger les lignes pour remaniement.
            var exerciceService = ExerciceService.Instance;
            if (exerciceService.CurrentExercice != null && exerciceService.CurrentExercice.EstCloture)
            {
                return new List<BudgetLine>();
            }

            // ✅ ICI on peut charger les Remaniements car on part de BudgetLine
            var allLines = await context.BudgetLines
                .Include(bl => bl.Nommenclature)
                .Include(bl => bl.Remaniements) // ✅ OK : pour calculer MontantDefinitif
                .AsNoTracking()
                .ToListAsync();

            // Récupérer tous les Nommenclatures avec leurs enfants
            var allNommenclatures = await context.Nommenclatures
                .Include(n => n.Enfants)
                .AsNoTracking()
                .ToListAsync();

            // Filtrer pour garder seulement celles qui n'ont pas d'enfants
            var nommenclaturesSansEnfants = allNommenclatures
                .Where(n => n.Enfants == null || !n.Enfants.Any())
                .Select(n => n.Id)
                .ToHashSet();

            // Retourner les BudgetLines correspondantes
            return allLines
                .Where(bl => nommenclaturesSansEnfants.Contains(bl.NommenclatureId))
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ToList();
        }

        /// <summary>
        /// 🆕 Récupère toutes les lignes budgétaires parentes dans la hiérarchie
        /// </summary>
        private async Task<List<BudgetLine>> GetParentBudgetLinesAsync(AppDbContext context, int nommenclatureId, int budgetPrimitifId)
        {
            var parentLines = new List<BudgetLine>();

            // Charger la nomenclature avec son parent
            var currentNommenclature = await context.Nommenclatures
                .Include(n => n.Parent)
                .FirstOrDefaultAsync(n => n.Id == nommenclatureId);

            // Remonter la hiérarchie
            while (currentNommenclature?.Parent != null)
            {
                // Trouver la BudgetLine correspondant à ce parent
                var parentBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => 
                        bl.NommenclatureId == currentNommenclature.Parent.Id && 
                        bl.BudgetPrimitifId == budgetPrimitifId);

                if (parentBudgetLine != null)
                {
                    parentLines.Add(parentBudgetLine);
                }

                // Remonter au parent suivant
                currentNommenclature = await context.Nommenclatures
                    .Include(n => n.Parent)
                    .FirstOrDefaultAsync(n => n.Id == currentNommenclature.ParentId);
            }

            return parentLines;
        }

        /// <summary>
        /// Crée toute BudgetLine ancêtre manquante (chapitre/article/paragraphe) pour la
        /// nomenclature donnée, sans modifier le MontantPrevu des ancêtres déjà existants - un
        /// remaniement ne doit jamais toucher au MontantPrevu, seulement aux Remaniements.
        /// Nécessaire pour qu'un remaniement "ex nihilo" (nomenclature jamais budgétée) trouve
        /// bien une ligne parente à chaque niveau lors de la propagation, même si toute la
        /// branche (chapitre y compris) n'a jamais été budgétée.
        /// </summary>
        private async Task EnsureBudgetLineAncestorChainAsync(AppDbContext context, int nommenclatureId, int budgetPrimitifId)
        {
            var currentNommenclature = await context.Nommenclatures
                .Include(n => n.Parent)
                .FirstOrDefaultAsync(n => n.Id == nommenclatureId);

            while (currentNommenclature?.Parent != null)
            {
                var parentId = currentNommenclature.Parent.Id;

                var parentBudgetLine = await context.BudgetLines
                    .FirstOrDefaultAsync(bl => bl.NommenclatureId == parentId && bl.BudgetPrimitifId == budgetPrimitifId);

                if (parentBudgetLine == null)
                {
                    context.BudgetLines.Add(new BudgetLine
                    {
                        BudgetPrimitifId = budgetPrimitifId,
                        NommenclatureId = parentId,
                        MontantPrevu = 0,
                        MontantActu = 0,
                        EstAjouteParRemaniement = true
                    });
                    await context.SaveChangesAsync();
                }

                currentNommenclature = await context.Nommenclatures
                    .Include(n => n.Parent)
                    .FirstOrDefaultAsync(n => n.Id == parentId);
            }
        }

        #endregion

        #region Création

        public async Task<(bool Success, string Message, Remaniement? Remaniement)> CreateRemaniementAsync(
            Remaniement remaniement, TypeRemaniement type, BudgetLine? selectedBudgetLine = null)
        {
            if (!SessionManager.HasPermission("Remaniement.Create"))
                return (false,
                    "Permission Remaniement.Create requise pour créer un remaniement.",
                    null);

            using var context = CreateContext();
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                (bool Success, string Message, Remaniement? Remaniement) result;

                await using var transaction =
                    await context.Database.BeginTransactionAsync();

                try
                {
                    // 🔹 Ligne "ex nihilo" : nomenclature jamais budgétée dans le Budget Primitif
                    // d'origine (ligne virtuelle Id = 0 affichée dans la grille de remaniement).
                    // On crée la BudgetLine réelle (et ses ancêtres manquants) uniquement
                    // maintenant, à l'intérieur de la transaction du remaniement, pour qu'un
                    // échec plus loin annule aussi cette création.
                    if (remaniement.IdBudgetLine <= 0)
                    {
                        if (selectedBudgetLine == null ||
                            selectedBudgetLine.NommenclatureId <= 0 ||
                            selectedBudgetLine.BudgetPrimitifId <= 0)
                            return (false, "La ligne budgétaire est obligatoire.", null);

                        await EnsureBudgetLineAncestorChainAsync(
                            context, selectedBudgetLine.NommenclatureId, selectedBudgetLine.BudgetPrimitifId);

                        var newLeafLine = new BudgetLine
                        {
                            BudgetPrimitifId = selectedBudgetLine.BudgetPrimitifId,
                            NommenclatureId = selectedBudgetLine.NommenclatureId,
                            MontantPrevu = 0,
                            MontantActu = 0,
                            EstAjouteParRemaniement = true
                        };

                        context.BudgetLines.Add(newLeafLine);
                        await context.SaveChangesAsync();

                        remaniement.IdBudgetLine = newLeafLine.Id;
                    }

                    // 🔹 Validations
                    if (remaniement.IdBudgetLine <= 0)
                        return (false, "La ligne budgétaire est obligatoire.", null);

                    if (remaniement.Montant <= 0)
                        return (false, "Le montant doit être supérieur à zéro.", null);

                    if (string.IsNullOrWhiteSpace(remaniement.Motif))
                        return (false, "Le motif est obligatoire.", null);

                    // 🔹 Charger la ligne budgétaire
                    var budgetLine = await context.BudgetLines
                        .Include(bl => bl.Nommenclature)
                        .Include(bl => bl.Remaniements)
                        .Include(bl => bl.BudgetPrimitif)
                        .FirstOrDefaultAsync(bl => bl.Id == remaniement.IdBudgetLine);

                    if (budgetLine == null)
                        return (false, "Ligne budgétaire introuvable.", null);

                    // 🔹 Vérifier que c’est une feuille (pas d’enfants)
                    var hasChildren = await context.Nommenclatures
                        .AnyAsync(n => n.ParentId == budgetLine.NommenclatureId);

                    if (hasChildren)
                        return (false,
                            "Impossible de créer un remaniement sur une ligne avec des sous-lignes.",
                            null);

                    // 🔹 Validation remaniement en moins
                    if (type == TypeRemaniement.en_moins)
                    {
                        var montantDefinitifActuel = budgetLine.MontantDefinitif;
                        var nouveauMontant =
                            montantDefinitifActuel - (decimal)remaniement.Montant;

                        if (nouveauMontant < 0)
                        {
                            return (false,
                                $"⚠️ Impossible : le remaniement rendrait le montant négatif.\n\n" +
                                $"Montant définitif actuel : {montantDefinitifActuel:N0} GNF\n" +
                                $"Remaniement demandé : -{remaniement.Montant:N0} GNF\n" +
                                $"Montant résultant : {nouveauMontant:N0} GNF ❌",
                                null);
                        }
                    }

                    // 🔹 Récupérer les lignes parentes
                    var parentBudgetLines = await GetParentBudgetLinesAsync(
                        context,
                        budgetLine.NommenclatureId,
                        budgetLine.BudgetPrimitifId);

                    // 1️⃣ Remaniement enfant
                    var newRemaniement = new Remaniement
                    {
                        IdBudgetLine = remaniement.IdBudgetLine,
                        Date = remaniement.Date,
                        Montant = remaniement.Montant,
                        Motif = remaniement.Motif.Trim(),
                        TypeRemaniement = type
                    };

                    context.Remaniements.Add(newRemaniement);
                    await context.SaveChangesAsync();

                    var remaniementsCreated = new List<string>
                        {
                            $"✅ Ligne enfant : {budgetLine.Nommenclature.Intitule} ({remaniement.Montant:N0} GNF)"
                        };

                    // 2️⃣ Remaniements parents
                    foreach (var parentLine in parentBudgetLines)
                    {
                        var parentRemaniement = new Remaniement
                        {
                            IdBudgetLine = parentLine.Id,
                            Date = remaniement.Date,
                            Montant = remaniement.Montant,
                            Motif = $"[Propagation] {remaniement.Motif.Trim()}",
                            TypeRemaniement = type
                        };

                        context.Remaniements.Add(parentRemaniement);

                        remaniementsCreated.Add(
                            $"✅ Ligne parente : {parentLine.Nommenclature.Intitule} ({remaniement.Montant:N0} GNF)");
                    }

                    await context.SaveChangesAsync();

                    // 3️⃣ Remaniements automatiques sur 662 et 110 pour Recette Fonctionnement
                    if (budgetLine.Nommenclature.Section == SectionType.Fonctionnement &&
                        budgetLine.Nommenclature.Nature == NatureType.Recette)
                    {
                        // Calculer 60% du montant remanié
                        var montant60Pourcent = remaniement.Montant * 0.6m;

                        // Récupérer les nomenclatures 662 et 110
                        var m662 = await context.Nommenclatures
                            .FirstOrDefaultAsync(n => n.Article == "662");

                        var m110 = await context.Nommenclatures
                            .FirstOrDefaultAsync(n => n.Article == "110");

                        // Créer remaniement sur 662 si la nomenclature existe
                        if (m662 != null)
                        {
                            var budgetLine662 = await context.BudgetLines
                                .FirstOrDefaultAsync(bl => bl.BudgetPrimitifId == budgetLine.BudgetPrimitifId &&
                                                           bl.NommenclatureId == m662.Id);

                            if (budgetLine662 == null)
                            {
                                // La ligne 662 n'a jamais été budgétée pour ce budget - on la
                                // fabrique (même logique que pour une ligne ex nihilo) afin que
                                // le transfert 60% s'applique aussi dans ce cas.
                                await EnsureBudgetLineAncestorChainAsync(context, m662.Id, budgetLine.BudgetPrimitifId);

                                budgetLine662 = new BudgetLine
                                {
                                    BudgetPrimitifId = budgetLine.BudgetPrimitifId,
                                    NommenclatureId = m662.Id,
                                    MontantPrevu = 0,
                                    MontantActu = 0,
                                    EstAjouteParRemaniement = true
                                };
                                context.BudgetLines.Add(budgetLine662);
                                await context.SaveChangesAsync();
                            }

                            // Remaniement sur la ligne 662
                            var remaniement662 = new Remaniement
                            {
                                IdBudgetLine = budgetLine662.Id,
                                Date = remaniement.Date,
                                Montant = montant60Pourcent,
                                Motif = $"[Auto - 60%] {remaniement.Motif.Trim()}",
                                TypeRemaniement = type
                            };

                            context.Remaniements.Add(remaniement662);
                            remaniementsCreated.Add(
                                $"✅ Ligne 662 (auto) : {m662.Intitule} ({montant60Pourcent:N0} GNF)");

                            // Récupérer les parents de 662 et créer des remaniements
                            var parents662 = await GetParentBudgetLinesAsync(
                                context,
                                m662.Id,
                                budgetLine.BudgetPrimitifId);

                            foreach (var parent662 in parents662)
                            {
                                var parentRemaniement662 = new Remaniement
                                {
                                    IdBudgetLine = parent662.Id,
                                    Date = remaniement.Date,
                                    Montant = montant60Pourcent,
                                    Motif = $"[Auto - 60% - Propagation] {remaniement.Motif.Trim()}",
                                    TypeRemaniement = type
                                };

                                context.Remaniements.Add(parentRemaniement662);
                                remaniementsCreated.Add(
                                    $"✅ Ligne parente 662 (auto) : {parent662.Nommenclature.Intitule} ({montant60Pourcent:N0} GNF)");
                            }
                        }

                        // Créer remaniement sur 110 si la nomenclature existe
                        if (m110 != null)
                        {
                            var budgetLine110 = await context.BudgetLines
                                .FirstOrDefaultAsync(bl => bl.BudgetPrimitifId == budgetLine.BudgetPrimitifId &&
                                                           bl.NommenclatureId == m110.Id);

                            if (budgetLine110 == null)
                            {
                                // La ligne 110 n'a jamais été budgétée pour ce budget - on la
                                // fabrique (même logique que pour une ligne ex nihilo) afin que
                                // le transfert 60% s'applique aussi dans ce cas.
                                await EnsureBudgetLineAncestorChainAsync(context, m110.Id, budgetLine.BudgetPrimitifId);

                                budgetLine110 = new BudgetLine
                                {
                                    BudgetPrimitifId = budgetLine.BudgetPrimitifId,
                                    NommenclatureId = m110.Id,
                                    MontantPrevu = 0,
                                    MontantActu = 0,
                                    EstAjouteParRemaniement = true
                                };
                                context.BudgetLines.Add(budgetLine110);
                                await context.SaveChangesAsync();
                            }

                            // Remaniement sur la ligne 110
                            var remaniement110 = new Remaniement
                            {
                                IdBudgetLine = budgetLine110.Id,
                                Date = remaniement.Date,
                                Montant = montant60Pourcent,
                                Motif = $"[Auto - 60%] {remaniement.Motif.Trim()}",
                                TypeRemaniement = type
                            };

                            context.Remaniements.Add(remaniement110);
                            remaniementsCreated.Add(
                                $"✅ Ligne 110 (auto) : {m110.Intitule} ({montant60Pourcent:N0} GNF)");

                            // Récupérer les parents de 110 et créer des remaniements
                            var parents110 = await GetParentBudgetLinesAsync(
                                context,
                                m110.Id,
                                budgetLine.BudgetPrimitifId);

                            foreach (var parent110 in parents110)
                            {
                                var parentRemaniement110 = new Remaniement
                                {
                                    IdBudgetLine = parent110.Id,
                                    Date = remaniement.Date,
                                    Montant = montant60Pourcent,
                                    Motif = $"[Auto - 60% - Propagation] {remaniement.Motif.Trim()}",
                                    TypeRemaniement = type
                                };

                                context.Remaniements.Add(parentRemaniement110);
                                remaniementsCreated.Add(
                                    $"✅ Ligne parente 110 (auto) : {parent110.Nommenclature.Intitule} ({montant60Pourcent:N0} GNF)");
                            }
                        }

                        await context.SaveChangesAsync();
                    }

                    // 4️⃣ Mise à jour MontantActu enfant
                    budgetLine.UpdateMontantActu();

                    // 5️⃣ Mise à jour MontantActu parents
                    foreach (var parentLine in parentBudgetLines)
                    {
                        var parentToUpdate = await context.BudgetLines
                            .Include(bl => bl.Remaniements)
                            .FirstOrDefaultAsync(bl => bl.Id == parentLine.Id);

                        parentToUpdate?.UpdateMontantActu();
                    }

                    // 6️⃣ Mise à jour MontantActu des lignes 662 et 110 et leurs parents (si remaniements automatiques créés)
                    if (budgetLine.Nommenclature.Section == SectionType.Fonctionnement &&
                        budgetLine.Nommenclature.Nature == NatureType.Recette)
                    {
                        var m662 = await context.Nommenclatures
                            .FirstOrDefaultAsync(n => n.Article == "662");

                        var m110 = await context.Nommenclatures
                            .FirstOrDefaultAsync(n => n.Article == "110");

                        if (m662 != null)
                        {
                            var budgetLine662 = await context.BudgetLines
                                .Include(bl => bl.Remaniements)
                                .FirstOrDefaultAsync(bl => bl.BudgetPrimitifId == budgetLine.BudgetPrimitifId &&
                                                           bl.NommenclatureId == m662.Id);

                            if (budgetLine662 != null)
                            {
                                budgetLine662.UpdateMontantActu();

                                // Mettre à jour les parents de 662
                                var parents662 = await GetParentBudgetLinesAsync(
                                    context,
                                    m662.Id,
                                    budgetLine.BudgetPrimitifId);

                                foreach (var parent662 in parents662)
                                {
                                    var parentToUpdate662 = await context.BudgetLines
                                        .Include(bl => bl.Remaniements)
                                        .FirstOrDefaultAsync(bl => bl.Id == parent662.Id);

                                    parentToUpdate662?.UpdateMontantActu();
                                }
                            }
                        }

                        if (m110 != null)
                        {
                            var budgetLine110 = await context.BudgetLines
                                .Include(bl => bl.Remaniements)
                                .FirstOrDefaultAsync(bl => bl.BudgetPrimitifId == budgetLine.BudgetPrimitifId &&
                                                           bl.NommenclatureId == m110.Id);

                            if (budgetLine110 != null)
                            {
                                budgetLine110.UpdateMontantActu();

                                // Mettre à jour les parents de 110
                                var parents110 = await GetParentBudgetLinesAsync(
                                    context,
                                    m110.Id,
                                    budgetLine.BudgetPrimitifId);

                                foreach (var parent110 in parents110)
                                {
                                    var parentToUpdate110 = await context.BudgetLines
                                        .Include(bl => bl.Remaniements)
                                        .FirstOrDefaultAsync(bl => bl.Id == parent110.Id);

                                    parentToUpdate110?.UpdateMontantActu();
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync();

                    // 7️⃣ Mise à jour du MontantTotal, MontantRecette ou MontantDepense du BudgetPrimitif
                    var budgetPrimitif = await context.BudgetsPrimitifs
                        .FirstOrDefaultAsync(bp => bp.Id == budgetLine.BudgetPrimitifId);

                    if (budgetPrimitif != null)
                    {
                        // Déterminer si c'est une recette ou une dépense
                        var isRecette = budgetLine.Nommenclature.Nature == NatureType.Recette;
                        var isDepense = budgetLine.Nommenclature.Nature == NatureType.Depense;

                        if (type == TypeRemaniement.en_plus)
                        {
                            
                            if (isRecette)
                            {
                                //budgetPrimitif.MontantTotal += remaniement.Montant;
                                budgetPrimitif.MontantRecette += remaniement.Montant;
                            }
                            else if (isDepense)
                            {
                                budgetPrimitif.MontantDepense += remaniement.Montant;
                            }
                        }
                        else if (type == TypeRemaniement.en_moins)
                        {
                            
                            if (isRecette)
                            {
                                //budgetPrimitif.MontantTotal -= remaniement.Montant;
                                budgetPrimitif.MontantRecette -= remaniement.Montant;
                            }
                            else if (isDepense)
                            {
                                budgetPrimitif.MontantDepense -= remaniement.Montant;
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 🔹 Recharger sans cycle
                    var savedRemaniement =
                        await GetRemaniementByIdAsync(newRemaniement.Id);

                    var typeText =
                        type == TypeRemaniement.en_plus ? "augmentation" : "diminution";

                    var message =
                        $"✅ Remaniement créé avec succès ({typeText} de {remaniement.Montant:N0} GNF).\n\n" +
                        $"📊 Remaniements créés :\n" +
                        string.Join("\n", remaniementsCreated);

                    result = (true, message, savedRemaniement);
                    return result;
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    return (false, $"Erreur base de données : {inner}", null);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Erreur : {ex.Message}", null);
                }
            });
        }

        #endregion

        #region Suppression

        public async Task<(bool Success, string Message)> DeleteRemaniementAsync(int id)
        {
            if (!SessionManager.HasPermission("Remaniement.Delete"))
                return (false, "Permission Remaniement.Delete requise pour supprimer un remaniement.");

            using var context = CreateContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var remaniement = await context.Remaniements
                    .Include(r => r.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (remaniement == null)
                    return (false, "Remaniement introuvable.");

                var budgetLineId = remaniement.IdBudgetLine;
                var budgetLine = remaniement.BudgetLine;

                // 🆕 Récupérer les lignes parentes
                var parentBudgetLines = await GetParentBudgetLinesAsync(
                    context,
                    budgetLine.NommenclatureId,
                    budgetLine.BudgetPrimitifId);

                // Supprimer le remaniement principal
                context.Remaniements.Remove(remaniement);

                // 🆕 Supprimer les remaniements correspondants sur les parents
                // (même date, même montant, même type, motif avec [Propagation])
                foreach (var parentLine in parentBudgetLines)
                {
                    var parentRemaniement = await context.Remaniements
                        .FirstOrDefaultAsync(r =>
                            r.IdBudgetLine == parentLine.Id &&
                            r.Date == remaniement.Date &&
                            r.Montant == remaniement.Montant &&
                            r.TypeRemaniement == remaniement.TypeRemaniement &&
                            r.Motif.StartsWith("[Propagation]"));

                    if (parentRemaniement != null)
                    {
                        context.Remaniements.Remove(parentRemaniement);
                    }
                }

                await context.SaveChangesAsync();

                // ✅ Mettre à jour MontantActu pour la ligne enfant
                var updatedBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => bl.Id == budgetLineId);

                if (updatedBudgetLine != null)
                {
                    updatedBudgetLine.UpdateMontantActu();
                }

                // 🆕 Mettre à jour MontantActu pour toutes les lignes parentes
                foreach (var parentLine in parentBudgetLines)
                {
                    var parentToUpdate = await context.BudgetLines
                        .Include(bl => bl.Remaniements)
                        .FirstOrDefaultAsync(bl => bl.Id == parentLine.Id);

                    if (parentToUpdate != null)
                    {
                        parentToUpdate.UpdateMontantActu();
                    }
                }

                await context.SaveChangesAsync();

                // 🔹 Mise à jour du MontantTotal, MontantRecette ou MontantDepense du BudgetPrimitif (inverser l'effet)
                var budgetPrimitif = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(bp => bp.Id == budgetLine.BudgetPrimitifId);

                if (budgetPrimitif != null)
                {
                    // Déterminer si c'est une recette ou une dépense
                    var isRecette = budgetLine.Nommenclature.Nature == NatureType.Recette;
                    var isDepense = budgetLine.Nommenclature.Nature == NatureType.Depense;

                    // Inverser l'effet : si c'était en_plus, on soustrait maintenant
                    // Si c'était en_moins, on ajoute maintenant
                    if (remaniement.TypeRemaniement == TypeRemaniement.en_plus)
                    {
                        //budgetPrimitif.MontantTotal -= remaniement.Montant;
                        
                        if (isRecette)
                        {
                            budgetPrimitif.MontantRecette -= remaniement.Montant;
                        }
                        else if (isDepense)
                        {
                            budgetPrimitif.MontantDepense -= remaniement.Montant;
                        }
                    }
                    else if (remaniement.TypeRemaniement == TypeRemaniement.en_moins)
                    {
                        //budgetPrimitif.MontantTotal += remaniement.Montant;
                        
                        if (isRecette)
                        {
                            budgetPrimitif.MontantRecette += remaniement.Montant;
                        }
                        else if (isDepense)
                        {
                            budgetPrimitif.MontantDepense += remaniement.Montant;
                        }
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "✅ Remaniement supprimé avec succès (y compris les propagations aux parents).");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        public async Task<RemaniementStatistiques> GetStatistiquesAsync()
        {
            using var context = CreateContext();

            var remaniements = await context.Remaniements
                .AsNoTracking()
                .ToListAsync();

            var totalEnPlus = remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                .Sum(r => r.Montant);

            var totalEnMoins = remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                .Sum(r => r.Montant);

            var countEnPlus = remaniements.Count(r => r.TypeRemaniement == TypeRemaniement.en_plus);
            var countEnMoins = remaniements.Count(r => r.TypeRemaniement == TypeRemaniement.en_moins);

            return new RemaniementStatistiques
            {
                TotalRemaniements = remaniements.Count,
                TotalEnPlus = totalEnPlus,
                TotalEnMoins = totalEnMoins,
                CountEnPlus = countEnPlus,
                CountEnMoins = countEnMoins,
                SoldeNet = totalEnPlus - totalEnMoins
            };
        }

        #endregion
    }

    public class RemaniementStatistiques
    {
        public int TotalRemaniements { get; set; }
        public decimal TotalEnPlus { get; set; }
        public decimal TotalEnMoins { get; set; }
        public int CountEnPlus { get; set; }
        public int CountEnMoins { get; set; }
        public decimal SoldeNet { get; set; }
    }
}