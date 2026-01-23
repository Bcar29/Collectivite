using System;
using static Collectivite.Models.Mandat;
using static Collectivite.Models.OrdreRecette;

namespace Collectivite.Services
{
    /// <summary>
    /// Enum pour le mode de règlement
    /// </summary>
    public enum ModeReglement
    {
        Virement,
        Cheque,
        Espece
    }

    /// <summary>
    /// DTO pour afficher un mandat avec son état de paiement
    /// </summary>
    public class MandatPaiementDTO
    {
        public int Id { get; set; }
        public string NumeroMandat { get; set; } = string.Empty;
        public string? Bordereau { get; set; }
        public DateTime DateEmission { get; set; }
        public string Objet { get; set; } = string.Empty;
        public string? Motif { get; set; }

        // Bénéficiaire (via Engagement -> Tiers)
        public string Beneficiaire { get; set; } = string.Empty;

        // Liens
        public int EngagementId { get; set; }
        public int? FactureId { get; set; }
        public int? BonCommandeId { get; set; }
        public bool HasFacture => FactureId.HasValue;
        public bool HasBonCommande => BonCommandeId.HasValue;

        // Montants
        public decimal MontantBrut { get; set; }
        public decimal MontantNet { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal MontantRestant => MontantNet - MontantPaye;
        public EtatMandat Etat { get; set; } = EtatMandat.Non_Validé;
        // État
        public bool EstTotalementPaye => MontantRestant <= 0;
        public decimal PourcentagePaye => MontantNet > 0 ? (MontantPaye / MontantNet) * 100 : 0;

        // Formatage
        public string MontantNetFormate => MontantNet.ToString("N0") + " GNF";
        public string MontantPayeFormate => MontantPaye.ToString("N0") + " GNF";
        public string MontantRestantFormate => MontantRestant.ToString("N0") + " GNF";
        public string DateEmissionFormatee => DateEmission.ToString("dd/MM/yyyy");
        public string PourcentagePayeFormate => PourcentagePaye.ToString("N1") + " %";
    }

    /// <summary>
    /// DTO pour afficher un ordre de recette avec son état d'encaissement
    /// </summary>
    public class OrdreRecetteEncaissementDTO
    {
        public int Id { get; set; }
        public string NumeroOrdre { get; set; } = string.Empty;
        public DateTime DateOrdre { get; set; }
        public string? Motifs { get; set; }
        public EtatOdre Etat { get; set; } = EtatOdre.Non_Validé;
        // Débiteur (Tiers)
        public string Debiteur { get; set; } = string.Empty;

        // Montants
        public decimal MontantOrdre { get; set; }
        public decimal MontantEncaisse { get; set; }
        public decimal MontantRestant => MontantOrdre - MontantEncaisse;

        // État
        public bool EstTotalementEncaisse => MontantRestant <= 0;
        public decimal PourcentageEncaisse => MontantOrdre > 0 ? (MontantEncaisse / MontantOrdre) * 100 : 0;

        // Formatage
        public string MontantOrdreFormate => MontantOrdre.ToString("N0") + " GNF";
        public string MontantEncaisseFormate => MontantEncaisse.ToString("N0") + " GNF";
        public string MontantRestantFormate => MontantRestant.ToString("N0") + " GNF";
        public string DateOrdreFormatee => DateOrdre.ToString("dd/MM/yyyy");
        public string PourcentageEncaisseFormate => PourcentageEncaisse.ToString("N1") + " %";
    }

    /// <summary>
    /// DTO pour la création d'un mouvement
    /// </summary>
    public class MouvementCreationDTO
    {
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public decimal Montant { get; set; }
        public ModeReglement ModeReglement { get; set; } = ModeReglement.Espece;

        // Champs spécifiques selon le mode
        public string? RefVirement { get; set; }
        public string? NumBanqueBenef { get; set; }
        public string? RefCheque { get; set; }

        // Fichier joint
        public byte[]? FichierJoint { get; set; }
        public string? FileName { get; set; }

        // Référence à la pièce
        public int? IdMandat { get; set; }
        public int? IdOrdreRecette { get; set; }
    }

    /// <summary>
    /// DTO pour afficher l'historique des mouvements
    /// CHANGEMENT: DateOnly → DateTime
    /// </summary>
    public class MouvementHistoriqueDTO
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }  
        public decimal Montant { get; set; }
        public string ModeReglement { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string CompteComptable { get; set; } = string.Empty;

        public string MontantFormate => Montant.ToString("N0") + " GNF";
        public string DateFormatee => Date.ToString("dd/MM/yyyy");
    }
}