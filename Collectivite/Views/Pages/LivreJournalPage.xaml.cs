using System.Windows.Controls;
using Collectivite.Services;
using Collectivite.ViewModels;
namespace Collectivite.Views.Pages
{
    public partial class LivreJournalPage : Page
    {
        public LivreJournalPage()
        {
            InitializeComponent();
            
            var context = new AppDbContext();
            var ecritureComptableService = new EcritureComptableService(context);
            var compteComptableService = new CompteComptableService(context);

            var viewModel = new LivreJournalViewModel(ecritureComptableService, compteComptableService);
            DataContext = viewModel;
        }
    }
}