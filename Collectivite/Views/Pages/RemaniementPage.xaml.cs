using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour RemaniementPage.xaml
    /// </summary>
    public partial class RemaniementPage : Page
    {
        public RemaniementPage()
        {
            InitializeComponent();

            // ✅ CORRECTION : Créer le ViewModel sans passer de service
            var viewModel = new RemaniementViewModel();
            DataContext = viewModel;
        }
    }
}