using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour ListeBudgetLinePage.xaml
    /// </summary>
    public partial class ListeBudgetLinePage : Page
    {
        public ListeBudgetLinePage()
        {
            InitializeComponent();
            // On attend que la Page soit entièrement chargée
            DataContext = new BudgetLinesViewModel(new BudgetLineService());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BudgetLinesViewModel viewModel)
            {
                viewModel.Dispose();
                //System.Diagnostics.Debug.WriteLine("BudgetLinesViewModel disposed");
            }
        }
    }
}
