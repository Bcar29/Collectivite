using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public interface ITiersGestionService
    {
        Task<List<TiersDebiteurDTO>> GetTiersDebiteursAsync(TiersFiltreDTO? filtre = null);
        Task<List<TiersCreancierDTO>> GetTiersCreanciersAsync(TiersFiltreDTO? filtre = null);
        Task<TiersStatistiquesDTO> GetStatistiquesAsync(int? exerciceId = null);
        Task<TiersDebiteurDTO?> GetDebiteurDetailAsync(int tiersId, int? exerciceId = null);
        Task<TiersCreancierDTO?> GetCreancierDetailAsync(int tiersId, int? exerciceId = null);
    }

    public class TiersGestionService : ITiersGestionService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // RÉCUPÉRATION DES DÉBITEURS
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<TiersDebiteurDTO>> GetTiersDebiteursAsync(TiersFiltreDTO? filtre = null)
        {
            try
            {
                using var context = new AppDbContext();

                var exerciceId = filtre?.ExerciceId ?? ExerciceService.Instance.CurrentExercice?.Id;

                if (exerciceId == null)
                {
                    return new List<TiersDebiteurDTO>();
                }

                // 1. Charger Engagements + Tiers (sans Mandat pour éviter les problèmes)
                var engagements = await context.Engagements
                    .Where(e => e.ExerciceId == exerciceId && e.TiersId != null)
                    .Include(e => e.Tiers)
                    .ToListAsync();

                if (!engagements.Any())
                {
                    return new List<TiersDebiteurDTO>();
                }

                // 2. Charger les mandats séparément
                var engagementIds = engagements.Select(e => e.Id).ToList();
                var mandats = await context.Mandats
                    .Where(m => engagementIds.Contains(m.EngagementId))
                    .ToListAsync();

                // 3. Charger les mouvements séparément (via idMandat)
                var mandatIds = mandats.Select(m => m.Id).ToList();
                var mouvements = await context.Mouvements
                    .Where(mv => mv.idMandat != null && mandatIds.Contains(mv.idMandat.Value))
                    .Select(mv => new { mv.idMandat, mv.Montant, mv.Date })
                    .ToListAsync();

                // 4. Dictionnaires pour accès rapide
                var mandatsByEngagement = mandats.ToDictionary(m => m.EngagementId, m => m);
                var mouvementsByMandat = mouvements
                    .GroupBy(m => m.idMandat!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => new { Total = g.Sum(m => m.Montant), DernierDate = g.Max(m => m.Date) }
                    );

                // 5. Grouper par Tiers
                var tiersGroupes = engagements
                    .Where(e => e.Tiers != null)
                    .GroupBy(e => e.TiersId!.Value)
                    .ToList();

                var result = new List<TiersDebiteurDTO>();

                foreach (var groupe in tiersGroupes)
                {
                    var tiers = groupe.First().Tiers!;

                    // Filtre recherche
                    if (!string.IsNullOrWhiteSpace(filtre?.RechercheTexte))
                    {
                        var recherche = filtre.RechercheTexte.ToLower();
                        var nomComplet = tiers.NomComplet?.ToLower() ?? "";
                        if (!nomComplet.Contains(recherche) &&
                            !(tiers.Telephone?.ToLower().Contains(recherche) ?? false) &&
                            !(tiers.Nif?.ToLower().Contains(recherche) ?? false) &&
                            !(tiers.Email?.ToLower().Contains(recherche) ?? false))
                        {
                            continue;
                        }
                    }

                    var dto = new TiersDebiteurDTO
                    {
                        TiersId = tiers.Id,
                        NomComplet = tiers.NomComplet ?? "N/A",
                        Adresse = tiers.Adresse,
                        Telephone = tiers.Telephone,
                        Email = tiers.Email,
                        TypeTiers = tiers.TypeDisplay,
                        CategorieTiers = tiers.CategorieDisplay,
                        Nif = tiers.Nif,
                        Rccm = tiers.Rccm,
                        NombreEngagements = groupe.Count()
                    };

                    DateTime? dernierPaiement = null;

                    foreach (var engagement in groupe)
                    {
                        if (mandatsByEngagement.TryGetValue(engagement.Id, out var mandat))
                        {
                            dto.NombreMandats++;
                            dto.TotalMontantAPayer += mandat.MontantNet;

                            decimal montantPaye = 0;
                            if (mouvementsByMandat.TryGetValue(mandat.Id, out var mvtInfo))
                            {
                                montantPaye = mvtInfo.Total;
                                var dateMvt = mvtInfo.DernierDate.ToDateTime(TimeOnly.MinValue);
                                if (dernierPaiement == null || dateMvt > dernierPaiement)
                                {
                                    dernierPaiement = dateMvt;
                                }
                            }

                            dto.TotalMontantPaye += montantPaye;

                            dto.Mandats.Add(new MandatDebiteurDTO
                            {
                                MandatId = mandat.Id,
                                Numero = mandat.NumeroMandat ?? $"M-{mandat.Id}",
                                DateMandat = mandat.DateEmission,
                                Objet = mandat.Objet,
                                Montant = mandat.MontantNet,
                                MontantPaye = montantPaye,
                                Statut = mandat.MandatStatut,
                                NumeroEngagement = $"ENG-{engagement.Id}"
                            });
                        }
                    }

                    dto.DateDernierPaiement = dernierPaiement;
                    dto.NombreMandatsPayes = dto.Mandats.Count(m => m.ResteAPayer == 0 && m.Montant > 0);
                    dto.NombreMandatsEnAttente = dto.Mandats.Count(m => m.ResteAPayer > 0);

                    // Filtres
                    if (!string.IsNullOrWhiteSpace(filtre?.Statut) && filtre.Statut != "Tous")
                    {
                        if (dto.Statut != filtre.Statut) continue;
                    }

                    if (filtre != null && !filtre.IncluireSoldes && dto.Statut == "Soldé")
                        continue;

                    result.Add(dto);
                }

                return result.OrderByDescending(d => d.ResteAPayer).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR Débiteurs: {ex.Message}");
                NotificationService.ShowError($"Erreur Débiteurs:\n{ex.Message}");
                return new List<TiersDebiteurDTO>();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RÉCUPÉRATION DES CRÉANCIERS
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<TiersCreancierDTO>> GetTiersCreanciersAsync(TiersFiltreDTO? filtre = null)
        {
            try
            {
                using var context = new AppDbContext();

                var exerciceId = filtre?.ExerciceId ?? ExerciceService.Instance.CurrentExercice?.Id;

                if (exerciceId == null)
                {
                    return new List<TiersCreancierDTO>();
                }

                // 1. Charger OrdreRecettes + Tiers (sans Mouvements)
                var ordresRecette = await context.OrdreRecettes
                    .Where(o => o.ExerciceId == exerciceId && o.TiersId != null)
                    .Include(o => o.Tiers)
                    .ToListAsync();

                if (!ordresRecette.Any())
                {
                    return new List<TiersCreancierDTO>();
                }

                // 2. Charger les mouvements séparément (via idOrdreRecette)
                var ordreIds = ordresRecette.Select(o => o.Id).ToList();
                var mouvements = await context.Mouvements
                    .Where(mv => mv.idOrdreRecette != null && ordreIds.Contains(mv.idOrdreRecette.Value))
                    .Select(mv => new { mv.idOrdreRecette, mv.Montant, mv.Date })
                    .ToListAsync();

                // 3. Dictionnaire pour accès rapide
                var mouvementsByOrdre = mouvements
                    .GroupBy(m => m.idOrdreRecette!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => new { Total = g.Sum(m => m.Montant), DernierDate = g.Max(m => m.Date) }
                    );

                // 4. Grouper par Tiers
                var tiersGroupes = ordresRecette
                    .Where(o => o.Tiers != null)
                    .GroupBy(o => o.TiersId!.Value)
                    .ToList();

                var result = new List<TiersCreancierDTO>();

                foreach (var groupe in tiersGroupes)
                {
                    var tiers = groupe.First().Tiers!;

                    // Filtre recherche
                    if (!string.IsNullOrWhiteSpace(filtre?.RechercheTexte))
                    {
                        var recherche = filtre.RechercheTexte.ToLower();
                        var nomComplet = tiers.NomComplet?.ToLower() ?? "";
                        if (!nomComplet.Contains(recherche) &&
                            !(tiers.Telephone?.ToLower().Contains(recherche) ?? false) &&
                            !(tiers.Nif?.ToLower().Contains(recherche) ?? false) &&
                            !(tiers.Email?.ToLower().Contains(recherche) ?? false))
                        {
                            continue;
                        }
                    }

                    var dto = new TiersCreancierDTO
                    {
                        TiersId = tiers.Id,
                        NomComplet = tiers.NomComplet ?? "N/A",
                        Adresse = tiers.Adresse,
                        Telephone = tiers.Telephone,
                        Email = tiers.Email,
                        TypeTiers = tiers.TypeDisplay,
                        CategorieTiers = tiers.CategorieDisplay,
                        Nif = tiers.Nif,
                        NombreOrdresRecette = groupe.Count()
                    };

                    DateTime? dernierEncaissement = null;

                    foreach (var ordre in groupe)
                    {
                        dto.TotalMontantAEncaisser += ordre.MontantOrdre;

                        decimal montantEncaisse = 0;
                        if (mouvementsByOrdre.TryGetValue(ordre.Id, out var mvtInfo))
                        {
                            montantEncaisse = mvtInfo.Total;
                            var dateMvt = mvtInfo.DernierDate.ToDateTime(TimeOnly.MinValue);
                            if (dernierEncaissement == null || dateMvt > dernierEncaissement)
                            {
                                dernierEncaissement = dateMvt;
                            }
                        }

                        dto.TotalMontantEncaisse += montantEncaisse;

                        dto.OrdresRecette.Add(new OrdreRecetteCreancierDTO
                        {
                            OrdreRecetteId = ordre.Id,
                            Numero = ordre.NumeroOrdre ?? $"OR-{ordre.Id}",
                            DateOrdre = ordre.DateOrdre,
                            Objet = ordre.Motifs,
                            Montant = ordre.MontantOrdre,
                            MontantEncaisse = montantEncaisse,
                            Statut = ordre.OrdreStatut
                        });
                    }

                    dto.DateDernierEncaissement = dernierEncaissement;
                    dto.NombreOrdresEncaisses = dto.OrdresRecette.Count(o => o.ResteAEncaisser == 0 && o.Montant > 0);
                    dto.NombreOrdresEnAttente = dto.OrdresRecette.Count(o => o.ResteAEncaisser > 0);

                    // Filtres
                    if (!string.IsNullOrWhiteSpace(filtre?.Statut) && filtre.Statut != "Tous")
                    {
                        var statutRecherche = filtre.Statut;
                        if (statutRecherche == "Non payé") statutRecherche = "Non encaissé";
                        if (dto.Statut != statutRecherche) continue;
                    }

                    if (filtre != null && !filtre.IncluireSoldes && dto.Statut == "Soldé")
                        continue;

                    result.Add(dto);
                }

                return result.OrderByDescending(c => c.ResteAEncaisser).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR Créanciers: {ex.Message}");
                NotificationService.ShowError($"Erreur Créanciers:\n{ex.Message}");
                return new List<TiersCreancierDTO>();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STATISTIQUES & DÉTAILS
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<TiersStatistiquesDTO> GetStatistiquesAsync(int? exerciceId = null)
        {
            var filtre = new TiersFiltreDTO
            {
                ExerciceId = exerciceId ?? ExerciceService.Instance.CurrentExercice?.Id,
                IncluireSoldes = true
            };

            var debiteurs = await GetTiersDebiteursAsync(filtre);
            var creanciers = await GetTiersCreanciersAsync(filtre);

            return new TiersStatistiquesDTO
            {
                NombreDebiteurs = debiteurs.Count,
                TotalAPayer = debiteurs.Sum(d => d.TotalMontantAPayer),
                TotalPaye = debiteurs.Sum(d => d.TotalMontantPaye),
                NombreCreanciers = creanciers.Count,
                TotalAEncaisser = creanciers.Sum(c => c.TotalMontantAEncaisser),
                TotalEncaisse = creanciers.Sum(c => c.TotalMontantEncaisse)
            };
        }

        public async Task<TiersDebiteurDTO?> GetDebiteurDetailAsync(int tiersId, int? exerciceId = null)
        {
            var filtre = new TiersFiltreDTO
            {
                ExerciceId = exerciceId ?? ExerciceService.Instance.CurrentExercice?.Id,
                IncluireSoldes = true
            };
            var debiteurs = await GetTiersDebiteursAsync(filtre);
            return debiteurs.FirstOrDefault(d => d.TiersId == tiersId);
        }

        public async Task<TiersCreancierDTO?> GetCreancierDetailAsync(int tiersId, int? exerciceId = null)
        {
            var filtre = new TiersFiltreDTO
            {
                ExerciceId = exerciceId ?? ExerciceService.Instance.CurrentExercice?.Id,
                IncluireSoldes = true
            };
            var creanciers = await GetTiersCreanciersAsync(filtre);
            return creanciers.FirstOrDefault(c => c.TiersId == tiersId);
        }
    }
}