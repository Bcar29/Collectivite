using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour EngagementFormPage.xaml
    /// </summary>
    public partial class EngagementFormPage : Page
    {
        private readonly BudgetLineService _service;
        public EngagementFormPage(int? engagementId = null)
        {
            InitializeComponent();

            _service = new BudgetLineService();

            // On attend que la Page soit entièrement chargée
            Loaded += BudgetLinesPage_Loaded; ;
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
            this.DataContext = new EngagementFormViewModel(bp.Id);
        }
    }
}