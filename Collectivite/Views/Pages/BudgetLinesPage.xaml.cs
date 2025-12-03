using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
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
    /// Logique d'interaction pour BudgetLinesPage.xaml
    /// </summary>
    public partial class BudgetLinesPage : Page
    {
        public BudgetLinesPage()
        {
            InitializeComponent();

            //_service = new BudgetLineService();

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
