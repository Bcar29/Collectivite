using System;
using System.Collections.Generic;

namespace Collectivite.Models
{
    // ═══════════════════════════════════════════════════════════════════════
    // DTOs POUR LA GESTION DES TIERS (DÉBITEURS ET CRÉANCIERS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// DTO représentant un tiers débiteur (lié aux engagements/mandats)
    /// </summary>
    public class TiersDebiteurDTO
    {
        public int TiersId { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string? Adresse { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }

        // Informations supplémentaires
        public string TypeTiers { get; set; } = string.Empty;
        public string CategorieTiers { get; set; } = string.Empty;
        public string? Nif { get; set; }
        public string? Rccm { get; set; }

        // Situation financière
        public decimal TotalMontantAPayer { get; set; }
        public decimal TotalMontantPaye { get; set; }
        public decimal ResteAPayer => TotalMontantAPayer - TotalMontantPaye;

        // Statistiques
        public int NombreEngagements { get; set; }
        public int NombreMandats { get; set; }
        public int NombreMandatsPayes { get; set; }
        public int NombreMandatsEnAttente { get; set; }

        // Dernier mouvement
        public DateTime? DateDernierPaiement { get; set; }

        // Formatage pour affichage
        public string TotalMontantAPayerFormate => $"{TotalMontantAPayer:N0}";
        public string TotalMontantPayeFormate => $"{TotalMontantPaye:N0}";
        public string ResteAPayerFormate => $"{ResteAPayer:N0}";
        public string DateDernierPaiementFormate => DateDernierPaiement?.ToString("dd/MM/yyyy") ?? "-";

        // Taux de paiement
        public decimal TauxPaiement => TotalMontantAPayer > 0
            ? Math.Round((TotalMontantPaye / TotalMontantAPayer) * 100, 2)
            : 0;
        public string TauxPaiementFormate => $"{TauxPaiement:N1}%";

        // Statut visuel
        public string Statut
        {
            get
            {
                if (ResteAPayer == 0 && TotalMontantAPayer > 0) return "Soldé";
                if (TotalMontantPaye > 0) return "En cours";
                return "Non payé";
            }
        }

        public string StatutCouleur
        {
            get
            {
                return Statut switch
                {
                    "Soldé" => "#4CAF50",      // Vert
                    "En cours" => "#FF9800",   // Orange
                    _ => "#F44336"             // Rouge
                };
            }
        }

        // Détails des mandats
        public List<MandatDebiteurDTO> Mandats { get; set; } = new();
    }

    /// <summary>
    /// DTO représentant un mandat lié à un débiteur
    /// </summary>
    public class MandatDebiteurDTO
    {
        public int MandatId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime DateMandat { get; set; }
        public string? Objet { get; set; }
        public decimal Montant { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal ResteAPayer => Montant - MontantPaye;
        public string Statut { get; set; } = string.Empty;
        public string? NumeroEngagement { get; set; }

        // Formatage
        public string DateMandatFormate => DateMandat.ToString("dd/MM/yyyy");
        public string MontantFormate => $"{Montant:N0}";
        public string MontantPayeFormate => $"{MontantPaye:N0}";
        public string ResteAPayerFormate => $"{ResteAPayer:N0}";
    }

    /// <summary>
    /// DTO représentant un tiers créancier (lié aux ordres de recette)
    /// </summary>
    public class TiersCreancierDTO
    {
        public int TiersId { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string? Adresse { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }

        // Informations supplémentaires
        public string TypeTiers { get; set; } = string.Empty;
        public string CategorieTiers { get; set; } = string.Empty;
        public string? Nif { get; set; }

        // Situation financière
        public decimal TotalMontantAEncaisser { get; set; }
        public decimal TotalMontantEncaisse { get; set; }
        public decimal ResteAEncaisser => TotalMontantAEncaisser - TotalMontantEncaisse;

        // Statistiques
        public int NombreOrdresRecette { get; set; }
        public int NombreOrdresEncaisses { get; set; }
        public int NombreOrdresEnAttente { get; set; }

        // Dernier mouvement
        public DateTime? DateDernierEncaissement { get; set; }

        // Formatage pour affichage
        public string TotalMontantAEncaisserFormate => $"{TotalMontantAEncaisser:N0}";
        public string TotalMontantEncaisseFormate => $"{TotalMontantEncaisse:N0}";
        public string ResteAEncaisserFormate => $"{ResteAEncaisser:N0}";
        public string DateDernierEncaissementFormate => DateDernierEncaissement?.ToString("dd/MM/yyyy") ?? "-";

        // Taux d'encaissement
        public decimal TauxEncaissement => TotalMontantAEncaisser > 0
            ? Math.Round((TotalMontantEncaisse / TotalMontantAEncaisser) * 100, 2)
            : 0;
        public string TauxEncaissementFormate => $"{TauxEncaissement:N1}%";

        // Statut visuel
        public string Statut
        {
            get
            {
                if (ResteAEncaisser == 0 && TotalMontantAEncaisser > 0) return "Soldé";
                if (TotalMontantEncaisse > 0) return "En cours";
                return "Non encaissé";
            }
        }

        public string StatutCouleur
        {
            get
            {
                return Statut switch
                {
                    "Soldé" => "#4CAF50",      // Vert
                    "En cours" => "#FF9800",   // Orange
                    _ => "#F44336"             // Rouge
                };
            }
        }

        // Détails des ordres de recette
        public List<OrdreRecetteCreancierDTO> OrdresRecette { get; set; } = new();
    }

    /// <summary>
    /// DTO représentant un ordre de recette lié à un créancier
    /// </summary>
    public class OrdreRecetteCreancierDTO
    {
        public int OrdreRecetteId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime DateOrdre { get; set; }
        public string? Objet { get; set; }
        public decimal Montant { get; set; }
        public decimal MontantEncaisse { get; set; }
        public decimal ResteAEncaisser => Montant - MontantEncaisse;
        public string Statut { get; set; } = string.Empty;

        // Formatage
        public string DateOrdreFormate => DateOrdre.ToString("dd/MM/yyyy");
        public string MontantFormate => $"{Montant:N0}";
        public string MontantEncaisseFormate => $"{MontantEncaisse:N0}";
        public string ResteAEncaisserFormate => $"{ResteAEncaisser:N0}";
    }

    /// <summary>
    /// Statistiques globales des tiers
    /// </summary>
    public class TiersStatistiquesDTO
    {
        // Débiteurs
        public int NombreDebiteurs { get; set; }
        public decimal TotalAPayer { get; set; }
        public decimal TotalPaye { get; set; }
        public decimal ResteAPayer => TotalAPayer - TotalPaye;

        // Créanciers
        public int NombreCreanciers { get; set; }
        public decimal TotalAEncaisser { get; set; }
        public decimal TotalEncaisse { get; set; }
        public decimal ResteAEncaisser => TotalAEncaisser - TotalEncaisse;

        // Formatage
        public string TotalAPayerFormate => $"{TotalAPayer:N0}";
        public string TotalPayeFormate => $"{TotalPaye:N0}";
        public string ResteAPayerFormate => $"{ResteAPayer:N0}";
        public string TotalAEncaisserFormate => $"{TotalAEncaisser:N0}";
        public string TotalEncaisseFormate => $"{TotalEncaisse:N0}";
        public string ResteAEncaisserFormate => $"{ResteAEncaisser:N0}";
    }

    /// <summary>
    /// Filtre pour la recherche des tiers
    /// </summary>
    public class TiersFiltreDTO
    {
        public int? ExerciceId { get; set; }
        public string? RechercheTexte { get; set; }
        public string? Statut { get; set; } // "Tous", "Soldé", "En cours", "Non payé/encaissé"
        public bool IncluireSoldes { get; set; } = true;
        public TiersType? TypeTiers { get; set; } // Filtrer par type
    }
}