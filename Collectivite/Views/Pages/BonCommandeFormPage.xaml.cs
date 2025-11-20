using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class BonCommandeFormPage : Page
    {
        public BonCommandeFormPage(int? bonCommandeId = null)
        {
            InitializeComponent();
            DataContext = new BonCommandeFormViewModel(bonCommandeId);
        }
    }
}