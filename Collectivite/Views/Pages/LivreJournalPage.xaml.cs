using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Page de gestion du Livre Journal
    /// </summary>
    public partial class LivreJournalPage : Page
    {
        private readonly LivreJournalViewModel _viewModel;

        public LivreJournalPage()
        {
            InitializeComponent();

            _viewModel = new LivreJournalViewModel();
            DataContext = _viewModel;

            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadDataAsync();
        }
    }
}