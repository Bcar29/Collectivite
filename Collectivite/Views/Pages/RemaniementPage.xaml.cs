using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class RemaniementPage : Page
    {
        public RemaniementPage()
        {
            InitializeComponent();

            // Initialiser le ViewModel
            var context = new AppDbContext();
            var remaniementService = new RemaniementService(context);
            var viewModel = new RemaniementViewModel(remaniementService);

            DataContext = viewModel;
        }
    }
}