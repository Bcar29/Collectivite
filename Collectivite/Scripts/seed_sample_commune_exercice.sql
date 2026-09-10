-- ============================================================================
-- Peuplement de données d'exemple : une commune (Guinée) + un exercice.
--
-- À exécuter après reset_db_keep_seed.sql si tu veux repartir avec un jeu de
-- données minimal pour tester l'application (au lieu d'une base totalement
-- vide côté Communes/Exercices).
--
-- Usage :
--   mysql -u <user> -p <nom_de_la_base> < seed_sample_commune_exercice.sql
-- ============================================================================

-- Commune urbaine de Ratoma (préfecture de Kindia, région de Kindia, Guinée)
INSERT INTO `Communes`
    (`Nom`, `Region`, `Prefecture`, `CommuneType`, `DistanceChefLieuProvince`, `DistanceChefLieuRegion`, `DistanceCapitale`, `DateCreation`)
VALUES
    ('Ratoma', 'Conakry', '', 0, 0, 0, 135, '1960-01-01');
-- CommuneType : 0 = URBAINE, 1 = RURALE

-- Exercice budgétaire 2026
INSERT INTO `Exercices`
    (`Libelle`, `DateDebut`, `DateFin`, `EstCloture`)
VALUES
    ('Exercice 2026', '2026-01-01', '2026-12-31', 0);
