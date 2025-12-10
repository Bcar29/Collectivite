using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Utils
{
    public static class SeedRolesPermissions
    {
        /// <summary>
        /// Permissions de base (existantes) + permissions CRUD dérivées des modèles.
        /// 
        /// ⚠️ Ces permissions servent uniquement de "catalogue" initial.
        /// Le Maire reste libre d'en utiliser certaines ou non via l'interface.
        /// </summary>
        private static readonly Permission[] DefaultPermissions =
        {
            // ───────────── Permissions existantes ─────────────
            new Permission { Name = "Approuver le budget", Code = "Budget.Approve", Description = "Permet d'approuver le budget primitif." },
            new Permission { Name = "Valider le budget", Code = "Budget.Validate", Description = "Permet de valider le budget primitif." },
            // new Permission { Name = "Gérer les remaniements", Code = "Remaniement.Manage", Description = "Permet d'ajouter/supprimer des remaniements." },
            // new Permission { Name = "Gérer les finances", Code = "Finance.Manage", Description = "Accès aux écrans financiers (mandats, recettes, etc.)." },
            // new Permission { Name = "Enregistrer les courriers", Code = "Courrier.Register", Description = "Permet l'enregistrement des courriers entrants/sortants." },

            // ───────────── Permissions CRUD par modèle ─────────────
            // AuditLog
            new Permission { Name = "Voir les journaux d'audit", Code = "AuditLog.View", Description = "Permet de consulter les journaux d'audit." },
            new Permission { Name = "Supprimer les journaux d'audit", Code = "AuditLog.Delete", Description = "Permet de supprimer des journaux d'audit." },

            // BonCommande
            new Permission { Name = "Créer un bon de commande", Code = "BonCommande.Create", Description = "Permet de créer un bon de commande." },
            new Permission { Name = "Modifier un bon de commande", Code = "BonCommande.Edit", Description = "Permet de modifier un bon de commande." },
            new Permission { Name = "Voir les bons de commande", Code = "BonCommande.View", Description = "Permet de consulter les bons de commande." },
            new Permission { Name = "Supprimer un bon de commande", Code = "BonCommande.Delete", Description = "Permet de supprimer un bon de commande." },

            // BudgetLine
            new Permission { Name = "Créer une ligne budgétaire", Code = "BudgetLine.Create", Description = "Permet de créer une ligne budgétaire." },
            new Permission { Name = "Modifier une ligne budgétaire", Code = "BudgetLine.Edit", Description = "Permet de modifier une ligne budgétaire." },
            new Permission { Name = "Voir les lignes budgétaires", Code = "BudgetLine.View", Description = "Permet de consulter les lignes budgétaires." },
            new Permission { Name = "Supprimer une ligne budgétaire", Code = "BudgetLine.Delete", Description = "Permet de supprimer une ligne budgétaire." },

            // BudgetPrimitif (en plus de Approve / Validate)
            new Permission { Name = "Créer un budget primitif", Code = "BudgetPrimitif.Create", Description = "Permet de créer un budget primitif." },
            new Permission { Name = "Modifier un budget primitif", Code = "BudgetPrimitif.Edit", Description = "Permet de modifier un budget primitif tant qu'il n'est pas validé." },
            new Permission { Name = "Voir les budgets primitifs", Code = "BudgetPrimitif.View", Description = "Permet de consulter les budgets primitifs." },
            new Permission { Name = "Supprimer un budget primitif", Code = "BudgetPrimitif.Delete", Description = "Permet de supprimer un budget primitif." },

            // Commune
            new Permission { Name = "Créer une commune", Code = "Commune.Create", Description = "Permet de créer une commune." },
            new Permission { Name = "Modifier une commune", Code = "Commune.Edit", Description = "Permet de modifier une commune." },
            new Permission { Name = "Voir les communes", Code = "Commune.View", Description = "Permet de consulter les communes." },
            new Permission { Name = "Supprimer une commune", Code = "Commune.Delete", Description = "Permet de supprimer une commune." },

            // CompteBancaire
            new Permission { Name = "Créer un compte bancaire", Code = "CompteBancaire.Create", Description = "Permet de créer un compte bancaire." },
            new Permission { Name = "Modifier un compte bancaire", Code = "CompteBancaire.Edit", Description = "Permet de modifier un compte bancaire." },
            new Permission { Name = "Voir les comptes bancaires", Code = "CompteBancaire.View", Description = "Permet de consulter les comptes bancaires." },
            new Permission { Name = "Supprimer un compte bancaire", Code = "CompteBancaire.Delete", Description = "Permet de supprimer un compte bancaire." },

            // CompteComptable
            new Permission { Name = "Créer un compte comptable", Code = "CompteComptable.Create", Description = "Permet de créer un compte comptable." },
            new Permission { Name = "Modifier un compte comptable", Code = "CompteComptable.Edit", Description = "Permet de modifier un compte comptable." },
            new Permission { Name = "Voir les comptes comptables", Code = "CompteComptable.View", Description = "Permet de consulter les comptes comptables." },
            new Permission { Name = "Supprimer un compte comptable", Code = "CompteComptable.Delete", Description = "Permet de supprimer un compte comptable." },

            // Contrats
            new Permission { Name = "Créer un contrat", Code = "Contrats.Create", Description = "Permet de créer un contrat." },
            new Permission { Name = "Modifier un contrat", Code = "Contrats.Edit", Description = "Permet de modifier un contrat." },
            new Permission { Name = "Voir les contrats", Code = "Contrats.View", Description = "Permet de consulter les contrats." },
            new Permission { Name = "Supprimer un contrat", Code = "Contrats.Delete", Description = "Permet de supprimer un contrat." },

            // DetailBonCommande
            new Permission { Name = "Créer un détail de bon de commande", Code = "DetailBonCommande.Create", Description = "Permet de créer un détail de bon de commande." },
            new Permission { Name = "Modifier un détail de bon de commande", Code = "DetailBonCommande.Edit", Description = "Permet de modifier un détail de bon de commande." },
            new Permission { Name = "Voir les détails de bons de commande", Code = "DetailBonCommande.View", Description = "Permet de consulter les détails de bons de commande." },
            new Permission { Name = "Supprimer un détail de bon de commande", Code = "DetailBonCommande.Delete", Description = "Permet de supprimer un détail de bon de commande." },

            // DetailCommune
            new Permission { Name = "Créer un détail de commune", Code = "DetailCommune.Create", Description = "Permet de créer un détail de commune." },
            new Permission { Name = "Modifier un détail de commune", Code = "DetailCommune.Edit", Description = "Permet de modifier un détail de commune." },
            new Permission { Name = "Voir les détails de commune", Code = "DetailCommune.View", Description = "Permet de consulter les détails de commune." },
            new Permission { Name = "Supprimer un détail de commune", Code = "DetailCommune.Delete", Description = "Permet de supprimer un détail de commune." },

            // DetailsFacture
            new Permission { Name = "Créer un détail de facture", Code = "DetailsFacture.Create", Description = "Permet de créer un détail de facture." },
            new Permission { Name = "Modifier un détail de facture", Code = "DetailsFacture.Edit", Description = "Permet de modifier un détail de facture." },
            new Permission { Name = "Voir les détails de facture", Code = "DetailsFacture.View", Description = "Permet de consulter les détails de facture." },
            new Permission { Name = "Supprimer un détail de facture", Code = "DetailsFacture.Delete", Description = "Permet de supprimer un détail de facture." },

            // DocumentTiers
            new Permission { Name = "Créer un document de tiers", Code = "DocumentTiers.Create", Description = "Permet de créer un document de tiers." },
            new Permission { Name = "Modifier un document de tiers", Code = "DocumentTiers.Edit", Description = "Permet de modifier un document de tiers." },
            new Permission { Name = "Voir les documents de tiers", Code = "DocumentTiers.View", Description = "Permet de consulter les documents de tiers." },
            new Permission { Name = "Supprimer un document de tiers", Code = "DocumentTiers.Delete", Description = "Permet de supprimer un document de tiers." },

            // EcritureComptable
            new Permission { Name = "Créer une écriture comptable", Code = "EcritureComptable.Create", Description = "Permet de créer une écriture comptable." },
            new Permission { Name = "Modifier une écriture comptable", Code = "EcritureComptable.Edit", Description = "Permet de modifier une écriture comptable." },
            new Permission { Name = "Voir les écritures comptables", Code = "EcritureComptable.View", Description = "Permet de consulter les écritures comptables." },
            new Permission { Name = "Supprimer une écriture comptable", Code = "EcritureComptable.Delete", Description = "Permet de supprimer une écriture comptable." },

            // Engagement
            new Permission { Name = "Créer un engagement", Code = "Engagement.Create", Description = "Permet de créer un engagement." },
            new Permission { Name = "Modifier un engagement", Code = "Engagement.Edit", Description = "Permet de modifier un engagement." },
            new Permission { Name = "Voir les engagements", Code = "Engagement.View", Description = "Permet de consulter les engagements." },
            new Permission { Name = "Supprimer un engagement", Code = "Engagement.Delete", Description = "Permet de supprimer un engagement." },

            // Exercice
            new Permission { Name = "Créer un exercice", Code = "Exercice.Create", Description = "Permet de créer un exercice budgétaire." },
            new Permission { Name = "Modifier un exercice", Code = "Exercice.Edit", Description = "Permet de modifier un exercice budgétaire." },
            new Permission { Name = "Voir les exercices", Code = "Exercice.View", Description = "Permet de consulter les exercices budgétaires." },
            new Permission { Name = "Supprimer un exercice", Code = "Exercice.Delete", Description = "Permet de supprimer un exercice budgétaire." },

            // Facture
            new Permission { Name = "Créer une facture", Code = "Facture.Create", Description = "Permet de créer une facture." },
            new Permission { Name = "Modifier une facture", Code = "Facture.Edit", Description = "Permet de modifier une facture." },
            new Permission { Name = "Voir les factures", Code = "Facture.View", Description = "Permet de consulter les factures." },
            new Permission { Name = "Supprimer une facture", Code = "Facture.Delete", Description = "Permet de supprimer une facture." },

            // Mandat
            new Permission { Name = "Créer un mandat", Code = "Mandat.Create", Description = "Permet de créer un mandat." },
            new Permission { Name = "Modifier un mandat", Code = "Mandat.Edit", Description = "Permet de modifier un mandat." },
            new Permission { Name = "Voir les mandats", Code = "Mandat.View", Description = "Permet de consulter les mandats." },
            new Permission { Name = "Supprimer un mandat", Code = "Mandat.Delete", Description = "Permet de supprimer un mandat." },

            // Nommenclature
            new Permission { Name = "Créer une nomenclature", Code = "Nommenclature.Create", Description = "Permet de créer une nomenclature budgétaire." },
            new Permission { Name = "Modifier une nomenclature", Code = "Nommenclature.Edit", Description = "Permet de modifier une nomenclature budgétaire." },
            new Permission { Name = "Voir les nomenclatures", Code = "Nommenclature.View", Description = "Permet de consulter les nomenclatures budgétaires." },
            new Permission { Name = "Supprimer une nomenclature", Code = "Nommenclature.Delete", Description = "Permet de supprimer une nomenclature budgétaire." },

            // OrdreRecette
            new Permission { Name = "Créer un ordre de recette", Code = "OrdreRecette.Create", Description = "Permet de créer un ordre de recette." },
            new Permission { Name = "Modifier un ordre de recette", Code = "OrdreRecette.Edit", Description = "Permet de modifier un ordre de recette." },
            new Permission { Name = "Voir les ordres de recette", Code = "OrdreRecette.View", Description = "Permet de consulter les ordres de recette." },
            new Permission { Name = "Supprimer un ordre de recette", Code = "OrdreRecette.Delete", Description = "Permet de supprimer un ordre de recette." },

            // Permission (gestion des permissions elles-mêmes)
            new Permission { Name = "Créer une permission", Code = "Permission.Create", Description = "Permet de créer une permission." },
            new Permission { Name = "Modifier une permission", Code = "Permission.Edit", Description = "Permet de modifier une permission." },
            new Permission { Name = "Voir les permissions", Code = "Permission.View", Description = "Permet de consulter les permissions." },
            new Permission { Name = "Supprimer une permission", Code = "Permission.Delete", Description = "Permet de supprimer une permission." },

            // Recensement
            new Permission { Name = "Créer un recensement", Code = "Recensement.Create", Description = "Permet de créer un recensement." },
            new Permission { Name = "Modifier un recensement", Code = "Recensement.Edit", Description = "Permet de modifier un recensement." },
            new Permission { Name = "Voir les recensements", Code = "Recensement.View", Description = "Permet de consulter les recensements." },
            new Permission { Name = "Supprimer un recensement", Code = "Recensement.Delete", Description = "Permet de supprimer un recensement." },

            // Remaniement
            new Permission { Name = "Créer un remaniement", Code = "Remaniement.Create", Description = "Permet de créer un remaniement budgétaire." },
            new Permission { Name = "Modifier un remaniement", Code = "Remaniement.Edit", Description = "Permet de modifier un remaniement budgétaire." },
            new Permission { Name = "Voir les remaniements", Code = "Remaniement.View", Description = "Permet de consulter les remaniements budgétaires." },
            new Permission { Name = "Supprimer un remaniement", Code = "Remaniement.Delete", Description = "Permet de supprimer un remaniement budgétaire." },
            // ExpressionBesoin
            new Permission { Name = "Créer une Expression de Besoin", Code = "ExpressionBesoin.Create", Description = "Permet de créer une Expression de Besoin." },
            new Permission { Name = "Modifier une Expression de Besoin", Code = "ExpressionBesoin.Edit", Description = "Permet de modifier une Expression de Besoin." },
            new Permission { Name = "Voir les Expression de Besoin", Code = "ExpressionBesoin.View", Description = "Permet de consulter les  Expressions de Besoin." },
            new Permission { Name = "Supprimer une Expression de Besoin", Code = "ExpressionBesoin.Delete", Description = "Permet de supprimer une Expression de Besoin." },

            // Role
            new Permission { Name = "Créer un rôle", Code = "Role.Create", Description = "Permet de créer un rôle applicatif." },
            new Permission { Name = "Modifier un rôle", Code = "Role.Edit", Description = "Permet de modifier un rôle applicatif." },
            new Permission { Name = "Voir les rôles", Code = "Role.View", Description = "Permet de consulter les rôles applicatifs." },
            new Permission { Name = "Supprimer un rôle", Code = "Role.Delete", Description = "Permet de supprimer un rôle applicatif." },

            // RolePermission (gestion fine des liens rôle/permission)
            new Permission { Name = "Gérer les permissions des rôles", Code = "RolePermission.Manage", Description = "Permet de gérer l'association des permissions aux rôles." },

            // Tiers
            new Permission { Name = "Créer un tiers", Code = "Tiers.Create", Description = "Permet de créer un tiers." },
            new Permission { Name = "Modifier un tiers", Code = "Tiers.Edit", Description = "Permet de modifier un tiers." },
            new Permission { Name = "Voir les tiers", Code = "Tiers.View", Description = "Permet de consulter les tiers." },
            new Permission { Name = "Supprimer un tiers", Code = "Tiers.Delete", Description = "Permet de supprimer un tiers." },

            // User
            new Permission { Name = "Créer un utilisateur", Code = "User.Create", Description = "Permet de créer un utilisateur." },
            new Permission { Name = "Modifier un utilisateur", Code = "User.Edit", Description = "Permet de modifier un utilisateur." },
            new Permission { Name = "Voir les utilisateurs", Code = "User.View", Description = "Permet de consulter les utilisateurs." },
            new Permission { Name = "Supprimer un utilisateur", Code = "User.Delete", Description = "Permet de supprimer un utilisateur." },

            // Administration
            new Permission { Name = "Accès à l'administration", Code = "Administration.Access", Description = "Permet d'accéder à la section d'administration (Rôles, Permissions, Utilisateurs)." },

            // Gestion comptable
            new Permission { Name = "Accès à la gestion comptable", Code = "GestionComptable.Access", Description = "Permet d'accéder à la section de gestion comptable (Comptes de gestion, Livre journal, Grand livre, Balance)." }

        };

        private static readonly Dictionary<string, string[]> RolePermissions = new()
        {
            { "Maire", new[] { "Budget.Approve", "Budget.Validate", "Remaniement.Manage" } },
            { "Secrétaire Général", new[] { "Courrier.Register", "Remaniement.Manage" } },
            { "Receveur", new[] { "Finance.Manage", "Remaniement.Manage" } }
        };

        public static void Seed(AppDbContext db)
        {
            //db.Database.Migrate();

            // Permissions
            foreach (var permission in DefaultPermissions)
            {
                if (!db.Permissions.Any(p => p.Code == permission.Code))
                {
                    db.Permissions.Add(new Permission
                    {
                        Name = permission.Name,
                        Code = permission.Code,
                        Description = permission.Description
                    });
                }
            }

            db.SaveChanges();

            // Roles + assignments
            // ⚠️ IMPORTANT : on ne supprime plus les RolePermissions existantes.
            // L'objectif est de fournir un "rôle par défaut" la première fois :
            // - si le rôle n'existe pas, on le crée
            // - si le rôle existe, on ajoute uniquement les permissions manquantes
            //   sans toucher aux permissions déjà configurées via l'interface.
            foreach (var roleEntry in RolePermissions)
            {
                var roleName = roleEntry.Key;
                var role = db.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefault(r => r.Name == roleName);

                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleName,
                        Description = $"Rôle par défaut : {roleName}",
                        IsActive = true
                    };
                    db.Roles.Add(role);
                    db.SaveChanges();

                    role = db.Roles
                        .Include(r => r.RolePermissions)
                        .First(r => r.Name == roleName);
                }

                var permissionCodes = roleEntry.Value ?? [];
                var permissionIds = db.Permissions
                    .Where(p => permissionCodes.Contains(p.Code))
                    .Select(p => p.Id)
                    .ToList();

                var existingPermissionIds = role.RolePermissions
                    .Select(rp => rp.PermissionId)
                    .ToHashSet();

                foreach (var pid in permissionIds)
                {
                    if (!existingPermissionIds.Contains(pid))
                    {
                        db.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = pid
                        });
                    }
                }

                db.SaveChanges();
            }

            // ════════════════════════════════════════════════════════════
            // Maire = super-admin : lui attribuer TOUTES les permissions existantes
            // sans retirer celles déjà présentes (ajout uniquement des manquantes).
            // ════════════════════════════════════════════════════════════
            var maire = db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefault(r => r.Name == "Maire");

            if (maire != null)
            {
                var allPermissionIds = db.Permissions.Select(p => p.Id).ToList();
                var existingIds = maire.RolePermissions
                    .Select(rp => rp.PermissionId)
                    .ToHashSet();

                foreach (var pid in allPermissionIds)
                {
                    if (!existingIds.Contains(pid))
                    {
                        db.RolePermissions.Add(new RolePermission
                        {
                            RoleId = maire.Id,
                            PermissionId = pid
                        });
                    }
                }

                db.SaveChanges();
            }
        }
    }
}

