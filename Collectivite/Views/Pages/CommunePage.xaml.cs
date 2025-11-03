using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour CommunePage.xaml
    /// </summary>
    public partial class CommunePage : Page
    {
        public CommunePage()
        {
            InitializeComponent();

            // Initialisation du ViewModel
            var context = new AppDbContext();
            var communeService = new CommuneService(context);
            var viewModel = new CommuneViewModel(communeService);

            // ⚠️ IMPORTANT : Définir le DataContext pour le binding
            DataContext = viewModel;
        }
    }
}