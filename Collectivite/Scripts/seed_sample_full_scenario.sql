-- ============================================================================
-- Jeu de données d'exemple complet (bout en bout) pour tester l'application.
--
-- Construit un scénario réaliste :
--   Commune (Ratoma, Guinée) + Exercice 2026
--   -> BudgetPrimitif + BudgetLine (sur une nomenclature de dépense déjà seedée)
--   -> Tiers "fournisseur" + son compte bancaire
--   -> Facture (+ 1 ligne de détail)
--   -> Engagement (lié au budget, à la facture, au tiers, à la commune)
--   -> Mandat (lié à l'engagement)
--
-- Idempotent : peut être exécuté plusieurs fois sans dupliquer les données
-- (réutilise les lignes existantes identifiées par leurs clés naturelles).
--
-- Pré-requis : les tables de seed (Nommenclatures notamment) doivent déjà
-- être peuplées (SeedNommenclature.cs, exécuté automatiquement par l'appli).
--
-- Usage :
--   mysql -u <user> -p <nom_de_la_base> < seed_sample_full_scenario.sql
-- ============================================================================

-- ─────────────────────────────────────────────────────────────────
-- 1. Commune : Ratoma (Conakry, Guinée)
-- ─────────────────────────────────────────────────────────────────
SET @commune_id = (SELECT Id FROM `Communes` WHERE `Nom` = 'Ratoma' LIMIT 1);

INSERT INTO `Communes`
    (`Nom`, `Region`, `Prefecture`, `CommuneType`, `DistanceChefLieuProvince`, `DistanceChefLieuRegion`, `DistanceCapitale`, `DateCreation`)
SELECT 'Ratoma', 'Conakry', '', 0, 0, 0, 135, '1960-01-01'
WHERE @commune_id IS NULL;

SET @commune_id = COALESCE(@commune_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 2. Exercice 2026
-- ─────────────────────────────────────────────────────────────────
SET @exercice_id = (SELECT Id FROM `Exercices` WHERE `Libelle` = 'Exercice 2026' LIMIT 1);

INSERT INTO `Exercices`
    (`Libelle`, `DateDebut`, `DateFin`, `EstCloture`)
SELECT 'Exercice 2026', '2026-01-01', '2026-12-31', 0
WHERE @exercice_id IS NULL;

SET @exercice_id = COALESCE(@exercice_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 3. Nomenclature de dépense existante (seedée) : 6045 - Fournitures de bureau
--    (Nature = 1 = Depense, ligne "feuille" sans enfant)
-- ─────────────────────────────────────────────────────────────────
SET @nomenclature_id = (
    SELECT Id FROM `Nommenclatures`
    WHERE `Paragraphe` = '6045' AND `SousParagraphe` IS NULL AND `Nature` = 1
    LIMIT 1
);

-- ─────────────────────────────────────────────────────────────────
-- 4. Budget primitif de l'exercice
-- ─────────────────────────────────────────────────────────────────
SET @budget_primitif_id = (SELECT Id FROM `BudgetsPrimitifs` WHERE `ExerciceId` = @exercice_id LIMIT 1);

INSERT INTO `BudgetsPrimitifs`
    (`ExerciceId`, `MontantDepense`, `MontantRecette`, `DateApprobation`, `DateValidation`, `Status`)
SELECT @exercice_id, 5000000, 8000000, NULL, NULL, 0
WHERE @budget_primitif_id IS NULL;

SET @budget_primitif_id = COALESCE(@budget_primitif_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 5. Ligne budgétaire : 5 000 000 GNF sur "Fournitures de bureau"
-- ─────────────────────────────────────────────────────────────────
SET @budget_line_id = (
    SELECT Id FROM `BudgetLines`
    WHERE `BudgetPrimitifId` = @budget_primitif_id AND `NommenclatureId` = @nomenclature_id
    LIMIT 1
);

INSERT INTO `BudgetLines`
    (`BudgetPrimitifId`, `NommenclatureId`, `MontantPrevu`, `MontantActu`, `MontantRealise`, `MontantEntreSortie`, `EstAjouteParRemaniement`)
SELECT @budget_primitif_id, @nomenclature_id, 5000000, 5000000, 0, 0, 0
WHERE @budget_line_id IS NULL;

SET @budget_line_id = COALESCE(@budget_line_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 6. Tiers : fournisseur (personne morale)
-- ─────────────────────────────────────────────────────────────────
SET @tiers_id = (SELECT Id FROM `Tiers` WHERE `Email` = 'contact@fournisseur-demo.gn' LIMIT 1);

INSERT INTO `Tiers`
    (`Type`, `Categorie`, `Email`, `Telephone`, `Adresse`, `IsActif`, `DateCreation`,
     `Nom`, `Prenom`, `NumeroPieceIdentite`, `TypePieceIdentite`,
     `RaisonSociale`, `Rccm`, `Nif`, `NumeroTva`, `SecteurActivite`)
SELECT 1, 1, 'contact@fournisseur-demo.gn', '+224 620 00 00 00', 'Ratoma, Conakry, Guinée', 1, NOW(),
       NULL, NULL, NULL, NULL,
       'Établissements Demo SARL', 'GC-KIN-2020-B-00123', '123456789', NULL, 'Fournitures de bureau'
WHERE @tiers_id IS NULL;

SET @tiers_id = COALESCE(@tiers_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 7. Compte bancaire du tiers
-- ─────────────────────────────────────────────────────────────────
SET @compte_bancaire_id = (SELECT Id FROM `CompteBancaires` WHERE `IBAN` = 'GN00000000000000000001' LIMIT 1);

INSERT INTO `CompteBancaires`
    (`TiersId`, `IBAN`, `BIC`, `Banque`, `Pays`)
SELECT @tiers_id, 'GN00000000000000000001', 'BICNGNCXXXX', 'Banque Centrale de la République de Guinée', 'Guinée'
WHERE @compte_bancaire_id IS NULL;

-- ─────────────────────────────────────────────────────────────────
-- 8. Facture émise par le tiers
-- ─────────────────────────────────────────────────────────────────
SET @facture_id = (SELECT Id FROM `Factures` WHERE `NumeroFacture` = 'FAC-2026-0001' LIMIT 1);

INSERT INTO `Factures`
    (`NumeroFacture`, `DateFacture`, `MontantHT`, `TauxTVA`, `MontantTTC`, `DateEcheance`, `Description`, `TiersId`, `ExerciceId`, `Status`)
SELECT 'FAC-2026-0001', '2026-02-05', 1500000, 18, 1770000, '2026-03-05', 'Fournitures de bureau pour la mairie de Ratoma', @tiers_id, @exercice_id, 0
WHERE @facture_id IS NULL;

SET @facture_id = COALESCE(@facture_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 9. Détail de la facture
-- ─────────────────────────────────────────────────────────────────
INSERT INTO `DetailsFactures`
    (`FactureId`, `Libelle`, `Quantite`, `PrixUnitaire`, `MontantTotal`)
SELECT @facture_id, 'Fournitures de bureau diverses', 100, 15000, 1500000
WHERE NOT EXISTS (SELECT 1 FROM `DetailsFactures` WHERE `FactureId` = @facture_id);

-- ─────────────────────────────────────────────────────────────────
-- 10. Engagement budgétaire lié à la facture
-- ─────────────────────────────────────────────────────────────────
SET @engagement_id = (
    SELECT Id FROM `Engagements`
    WHERE `FactureId` = @facture_id
    LIMIT 1
);

INSERT INTO `Engagements`
    (`ExerciceId`, `CommuneId`, `BudgetLineId`, `TiersId`, `Objet`, `DateEngagement`,
     `CreditsBudgetaires`, `EngagementsAnterieurs`, `MontantEngagement`, `MontantLettre`,
     `FactureId`, `Etat`, `BonCommandeId`)
SELECT @exercice_id, @commune_id, @budget_line_id, @tiers_id, 'Achat de fournitures de bureau pour la mairie de Ratoma', '2026-02-01',
       5000000, 0, 1500000, 'Un million cinq cent mille francs guinéens',
       @facture_id, 0, NULL
WHERE @engagement_id IS NULL;

SET @engagement_id = COALESCE(@engagement_id, LAST_INSERT_ID());

-- ─────────────────────────────────────────────────────────────────
-- 11. Mandat de paiement de l'engagement
-- ─────────────────────────────────────────────────────────────────
INSERT INTO `Mandats`
    (`NumeroMandat`, `Bordereau`, `Mois`, `EngagementId`, `MontantBrut`, `Rts`, `AutresPrecomptes`,
     `MontantNet`, `MontantLettre`, `DateEmission`, `Objet`, `DatePaiement`, `Status`, `Etat`)
SELECT 'M-2026-0001', NULL, 1, @engagement_id, 1500000, 0, 0,
       1500000, 'Un million cinq cent mille francs guinéens', '2026-02-10', 'Paiement fournitures de bureau', NULL, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM `Mandats` WHERE `EngagementId` = @engagement_id);
