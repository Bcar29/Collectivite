using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la liste des engagements
    /// </summary>
    public class EngagementViewModel : ViewModelBase, IDisposable
    {
        // private readonly string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";
        private bool _isLoading;
        private Engagement? _selectedEngagement;
        private string _searchText;
        private int? _selectedExerciceId;
        private bool _isDisposed;
        private readonly ExerciceService _exerciceService;


        public EngagementViewModel()
        {
            _searchText = string.Empty;
            _exerciceService = ExerciceService.Instance;
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SearchCommand = new RelayCommand(async _ => await SearchAsync());
            DeleteCommand = new RelayCommand<Engagement>(async engagement => await DeleteAsync(engagement));
            ShowStatistiquesCommand = new RelayCommand(async _ => await ShowStatistiquesAsync());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Permissions

        public bool CanViewEngagement => SessionManager.HasPermission("Engagement.View");
        public bool CanCreateEngagement => SessionManager.HasPermission("Engagement.Create");
        public bool CanEditEngagement => SessionManager.HasPermission("Engagement.Edit");
        public bool CanDeleteEngagement => SessionManager.HasPermission("Engagement.Delete");

        #endregion

        #region Properties

        public ObservableCollection<Engagement> Engagements { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Engagement? SelectedEngagement
        {
            get => _selectedEngagement;
            set => SetProperty(ref _selectedEngagement, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public int? SelectedExerciceId
        {
            get => _selectedExerciceId;
            set
            {
                if (SetProperty(ref _selectedExerciceId, value))
                {
                    LoadDataCommand.Execute(null);
                }
            }
        }

        public int TotalEngagements => Engagements.Count;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ShowStatistiquesCommand { get; }

        #endregion

        #region Methods
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDataAsync();
            });
        }
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                if (!CanViewEngagement)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les engagements.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Engagements.Clear();
                    IsLoading = false;
                    return;
                }

                var service = new EngagementService();
                List<Engagement> engagements;

                if (SelectedExerciceId.HasValue)
                {
                    engagements = await service.GetEngagementsByExerciceAsync(SelectedExerciceId.Value);
                }
                else
                {
                    engagements = await service.GetAllEngagementsAsync();
                }

                Engagements.Clear();
                foreach (var engagement in engagements)
                {
                    Engagements.Add(engagement);
                }

                OnPropertyChanged(nameof(TotalEngagements));
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

        private async System.Threading.Tasks.Task SearchAsync()
        {
            IsLoading = true;

            try
            {
                if (!CanViewEngagement)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les engagements.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Engagements.Clear();
                    IsLoading = false;
                    return;
                }

                var service = new EngagementService();
                var results = await service.SearchEngagementsAsync(SearchText);

                Engagements.Clear();
                foreach (var engagement in results)
                {
                    Engagements.Add(engagement);
                }

                OnPropertyChanged(nameof(TotalEngagements));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task DeleteAsync(Engagement? engagement)
        {
            if (engagement == null) return;

            if (!CanDeleteEngagement)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de supprimer les engagements.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cet engagement ?\n\n" +
                $"Objet : {engagement.Objet}\n" +
                $"Montant : {engagement.MontantEngagement:N0} GNF\n" +
                $"Tiers : {engagement.Tiers?.Nom}\n\n" +
                "Cette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var service = new EngagementService();
                var (success, message) = await service.DeleteEngagementAsync(engagement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                }

                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task ShowStatistiquesAsync()
        {
            try
            {
                var service = new EngagementService();
                var stats = await service.GetStatistiquesAsync(SelectedExerciceId);

                var message = $"📊 STATISTIQUES DES ENGAGEMENTS\n\n" +
                             $"Total d'engagements : {stats.TotalEngagements}\n" +
                             $"Montant total : {stats.MontantTotal:N0} GNF\n" +
                             $"Montant moyen : {stats.MontantMoyen:N0} GNF\n\n" +
                             $"Top 10 des tiers :\n";

                foreach (var item in stats.Top10Tiers)
                {
                    message += $"• {item.Key} : {item.Value:N0} GNF\n";
                }

                MessageBox.Show(message, "Statistiques",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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