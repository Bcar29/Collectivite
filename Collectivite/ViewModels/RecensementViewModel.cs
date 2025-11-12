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
    public class RecensementViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Recensement? _selectedRecensement;
        private bool _isDialogOpen;
        private Recensement _dialogRecensement;
        private bool _isEditMode;

        // Visibilité des colonnes
        private bool _showExercice = true;
        private bool _showCommune = true;
        private bool _showChapitre = true;
        private bool _showArticle = true;
        private bool _showParagraphe = true;
        private bool _showSousParagraphe = true;
        private bool _showIntitule = true;
        private bool _showTiers = true;
        private bool _showMontant = true;

        public RecensementViewModel()
        {
            _dialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(async _ => await OpenAddDialogAsync());
            OpenEditDialogCommand = new RelayCommand<Recensement>(r => OpenEditDialog(r));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Recensement>(async r => await DeleteAsync(r));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Recensement> Recensements { get; } = new();
        public ObservableCollection<BudgetLine> LignesBudgetaires { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Recensement? SelectedRecensement
        {
            get => _selectedRecensement;
            set => SetProperty(ref _selectedRecensement, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Recensement DialogRecensement
        {
            get => _dialogRecensement;
            set => SetProperty(ref _dialogRecensement, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier le recensement" : "Nouveau recensement";

        public double TotalRecense => Recensements.Sum(r => r.MontantRecense);

        #endregion

        #region Visibilité des colonnes

        public bool ShowExercice
        {
            get => _showExercice;
            set => SetProperty(ref _showExercice, value);
        }

        public bool ShowCommune
        {
            get => _showCommune;
            set => SetProperty(ref _showCommune, value);
        }

        public bool ShowChapitre
        {
            get => _showChapitre;
            set => SetProperty(ref _showChapitre, value);
        }

        public bool ShowArticle
        {
            get => _showArticle;
            set => SetProperty(ref _showArticle, value);
        }

        public bool ShowParagraphe
        {
            get => _showParagraphe;
            set => SetProperty(ref _showParagraphe, value);
        }

        public bool ShowSousParagraphe
        {
            get => _showSousParagraphe;
            set => SetProperty(ref _showSousParagraphe, value);
        }

        public bool ShowIntitule
        {
            get => _showIntitule;
            set => SetProperty(ref _showIntitule, value);
        }

        public bool ShowTiers
        {
            get => _showTiers;
            set => SetProperty(ref _showTiers, value);
        }

        public bool ShowMontant
        {
            get => _showMontant;
            set => SetProperty(ref _showMontant, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var recensementService = new RecensementService();

                // Charger tous les recensements
                var recensements = await recensementService.GetAllRecensementsAsync();

                Recensements.Clear();
                foreach (var r in recensements)
                {
                    Recensements.Add(r);
                }

                // ✅ Charger TOUTES les lignes budgétaires (fiscales ET non fiscales)
                var lignesBudgetaires = await recensementService.GetAllBudgetLinesAsync();

                LignesBudgetaires.Clear();
                foreach (var lb in lignesBudgetaires)
                {
                    LignesBudgetaires.Add(lb);
                }

                System.Diagnostics.Debug.WriteLine($"✅ {LignesBudgetaires.Count} lignes budgétaires chargées");

                // Charger les exercices
                using (var context = new AppDbContext())
                {
                    var exerciceService = new ExerciceService(context);
                    var exercices = await exerciceService.GetAllExerciceAsync();

                    Exercices.Clear();
                    foreach (var ex in exercices.Where(e => !e.EstCloture))
                    {
                        Exercices.Add(ex);
                    }
                }

                // Charger les communes
                using (var context = new AppDbContext())
                {
                    var communeService = new CommuneService(context);
                    var communes = await communeService.GetAllCommuneAsync();

                    Communes.Clear();
                    foreach (var c in communes)
                    {
                        Communes.Add(c);
                    }
                }

                // Charger les tiers
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }

                OnPropertyChanged(nameof(TotalRecense));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR : {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task OpenAddDialogAsync()
        {
            IsEditMode = false;

            DialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

            // ✅ Recharger toutes les lignes budgétaires
            IsLoading = true;
            try
            {
                var recensementService = new RecensementService();
                var lignesBudgetaires = await recensementService.GetAllBudgetLinesAsync();

                LignesBudgetaires.Clear();
                foreach (var lb in lignesBudgetaires)
                {
                    LignesBudgetaires.Add(lb);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Modal : {LignesBudgetaires.Count} lignes budgétaires chargées");

                if (LignesBudgetaires.Count == 0)
                {
                    MessageBox.Show(
                        "⚠️ Aucune ligne budgétaire n'a été trouvée.\n\n" +
                        "Veuillez d'abord créer des lignes budgétaires dans le module approprié.",
                        "Attention",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                OnPropertyChanged(nameof(LignesBudgetaires));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR : {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des lignes budgétaires : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                IsLoading = false;
            }

            IsDialogOpen = true;
        }

        private void OpenEditDialog(Recensement? recensement)
        {
            if (recensement == null) return;

            IsEditMode = true;

            DialogRecensement = new Recensement
            {
                Id = recensement.Id,
                BudgetLineId = recensement.BudgetLineId,
                ExerciceId = recensement.ExerciceId,
                CommuneId = recensement.CommuneId,
                TiersId = recensement.TiersId,
                MontantRecense = recensement.MontantRecense
            };

            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return DialogRecensement != null &&
                   DialogRecensement.BudgetLineId > 0 &&
                   DialogRecensement.ExerciceId > 0 &&
                   DialogRecensement.CommuneId > 0 &&
                   DialogRecensement.TiersId > 0 &&
                   DialogRecensement.MontantRecense > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                var recensementService = new RecensementService();

                if (IsEditMode)
                {
                    var (success, message) = await recensementService.UpdateRecensementAsync(DialogRecensement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
                    }
                }
                else
                {
                    var (success, message, recensement) = await recensementService.CreateRecensementAsync(DialogRecensement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
                    }
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

        private void CancelDialog()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteAsync(Recensement? recensement)
        {
            if (recensement == null) return;

            var result = MessageBox.Show(
                $"⚠️ Supprimer ce recensement ?\n\n" +
                $"Commune : {recensement.Commune?.Nom}\n" +
                $"Tiers : {recensement.Tiers?.Nom}\n" +
                $"Montant : {recensement.MontantRecense:N0} GNF\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var recensementService = new RecensementService();
                var (success, message) = await recensementService.DeleteRecensementAsync(recensement.Id);

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

        #endregion
    }
}