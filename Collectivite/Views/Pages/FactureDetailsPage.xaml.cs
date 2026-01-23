using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class FactureDetailsPage : Page
    {
        public FactureDetailsPage(int factureId)
        {
            InitializeComponent();
            DataContext = new FactureDetailsViewModel(factureId);
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
