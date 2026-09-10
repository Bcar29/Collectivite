-- ============================================================================
-- Nettoyage de la base de données en conservant les données de seed.
--
-- Ce script vide toutes les tables "métier" (données de test/production
-- saisies via l'application) mais préserve :
--   - Roles, Permissions, RolePermissions      (SeedRolesPermissions.cs)
--   - Nommenclatures                            (SeedNommenclature.cs)
--   - CompteComptables                          (SeedPlanComptable.cs)
--   - Users                                     (uniquement le compte "admin")
--
-- Usage :
--   mysql -u <user> -p <nom_de_la_base> < reset_db_keep_seed.sql
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;

-- Écritures comptables et mouvements
TRUNCATE TABLE `mouvement`;
TRUNCATE TABLE `EcritureComptables`;

-- Pièces comptables (dans un ordre indifférent grâce à FK checks désactivés)
TRUNCATE TABLE `DetailsFactures`;
TRUNCATE TABLE `Factures`;
TRUNCATE TABLE `DetailsBonCommandes`;
TRUNCATE TABLE `BonCommandes`;
TRUNCATE TABLE `DetailExpressionBesoins`;
TRUNCATE TABLE `ExpressionBesoins`;
TRUNCATE TABLE `Mandats`;
TRUNCATE TABLE `OrdreRecettes`;
TRUNCATE TABLE `Engagements`;
TRUNCATE TABLE `Remaniements`;

-- Budget
TRUNCATE TABLE `BudgetLines`;
TRUNCATE TABLE `BudgetsPrimitifs`;
TRUNCATE TABLE `Exercices`;

-- Tiers / comptes bancaires / documents
TRUNCATE TABLE `CompteBancaires`;
TRUNCATE TABLE `DocumentTiers`;
TRUNCATE TABLE `Tiers`;

-- Communes et détails
TRUNCATE TABLE `DetailCommunes`;
TRUNCATE TABLE `Communes`;

-- Journal d'audit
TRUNCATE TABLE `AuditLogs`;

-- Utilisateurs : on garde uniquement le super-admin seedé
DELETE FROM `Users` WHERE `Username` <> 'admin';

SET FOREIGN_KEY_CHECKS = 1;

-- Tables volontairement NON touchées (données de seed) :
--   Roles, Permissions, RolePermissions, Nommenclatures, CompteComptables
