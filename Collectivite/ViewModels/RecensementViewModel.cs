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

        public RecensementViewModel()
        {
            _dialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Recensement>(r => OpenEditDialog(r));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Recensement>(async r => await DeleteAsync(r));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Recensement> Recensements { get; } = new();
        public ObservableCollection<BudgetLine> RecettesFiscales { get; } = new();
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
                var recensements = await recensementService.GetAllRecensementsAsync();

                Recensements.Clear();
                foreach (var r in recensements)
                {
                    Recensements.Add(r);
                }

                // ✅ Charger uniquement les recettes fiscales (71xx)
                var recettesFiscales = await recensementService.GetRecettesFiscalesBudgetLinesAsync();

                RecettesFiscales.Clear();
                foreach (var rf in recettesFiscales)
                {
                    RecettesFiscales.Add(rf);
                }

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

                // ✅ APRÈS (correct)
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
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddDialog()
        {
            IsEditMode = false;

            DialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

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