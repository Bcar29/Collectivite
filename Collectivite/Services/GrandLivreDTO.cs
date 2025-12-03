using System;
using System.Collections.Generic;

namespace Collectivite.Services
{
    /// <summary>
    /// DTO représentant un compte dans le Grand Livre
    /// </summary>
    public class GrandLivreCompteDTO
    {
        public int CompteId { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string IntituleCompte { get; set; } = string.Empty;

        // Liste des mouvements sur ce compte
        public List<GrandLivreMouvementDTO> Mouvements { get; set; } = new();

        // Totaux calculés
        public decimal TotalDebit => Mouvements.Sum(m => m.MontantDebit);
        public decimal TotalCredit => Mouvements.Sum(m => m.MontantCredit);
        public decimal Solde => TotalDebit - TotalCredit;

        // Type de solde pour l'affichage
        public string TypeSolde => Solde >= 0 ? "Débiteur" : "Créditeur";
        public decimal SoldeAbsolu => Math.Abs(Solde);

        // Pour l'affichage formaté
        public string TotalDebitFormate => TotalDebit.ToString("N0");
        public string TotalCreditFormate => TotalCredit.ToString("N0");
        public string SoldeFormate => $"{SoldeAbsolu:N0} ({TypeSolde})";
    }

    /// <summary>
    /// DTO représentant un mouvement (ligne) dans le Grand Livre
    /// </summary>
    public class GrandLivreMouvementDTO
    {
        public int EcritureId { get; set; }
        public DateOnly DateEcriture { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string CompteContrepartie { get; set; } = string.Empty;
        public decimal MontantDebit { get; set; }
        public decimal MontantCredit { get; set; }
        public decimal SoldeCumulé { get; set; }

        // Référence document (ordre de recette ou mandat)
        public string Reference { get; set; } = string.Empty;
        public string TypeDocument { get; set; } = string.Empty; // "Recette" ou "Dépense"

        // Pour l'affichage formaté
        public string DateFormattee => DateEcriture.ToString("dd/MM/yyyy");
        public string DebitFormate => MontantDebit > 0 ? MontantDebit.ToString("N0") : "";
        public string CreditFormate => MontantCredit > 0 ? MontantCredit.ToString("N0") : "";
    }

    /// <summary>
    /// DTO pour les filtres du Grand Livre
    /// </summary>
    public class GrandLivreFiltreDTO
    {
        public int? Annee { get; set; }
        public int? Mois { get; set; }
        public string? NumeroCompte { get; set; }
        public string? RechercheTexte { get; set; }
        public DateOnly? DateDebut { get; set; }
        public DateOnly? DateFin { get; set; }
        public bool IncluреComptesVides { get; set; } = false;
    }

    /// <summary>
    /// DTO pour les statistiques globales du Grand Livre
    /// </summary>
    public class GrandLivreStatsDTO
    {
        public int NombreComptes { get; set; }
        public int NombreEcritures { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal Equilibre => TotalDebits - TotalCredits;
        public bool EstEquilibre => Math.Abs(Equilibre) < 0.01m;

        public string TotalDebitsFormate => TotalDebits.ToString("N0") + " GNF";
        public string TotalCreditsFormate => TotalCredits.ToString("N0") + " GNF";
    }
}