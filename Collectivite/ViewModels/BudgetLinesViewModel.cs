using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class BudgetLinesViewModel : ViewModelBase
    {
        private readonly BudgetLineService _service;
        private readonly int _budgetPrimitifId;

        public ObservableCollection<BudgetLine> DisplayedLines { get; } = new ObservableCollection<BudgetLine>();

        // onglet sélectionné (0..3)
        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                SetProperty(ref _selectedTabIndex, value);
                _ = LoadForSelectedTabAsync();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand RefreshCommand { get; }

        public BudgetLinesViewModel(BudgetLineService service, int budgetPrimitifId)
        {
            _service = service;
            _budgetPrimitifId = budgetPrimitifId;

            AddCommand = new RelayCommand(async _ => await OpenAddDialogAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadForSelectedTabAsync());

            _ = LoadForSelectedTabAsync();
        }

        private (NatureType nature, SectionType section) TabToFilter(int tabIndex)
        {
            // 0: Recette Fonctionnement
            // 1: Recette Investissement
            // 2: Depense Fonctionnement
            // 3: Depense Investissement
            return tabIndex switch
            {
                0 => (NatureType.Recette, SectionType.Fonctionnement),
                1 => (NatureType.Recette, SectionType.Investissement),
                2 => (NatureType.Depense, SectionType.Fonctionnement),
                3 => (NatureType.Depense, SectionType.Investissement),
                _ => (NatureType.Recette, SectionType.Fonctionnement)
            };
        }

        public async Task LoadForSelectedTabAsync()
        {
            var filter = TabToFilter(SelectedTabIndex);

            var all = await _service.GetBudgetLinesForBudgetPrimitifAsync(_budgetPrimitifId);
            var filtered = all.Where(b => b.Nommenclature.Nature == filter.nature && b.Nommenclature.Section == filter.section).ToList();

            DisplayedLines.Clear();
            foreach (var l in filtered.OrderBy(b => b.Nommenclature.Intitule))
                DisplayedLines.Add(l);
        }

        // Ouvre le dialog d'ajout (ici on utilise un ViewModel de dialog simple)
        private async Task OpenAddDialogAsync()
        {
            var filter = TabToFilter(SelectedTabIndex);
            var available = await _service.GetLeafNomenclaturesNotLinkedAsync(
                _budgetPrimitifId,
                filter.nature,
                filter.section
            );

            //var addVm = new AddBudgetLineViewModel(_service, _budgetPrimitifId, available);
            var dialog = new Views.AddBudgetLineWindow(_service, _budgetPrimitifId, available);

            if (dialog.ShowDialog() == true)
                await LoadForSelectedTabAsync();
        }

    }
}
