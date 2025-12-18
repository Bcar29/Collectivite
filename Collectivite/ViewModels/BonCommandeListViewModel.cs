using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class BonCommandeListViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande? _selectedBonCommande;
        private readonly ExerciceService _exerciceService;
        private readonly AuditService _auditService;
        private readonly AuthService _authService;
        private bool _isDisposed;
        public BonCommandeListViewModel(AuthService authService, AuditService auditService)
        {
            _exerciceService = ExerciceService.Instance;
            _authService = authService;
            _auditService = auditService;


            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;
            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddPageCommand = new RelayCommand(_ => OpenAddPage());
            GoForwardCommand = new RelayCommand(_ => NavigateFront());
            OpenEditPageCommand = new RelayCommand<BonCommande>(bc => OpenEditPage(bc));
            OpenDetailsPageCommand = new RelayCommand<BonCommande>(bc => OpenDetailsPage(bc));
            DeleteCommand = new RelayCommand<BonCommande>(async bc => await DeleteAsync(bc));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Permissions

        public bool CanViewBonCommande => SessionManager.HasPermission("BonCommande.View");
        public bool CanCreateBonCommande => SessionManager.HasPermission("BonCommande.Create");
        public bool CanEditBonCommande => SessionManager.HasPermission("BonCommande.Edit");
        public bool CanDeleteBonCommande => SessionManager.HasPermission("BonCommande.Delete");

        #endregion

        #region Properties

        public ObservableCollection<BonCommande> BonCommandes { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BonCommande? SelectedBonCommande
        {
            get => _selectedBonCommande;
            set => SetProperty(ref _selectedBonCommande, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddPageCommand { get; }
        public ICommand OpenEditPageCommand { get; }
        public ICommand OpenDetailsPageCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand GoForwardCommand { get; }

        #endregion

        #region Methods

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                //recharge les expression de besoin 
                await LoadDataAsync();

            });
        }
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                if (!CanViewBonCommande)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les bons de commande.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BonCommandes.Clear();
                    return;
                }

                var service = new BonCommandeService();
                var bonCommandes = await service.GetAllBonCommandesAsync();

                BonCommandes.Clear();
                foreach (var bc in bonCommandes)
                {
                    BonCommandes.Add(bc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddPage()
        {
            if (!CanCreateBonCommande)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des bons de commande.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeFormPage(_authService));
        }

        private void OpenEditPage(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;
            if (!CanEditBonCommande)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier des bons de commande.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeFormPage(_authService,bonCommande.Id));
        }

        private void OpenDetailsPage(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;
            if (!CanViewBonCommande)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les bons de commande.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeDetailsPage(bonCommande.Id));
        }

        private async System.Threading.Tasks.Task DeleteAsync(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;
            if (!CanDeleteBonCommande)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de supprimer des bons de commande.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ Supprimer le bon de commande '{bonCommande.Numero}' ?\n\n" +
                $"Cette action supprimera également tous les détails associés.\n" +
                $"Les engagements liés seront dissociés.\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var service = new BonCommandeService();
                var (success, message) = await service.DeleteBonCommandeAsync(bonCommande.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateFront()
        {
            NavigationService.Instance.GoForward();
        }
        /// <summary>
        /// Nettoyer les ressources et se désabonner des événements
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _exerciceService.ExerciceChanged -= OnExerciceChanged;
                _isDisposed = true;
            }
        }

        #endregion
    }
}