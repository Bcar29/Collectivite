using Collectivite.Models;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// DTO pour afficher l'ordre de recette avec les infos de paiement
    /// Sans modifier le modèle OrdreRecette
    /// </summary>
    public class OrdreRecetteDisplay
    {
        public OrdreRecette OrdreRecette { get; set; }

        // Montant encaissé (somme des mouvements)
        public decimal MontantEncaisse { get; set; }

        // Statut calculé
        public OrdreRecette.StatutOrdre Statut { get; set; }

        // Propriétés de raccourci pour le binding XAML
        public int Id => OrdreRecette.Id;
        public string NumeroOrdre => OrdreRecette.NumeroOrdre;
        public DateTime DateOrdre => OrdreRecette.DateOrdre;
        public decimal MontantOrdre => OrdreRecette.MontantOrdre;
        public string? Comptable => OrdreRecette.Comptable;
        public Exercice Exercice => OrdreRecette.Exercice;
        public Commune Commune => OrdreRecette.Commune;
        public BudgetLine BudgetLine => OrdreRecette.BudgetLine;
        public Tiers? Tiers => OrdreRecette.Tiers;

        // Texte du statut pour affichage
        public string StatutTexte => Statut switch
        {
            OrdreRecette.StatutOrdre.Non_Encaissé => "Non Encaissé",
            OrdreRecette.StatutOrdre.Partiel => "Partiel",
            OrdreRecette.StatutOrdre.Enciassé => "Encaissé",
            _ => "Inconnu"
        };

        public OrdreRecetteDisplay(OrdreRecette ordreRecette, decimal montantEncaisse, OrdreRecette.StatutOrdre statut)
        {
            OrdreRecette = ordreRecette;
            MontantEncaisse = montantEncaisse;
            Statut = statut;
        }
    }
}