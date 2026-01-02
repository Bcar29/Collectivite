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

            var exerciceService = new ExerciceService();

            DataContext = new ExerciceViewModel(exerciceService);
        }
    }
}