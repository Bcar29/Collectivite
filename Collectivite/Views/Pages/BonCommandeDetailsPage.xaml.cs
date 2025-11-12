using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class BonCommandeDetailsPage : Page
    {
        public BonCommandeDetailsPage(int bonCommandeId)
        {
            InitializeComponent();
            DataContext = new BonCommandeDetailsViewModel(bonCommandeId);
        }

        // Numérotation automatique des lignes dans le DataGrid
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}