using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class ExercicePage : Page
    {
        public ExercicePage()
        {
            InitializeComponent();

            // Initialiser le ViewModel
            //var context = new AppDbContext();
            var exerciceService = new ExerciceService();
            var viewModel = new ExerciceViewModel(exerciceService);

            DataContext = viewModel;
        }
    }
}