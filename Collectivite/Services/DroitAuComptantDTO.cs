using System;

namespace Collectivite.Services
{
    

    /// <summary>
    /// DTO pour l'affichage des imputations dans le ComboBox
    /// </summary>
    public class ImputationDTO
    {
        public int BudgetLineId { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public decimal TotalMouvement { get; set; }
        public int CompteComptableId { get; set; }

        /// <summary>
        /// Texte affiché dans le ComboBox : "numéro - libellé - (total GNF)"
        /// </summary>
        public string DisplayText => $"{NumeroCompte} - {Libelle} - ({TotalMouvement:N0} GNF)";
    }

    /// <summary>
    /// DTO pour l'affichage de la liste des droits au comptant
    /// </summary>
    public class DroitAuComptantDTO
    {
        public int OrdreRecetteId { get; set; }
        public string NumeroOrdre { get; set; } = string.Empty;
        public DateOnly DateOrdre { get; set; }
        public string Imputation { get; set; } = string.Empty;
        public string Debiteur { get; set; } = string.Empty;
        public decimal MontantOrdre { get; set; }
        public decimal MontantEncaisse { get; set; }
        public string ModeReglement { get; set; } = string.Empty;
        public int? MouvementId { get; set; }

        // Informations supplémentaires pour la modification
        public int BudgetLineId { get; set; }
        public int? TiersId { get; set; }
        public string? Comptable { get; set; }
        public string? Motifs { get; set; }
        public string? RefVirement { get; set; }
        public string? NumBanqueBenef { get; set; }
        public string? RefCheque { get; set; }

        /// <summary>
        /// Date formatée pour l'affichage
        /// </summary>
        public string DateOrdreFormatee => DateOrdre.ToString("dd/MM/yyyy");

        /// <summary>
        /// Montant de l'ordre formaté
        /// </summary>
        public string MontantOrdreFormate => MontantOrdre.ToString("N0") + " GNF";

        /// <summary>
        /// Montant encaissé formaté
        /// </summary>
        public string MontantEncaisseFormate => MontantEncaisse.ToString("N0") + " GNF";
    }

    /// <summary>
    /// DTO pour la création d'une nouvelle opération
    /// </summary>
    public class DroitAuComptantCreationDTO
    {
        // Imputation
        public int BudgetLineId { get; set; }
        public int CompteComptableId { get; set; }

        // Informations de l'ordre
        public string? NumeroOrdre { get; set; }
        public DateOnly DateOrdre { get; set; }
        public string? Motifs { get; set; }
        public string Comptable { get; set; } = string.Empty;
        public int? TiersId { get; set; }

        // Encaissement
        public decimal Montant { get; set; }
        public ModeReglement ModeReglement { get; set; }
        public string? RefVirement { get; set; }
        public string? NumBanqueBenef { get; set; }
        public string? RefCheque { get; set; }
    }

    /// <summary>
    /// DTO pour la modification d'une opération existante
    /// </summary>
    public class DroitAuComptantModificationDTO
    {
        // Identifiants de l'opération à modifier
        public int OrdreRecetteId { get; set; }
        public int? MouvementId { get; set; }

        // Informations modifiables
        public DateOnly DateOrdre { get; set; }
        public decimal Montant { get; set; }
        public string Comptable { get; set; } = string.Empty;
        public string? Motifs { get; set; }
        public int? TiersId { get; set; }

        // Mode de règlement et références
        public ModeReglement ModeReglement { get; set; }
        public string? RefVirement { get; set; }
        public string? NumBanqueBenef { get; set; }
        public string? RefCheque { get; set; }
    }
}