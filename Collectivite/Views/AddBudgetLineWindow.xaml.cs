using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;


namespace Collectivite.Views
{
    public partial class AddBudgetLineWindow : Window
    {
        public AddBudgetLineWindow(
             BudgetLineService service,
             int budgetPrimitifId,
             IEnumerable<Nommenclature> available)
        {
            InitializeComponent();

            DataContext = new AddBudgetLineViewModel(service, 1, available);
        }
    }
}
