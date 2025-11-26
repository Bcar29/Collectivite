using Collectivite.Models;
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
    /// Logique d'interaction pour BudgetPrimitifPage.xaml
    /// </summary>
    public partial class BudgetPrimitifPage : Page
    {
        private readonly BudgetPrimitifViewModel _viewModel;

        public BudgetPrimitifPage()
        {
            InitializeComponent();

            // Créer et assigner le ViewModel
            _viewModel = new BudgetPrimitifViewModel(new BudgetPrimitifService());
            DataContext = _viewModel;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Disposer le ViewModel
            _viewModel?.Dispose();
        }
    }
}
