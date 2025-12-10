using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class AddBudgetLineViewModel : ViewModelBase
    {
        private readonly BudgetLineService _service;
        private readonly int _budgetPrimitifId;
        private readonly AuditService _auditService;
        private readonly AuthService _authService;

        public ObservableCollection<Nommenclature> AvailableNoms { get; } = new ObservableCollection<Nommenclature>();

        private Nommenclature? _selectedNomenclature;
        public Nommenclature? SelectedNomenclature
        {
            get => _selectedNomenclature;
            set => SetProperty(ref _selectedNomenclature, value);
        }

        private int _montant;
        public int Montant
        {
            get => _montant;
            set => SetProperty(ref _montant, value);
        }

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public AddBudgetLineViewModel(BudgetLineService service, int budgetPrimitifId,  IEnumerable<Nommenclature> available, AuthService authService, AuditService auditService)
        {
            _service = service;
            _budgetPrimitifId = budgetPrimitifId;
            _authService = authService;

            foreach (var n in available) AvailableNoms.Add(n);

            CreateCommand = new RelayCommand(async _ => await CreateAsync(), _ => SelectedNomenclature != null && Montant >= 0);
            CancelCommand = new RelayCommand(_ => CloseDialog(false));
            _authService = authService;
            _auditService = auditService;
        }

        private void CloseDialog(bool result)
        {
            // On suppose que la fenêtre DataContext est liée ; on ferme depuis la VM via Message ou direct.
            // Simplicité : chercher la fenêtre et la fermer.
            var window = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = result;
                window.Close();
            }
        }

        private async Task CreateAsync()
        {
            if (SelectedNomenclature == null) return;

            // vérification supplémentaire : bloquer si la nomenclature a des enfants
            if (await _service.HasChildrenAsync(SelectedNomenclature.Id))
            {
                System.Windows.MessageBox.Show("La nomenclature sélectionnée possède des enfants. Choisissez une feuille.", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                BudgetLine bl =  await _service.CreateBudgetLineAsync(_budgetPrimitifId, SelectedNomenclature.Id, Montant);
                CloseDialog(true);

                var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                await _auditService.LogAsync(
                            "Nouvelle Prevision ",
                            $"{bl.Nommenclature.code()} montant : {bl.MontantPrevu} {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                            username);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la création : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
