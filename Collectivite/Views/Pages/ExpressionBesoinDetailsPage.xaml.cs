using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class ExpressionBesoinDetailsPage : Page
    {
        public ExpressionBesoinDetailsPage(int expressionBesoinId)
        {
            InitializeComponent();
            DataContext = new ExpressionBesoinDetailsViewModel(expressionBesoinId);
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}