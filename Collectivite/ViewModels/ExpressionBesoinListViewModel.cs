using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class ExpressionBesoinListViewModel : ViewModelBase, IDisposable
    {
        private bool _isLoading;
        private ExpressionBesoin? _selectedExpressionBesoin;
        private readonly ExerciceService _exerciceService;
        private readonly AuditService _auditService;
        private readonly AuthService _authService;
        private bool _isDisposed;

        public ExpressionBesoinListViewModel(AuthService authService, AuditService auditService)
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
            OpenEditPageCommand = new RelayCommand<ExpressionBesoin>(eb => OpenEditPage(eb));
            OpenDetailsPageCommand = new RelayCommand<ExpressionBesoin>(eb => OpenDetailsPage(eb));
            DeleteCommand = new RelayCommand<ExpressionBesoin>(async eb => await DeleteAsync(eb));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Permissions

        public bool CanViewExpressionBesoin => SessionManager.HasPermission("ExpressionBesoin.View");
        public bool CanCreateExpressionBesoin => SessionManager.HasPermission("ExpressionBesoin.Create");
        public bool CanEditExpressionBesoin => SessionManager.HasPermission("ExpressionBesoin.Edit");
        public bool CanDeleteExpressionBesoin => SessionManager.HasPermission("ExpressionBesoin.Delete");

        #endregion

        #region Properties

        public ObservableCollection<ExpressionBesoin> ExpressionBesoins { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ExpressionBesoin? SelectedExpressionBesoin
        {
            get => _selectedExpressionBesoin;
            set => SetProperty(ref _selectedExpressionBesoin, value);
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
                if (!CanViewExpressionBesoin)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les expressions de besoin.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ExpressionBesoins.Clear();
                    return;
                }

                var service = new ExpressionBesoinService();
                var expressionBesoins = await service.GetAllExpressionBesoinsAsync();

                ExpressionBesoins.Clear();
                foreach (var eb in expressionBesoins)
                {
                    ExpressionBesoins.Add(eb);
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
            if (!CanCreateExpressionBesoin)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des expressions de besoin.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.ExpressionBesoinFormPage(_authService));
        }

        private void OpenEditPage(ExpressionBesoin? expressionBesoin)
        {
            if (expressionBesoin == null) return;
            if (!CanEditExpressionBesoin)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier des expressions de besoin.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.ExpressionBesoinFormPage(_authService, expressionBesoin.Id));
        }

        private void OpenDetailsPage(ExpressionBesoin? expressionBesoin)
        {
            if (expressionBesoin == null) return;
            if (!CanViewExpressionBesoin)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les expressions de besoin.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.ExpressionBesoinDetailsPage(expressionBesoin.Id));
        }

        private async System.Threading.Tasks.Task DeleteAsync(ExpressionBesoin? expressionBesoin)
        {
            if (expressionBesoin == null) return;
            if (!CanDeleteExpressionBesoin)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de supprimer des expressions de besoin.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ Supprimer l'expression de besoin '{expressionBesoin.Numero}' ?\n\n" +
                $"Cette action supprimera également tous les détails associés.\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var service = new ExpressionBesoinService();
                var (success, message) = await service.DeleteExpressionBesoinAsync(expressionBesoin.Id);

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