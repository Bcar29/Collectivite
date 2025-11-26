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
        private readonly BudgetLineService _service;

        public BudgetLinesPage()
        {
            InitializeComponent();

            _service = new BudgetLineService();

            // On attend que la Page soit entièrement chargée
            Loaded += BudgetLinesPage_Loaded;
        }

        private async void BudgetLinesPage_Loaded(object sender, RoutedEventArgs e)
        {
            using var ctx = new AppDbContext();

            // 🔍 Récupérer le budget primitif actif
            BudgetPrimitif? bp = await ctx.BudgetsPrimitifs
                .Include(b => b.Exercice)
                .FirstOrDefaultAsync(b => b.Exercice.EstCloture == false);

            if (bp == null)
            {
                MessageBox.Show("Aucun budget primitif actif trouvé.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ✔ Mettre la valeur dynamique dans le ViewModel
            this.DataContext = new BudgetLinesViewModel(_service, bp.Id);
        }
    }

}
