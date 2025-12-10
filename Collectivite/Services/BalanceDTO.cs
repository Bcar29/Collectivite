using System;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Services
{
    /// <summary>
    /// DTO représentant une ligne de la Balance (un compte)
    /// </summary>
    public class BalanceLigneDTO
    {
        public int CompteId { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string IntituleCompte { get; set; } = string.Empty;

        // ═══════════════════════════════════════
        // DÉBIT
        // ═══════════════════════════════════════

        /// <summary>Balance d'entrée Débit (solde initial de l'exercice)</summary>
        public decimal DebitBalanceEntree { get; set; }

        /// <summary>Mouvements antérieurs Débit (avant le mois sélectionné)</summary>
        public decimal DebitMouvAnterieur { get; set; }

        /// <summary>Mouvements du mois Débit</summary>
        public decimal DebitMouvMois { get; set; }

        /// <summary>Total Débit = Balance Entrée + Mouv Antérieur + Mouv Mois</summary>
        public decimal DebitTotal => DebitBalanceEntree + DebitMouvAnterieur + DebitMouvMois;

        // ═══════════════════════════════════════
        // CRÉDIT
        // ═══════════════════════════════════════

        /// <summary>Balance d'entrée Crédit (solde initial de l'exercice)</summary>
        public decimal CreditBalanceEntree { get; set; }

        /// <summary>Mouvements antérieurs Crédit (avant le mois sélectionné)</summary>
        public decimal CreditMouvAnterieur { get; set; }

        /// <summary>Mouvements du mois Crédit</summary>
        public decimal CreditMouvMois { get; set; }

        /// <summary>Total Crédit = Balance Entrée + Mouv Antérieur + Mouv Mois</summary>
        public decimal CreditTotal => CreditBalanceEntree + CreditMouvAnterieur + CreditMouvMois;

        // ═══════════════════════════════════════
        // SOLDE
        // ═══════════════════════════════════════

        /// <summary>Solde Débiteur (si Total Débit > Total Crédit)</summary>
        public decimal SoldeDebiteur => DebitTotal > CreditTotal ? DebitTotal - CreditTotal : 0;

        /// <summary>Solde Créditeur (si Total Crédit > Total Débit)</summary>
        public decimal SoldeCrebiteur => CreditTotal > DebitTotal ? CreditTotal - DebitTotal : 0;

        // ═══════════════════════════════════════
        // FORMATAGE POUR L'AFFICHAGE
        // ═══════════════════════════════════════

        public string DebitBalanceEntreeFormate => DebitBalanceEntree > 0 ? DebitBalanceEntree.ToString("N0") : "";
        public string DebitMouvAnterieurFormate => DebitMouvAnterieur > 0 ? DebitMouvAnterieur.ToString("N0") : "";
        public string DebitMouvMoisFormate => DebitMouvMois > 0 ? DebitMouvMois.ToString("N0") : "";
        public string DebitTotalFormate => DebitTotal > 0 ? DebitTotal.ToString("N0") : "";

        public string CreditBalanceEntreeFormate => CreditBalanceEntree > 0 ? CreditBalanceEntree.ToString("N0") : "";
        public string CreditMouvAnterieurFormate => CreditMouvAnterieur > 0 ? CreditMouvAnterieur.ToString("N0") : "";
        public string CreditMouvMoisFormate => CreditMouvMois > 0 ? CreditMouvMois.ToString("N0") : "";
        public string CreditTotalFormate => CreditTotal > 0 ? CreditTotal.ToString("N0") : "";

        public string SoldeDebiteurFormate => SoldeDebiteur > 0 ? SoldeDebiteur.ToString("N0") : "";
        public string SoldeCrebiteurFormate => SoldeCrebiteur > 0 ? SoldeCrebiteur.ToString("N0") : "";
    }

    /// <summary>
    /// DTO pour les filtres de la Balance
    /// </summary>
    public class BalanceFiltreDTO
    {
        public int Annee { get; set; } = DateTime.Now.Year;
        public int Mois { get; set; } = DateTime.Now.Month;
        public string? NumeroCompte { get; set; }
        public string? RechercheTexte { get; set; }
        public bool AfficherComptesVides { get; set; } = false;
        public string? ClasseCompte { get; set; } // Pour filtrer par classe (1, 2, 3...)
    }

    /// <summary>
    /// DTO pour les totaux de la Balance
    /// </summary>
    public class BalanceTotauxDTO
    {
        // Totaux Débit
        public decimal TotalDebitBalanceEntree { get; set; }
        public decimal TotalDebitMouvAnterieur { get; set; }
        public decimal TotalDebitMouvMois { get; set; }
        public decimal TotalDebit => TotalDebitBalanceEntree + TotalDebitMouvAnterieur + TotalDebitMouvMois;

        // Totaux Crédit
        public decimal TotalCreditBalanceEntree { get; set; }
        public decimal TotalCreditMouvAnterieur { get; set; }
        public decimal TotalCreditMouvMois { get; set; }
        public decimal TotalCredit => TotalCreditBalanceEntree + TotalCreditMouvAnterieur + TotalCreditMouvMois;

        // Totaux Solde
        public decimal TotalSoldeDebiteur { get; set; }
        public decimal TotalSoldeCrebiteur { get; set; }

        // Vérification de l'équilibre
        public bool EstEquilibree => Math.Abs(TotalDebit - TotalCredit) < 0.01m &&
                                     Math.Abs(TotalSoldeDebiteur - TotalSoldeCrebiteur) < 0.01m;

        // Formatage
        public string TotalDebitBalanceEntreeFormate => TotalDebitBalanceEntree.ToString("N0");
        public string TotalDebitMouvAnterieurFormate => TotalDebitMouvAnterieur.ToString("N0");
        public string TotalDebitMouvMoisFormate => TotalDebitMouvMois.ToString("N0");
        public string TotalDebitFormate => TotalDebit.ToString("N0");

        public string TotalCreditBalanceEntreeFormate => TotalCreditBalanceEntree.ToString("N0");
        public string TotalCreditMouvAnterieurFormate => TotalCreditMouvAnterieur.ToString("N0");
        public string TotalCreditMouvMoisFormate => TotalCreditMouvMois.ToString("N0");
        public string TotalCreditFormate => TotalCredit.ToString("N0");

        public string TotalSoldeDebiteurFormate => TotalSoldeDebiteur.ToString("N0");
        public string TotalSoldeCrebiteurFormate => TotalSoldeCrebiteur.ToString("N0");
    }

    /// <summary>
    /// DTO pour les statistiques de la Balance
    /// </summary>
    public class BalanceStatsDTO
    {
        public int NombreComptes { get; set; }
        public int NombreComptesDebiteurs { get; set; }
        public int NombreComptesCrediteures { get; set; }
        public int NombreComptesEquilibres { get; set; }
        public decimal TotalMouvements { get; set; }

        public string TotalMouvementsFormate => TotalMouvements.ToString("N0") + " GNF";
    }
}