using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour CompteBancairePage.xaml
    /// </summary>
    public partial class CompteBancairePage : Page
    {
        public CompteBancairePage()
        {
            InitializeComponent();

            // ✅ CORRECTION : Créer simplement le ViewModel
            // Plus besoin de passer les services
            var viewModel = new CompteBancaireViewModel();
            DataContext = viewModel;
        }
    }
}