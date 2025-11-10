using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class FacturePage : Page
    {
        public FacturePage()
        {
            InitializeComponent();
            DataContext = new FactureViewModel();
        }
    }
}