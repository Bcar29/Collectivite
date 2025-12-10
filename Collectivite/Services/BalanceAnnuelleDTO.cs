using System;

namespace Collectivite.Services
{
    /// <summary>
    /// DTO représentant une ligne de la Balance Annuelle
    /// Structure : N° Comptes | Intitulés | Débit (Balance Entrée, Mouv Annuel, Total) | Crédit (Balance Entrée, Mouv Annuel, Total) | Solde (Débiteur, Créditeur)
    /// </summary>
    public class BalanceAnnuelleLigneDTO
    {
        public int CompteId { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string IntituleCompte { get; set; } = string.Empty;

        // ═══════════════════════════════════════
        // DÉBIT
        // ═══════════════════════════════════════

        /// <summary>
        /// Balance d'entrée au débit (solde initial de l'exercice)
        /// </summary>
        public decimal DebitBalanceEntree { get; set; }

        /// <summary>
        /// Mouvements annuels au débit (total des débits de l'année)
        /// </summary>
        public decimal DebitMouvAnnuel { get; set; }

        /// <summary>
        /// Total Débit = Balance Entrée + Mouv Annuel
        /// </summary>
        public decimal DebitTotal => DebitBalanceEntree + DebitMouvAnnuel;

        // ═══════════════════════════════════════
        // CRÉDIT
        // ═══════════════════════════════════════

        /// <summary>
        /// Balance d'entrée au crédit (solde initial de l'exercice)
        /// </summary>
        public decimal CreditBalanceEntree { get; set; }

        /// <summary>
        /// Mouvements annuels au crédit (total des crédits de l'année)
        /// </summary>
        public decimal CreditMouvAnnuel { get; set; }

        /// <summary>
        /// Total Crédit = Balance Entrée + Mouv Annuel
        /// </summary>
        public decimal CreditTotal => CreditBalanceEntree + CreditMouvAnnuel;

        // ═══════════════════════════════════════
        // SOLDE
        // ═══════════════════════════════════════

        /// <summary>
        /// Solde débiteur (si Total Débit > Total Crédit)
        /// </summary>
        public decimal SoldeDebiteur => DebitTotal > CreditTotal ? DebitTotal - CreditTotal : 0;

        /// <summary>
        /// Solde créditeur (si Total Crédit > Total Débit)
        /// </summary>
        public decimal SoldeCrebiteur => CreditTotal > DebitTotal ? CreditTotal - DebitTotal : 0;

        // ═══════════════════════════════════════
        // PROPRIÉTÉS FORMATÉES POUR L'AFFICHAGE
        // ═══════════════════════════════════════

        public string DebitBalanceEntreeFormate => DebitBalanceEntree > 0 ? DebitBalanceEntree.ToString("N0") : "";
        public string DebitMouvAnnuelFormate => DebitMouvAnnuel > 0 ? DebitMouvAnnuel.ToString("N0") : "";
        public string DebitTotalFormate => DebitTotal > 0 ? DebitTotal.ToString("N0") : "";

        public string CreditBalanceEntreeFormate => CreditBalanceEntree > 0 ? CreditBalanceEntree.ToString("N0") : "";
        public string CreditMouvAnnuelFormate => CreditMouvAnnuel > 0 ? CreditMouvAnnuel.ToString("N0") : "";
        public string CreditTotalFormate => CreditTotal > 0 ? CreditTotal.ToString("N0") : "";

        public string SoldeDebiteurFormate => SoldeDebiteur > 0 ? SoldeDebiteur.ToString("N0") : "";
        public string SoldeCrebiteurFormate => SoldeCrebiteur > 0 ? SoldeCrebiteur.ToString("N0") : "";
    }

    /// <summary>
    /// DTO pour les filtres de la Balance Annuelle
    /// </summary>
    public class BalanceAnnuelleFiltreDTO
    {
        /// <summary>
        /// Année de la balance
        /// </summary>
        public int Annee { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Filtre par numéro de compte (optionnel)
        /// </summary>
        public string? NumeroCompte { get; set; }

        /// <summary>
        /// Recherche textuelle dans numéro ou intitulé
        /// </summary>
        public string? RechercheTexte { get; set; }

        /// <summary>
        /// Afficher les comptes sans mouvements
        /// </summary>
        public bool AfficherComptesVides { get; set; } = false;

        /// <summary>
        /// Filtrer par classe de compte (1 à 9)
        /// </summary>
        public string? ClasseCompte { get; set; }
    }

    /// <summary>
    /// DTO pour les totaux de la Balance Annuelle
    /// </summary>
    public class BalanceAnnuelleTotauxDTO
    {
        // Totaux Débit
        public decimal TotalDebitBalanceEntree { get; set; }
        public decimal TotalDebitMouvAnnuel { get; set; }
        public decimal TotalDebit => TotalDebitBalanceEntree + TotalDebitMouvAnnuel;

        // Totaux Crédit
        public decimal TotalCreditBalanceEntree { get; set; }
        public decimal TotalCreditMouvAnnuel { get; set; }
        public decimal TotalCredit => TotalCreditBalanceEntree + TotalCreditMouvAnnuel;

        // Totaux Solde
        public decimal TotalSoldeDebiteur { get; set; }
        public decimal TotalSoldeCrebiteur { get; set; }

        /// <summary>
        /// Vérifie si la balance est équilibrée
        /// </summary>
        public bool EstEquilibree => Math.Abs(TotalDebit - TotalCredit) < 0.01m;

        // Propriétés formatées
        public string TotalDebitBalanceEntreeFormate => TotalDebitBalanceEntree.ToString("N0");
        public string TotalDebitMouvAnnuelFormate => TotalDebitMouvAnnuel.ToString("N0");
        public string TotalDebitFormate => TotalDebit.ToString("N0");

        public string TotalCreditBalanceEntreeFormate => TotalCreditBalanceEntree.ToString("N0");
        public string TotalCreditMouvAnnuelFormate => TotalCreditMouvAnnuel.ToString("N0");
        public string TotalCreditFormate => TotalCredit.ToString("N0");

        public string TotalSoldeDebiteurFormate => TotalSoldeDebiteur.ToString("N0");
        public string TotalSoldeCrebiteurFormate => TotalSoldeCrebiteur.ToString("N0");
    }

    /// <summary>
    /// DTO pour les statistiques de la Balance Annuelle
    /// </summary>
    public class BalanceAnnuelleStatsDTO
    {
        public int NombreComptes { get; set; }
        public int NombreComptesDebiteurs { get; set; }
        public int NombreComptesCrediteures { get; set; }
        public int NombreComptesEquilibres { get; set; }
        public decimal TotalMouvements { get; set; }
    }
}