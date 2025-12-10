using Collectivite.Models;
using Collectivite.Services;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Services
{
    public class CommuneService : ICommuneService
    {
        private static CommuneService? _instance { get; set; }
        private Commune? _communeCurrent;

        // Singleton
        public static CommuneService Instance => _instance ??= new CommuneService();

        // Commune courant
        public Commune? CurrentCommune
        {
            get => _communeCurrent;
            set => _communeCurrent = value;
        }
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        // Recuperer toutes les communes 
        public async Task<List<Commune>> GetAllCommuneAsync()
        {
            if (!SessionManager.HasPermission("Commune.View"))
                throw new UnauthorizedAccessException("Permission Commune.View requise pour consulter les communes.");

            using var context = CreateContext();
            return await context.Communes
                .AsNoTracking()  
                .OrderBy(c => c.Nom)
                .ToListAsync();
        }
        /// <summary>
        /// Récupère une commune selon son Id (VERSION SYNCHRONE)
        /// </summary>
        public Commune? GetCommuneById(int id)
        {
            using var context = CreateContext();
            return context.Communes
                .Include(c => c.DetailCommunes)
                .Include(c => c.Users)
                .Include(c => c.Engagements)
                .Include(c => c.Recensements)
                .FirstOrDefault(c => c.Id == id);  // ✅ Sans "Async"
        }

        // ajouter une communes
        public async Task<(bool Succes, string Message, Commune? Commune)> CreateCommuneAsync(Commune commune)
        {
            try
            {
                if (!SessionManager.HasPermission("Commune.Create"))
                    return (false, "Permission Commune.Create requise pour créer une commune.", null);

                using var context = CreateContext();
                var existe = await context.Communes
                    .AnyAsync(c => c.Nom == commune.Nom);
                if (existe)
                {
                    return (false, $"{commune.Nom} existe déjà ", null);
                }
                context.Communes.Add(commune);
                await context.SaveChangesAsync();
                return (true, $"la commune {commune.Nom} ajoute avec succes", commune);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la creation de la commune: {ex.Message}", null);
            }

        }

        // mettre à jour une commune
        public async Task<(bool Succes, string Message)> UpdateCommuneAsync(Commune commune)
        {
            try
            {
                if (!SessionManager.HasPermission("Commune.Edit"))
                    return (false, "Permission Commune.Edit requise pour modifier une commune.");

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 1 : DÉTACHER TOUTES LES ENTITÉS TRACKÉES
                // ══════════════════════════════════════════════════════════
                using var context = CreateContext();
                var trackedEntries = context.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Detached)
                    .ToList();

                foreach (var entry in trackedEntries)
                {
                    entry.State = EntityState.Detached;
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 2 : CHARGER L'ENTITÉ EXISTANTE
                // ══════════════════════════════════════════════════════════
                var existingCommune = await context.Communes
                    .FirstOrDefaultAsync(c => c.Id == commune.Id);

                if (existingCommune == null)
                {
                    return (false, "Commune non trouvée");
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 3 : VÉRIFIER L'UNICITÉ DU NOM
                // ══════════════════════════════════════════════════════════
                var existe = await context.Communes
                    .AnyAsync(c => c.Nom == commune.Nom && c.Id != commune.Id);

                if (existe)
                {
                    return (false, $"{commune.Nom} existe déjà");
                }

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 4 : METTRE À JOUR LES PROPRIÉTÉS
                // ══════════════════════════════════════════════════════════
                existingCommune.Nom = commune.Nom;
                existingCommune.DateCreation = commune.DateCreation;
                existingCommune.DistanceChefLieuRegion = commune.DistanceChefLieuRegion;
                existingCommune.DistanceChefLieuProvince = commune.DistanceChefLieuProvince;
                existingCommune.DistanceCapitale = commune.DistanceCapitale;

                // ══════════════════════════════════════════════════════════
                // ✅ ÉTAPE 5 : SAUVEGARDER
                // ══════════════════════════════════════════════════════════
                await context.SaveChangesAsync();

                return (true, "Commune mise à jour avec succès");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour de la commune : {ex.Message}");
            }
        }

        //supprimer une commune
        public async Task<(bool Succes, string Message)> DeleteCommuneAsync(int communeId)
        {
            try
            {
                if (!SessionManager.HasPermission("Commune.Delete"))
                    return (false, "Permission Commune.Delete requise pour supprimer une commune.");

                using var context = CreateContext();
                var existingCommune = await context.Communes
                    .FirstOrDefaultAsync(c => c.Id == communeId);
                if (existingCommune == null)
                {
                    return (false, "Commune non trouvée .");
                }
                context.Communes.Remove(existingCommune);
                await context.SaveChangesAsync();
                return (true, "Commune supprimée avec succès");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression de la commune : {ex.Message}");
            }
        }
    }
}