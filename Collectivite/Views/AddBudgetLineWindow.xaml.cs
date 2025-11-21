using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;


namespace Collectivite.Views
{
    public partial class AddBudgetLineWindow : Window
    {
        private readonly BudgetLineService _service;
        private readonly IEnumerable<Nommenclature> _available;

        public AddBudgetLineWindow(
             BudgetLineService service,
             int budgetPrimitifId,
             IEnumerable<Nommenclature> available)
        {
            InitializeComponent();

            _service = service;
            _available = available;

            // On appelle une méthode async quand la fenêtre est chargée
            Loaded += AddBudgetLineWindow_Loaded;
        }

        private async void AddBudgetLineWindow_Loaded(object sender, RoutedEventArgs e)
        {
            using var ctx = new AppDbContext();

            BudgetPrimitif? bp = await ctx.BudgetsPrimitifs
                .Include(b => b.Exercice)
                .FirstOrDefaultAsync(b => b.Exercice.EstCloture == false);
           

            if (bp == null)
            {
                MessageBox.Show("Aucun budget primitif actif trouvé.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            

            // Mise à jour du DataContext avec le bon ID
            DataContext = new AddBudgetLineViewModel(_service, bp.Id, _available);
        }
    }

}
