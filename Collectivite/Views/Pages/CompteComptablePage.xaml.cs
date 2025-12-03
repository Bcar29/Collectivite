using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour CompteComptablePage.xaml
    /// Version adaptée avec support des nomenclatures budgétaires
    /// Solution 1 : Code-behind minimal (RECOMMANDÉ)
    /// </summary>
    public partial class CompteComptablePage : Page
    {
        public CompteComptablePage()
        {
            InitializeComponent();

            // Création des instances (votre approche actuelle)
            var context = new AppDbContext();
            var compteComptableService = new CompteComptableService(context);
            var nomenclatureService = new NommenclatureService(context);

            // Création du ViewModel avec les 2 services
            var viewModel = new CompteComptableViewModel(compteComptableService, nomenclatureService);

            // ⚠️ IMPORTANT : Définir le DataContext pour le binding
            DataContext = viewModel;

            // Note: Le constructeur du ViewModel charge déjà les comptes automatiquement
            // Les nomenclatures sont chargées automatiquement via les PropertyChanged des filtres
        }
    }
}