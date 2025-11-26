using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des remaniements budgétaires
    /// </summary>
    public class RemaniementViewModel : ViewModelBase
    {
        private bool _isLoading;
        private bool _isDialogOpen;
        private Remaniement _dialogRemaniement;
        private BudgetLine? _selectedBudgetLine;
        private int _selectedTabIndex;
        private bool _isDetailDialogOpen;
        private BudgetLine? _detailBudgetLine;

        private readonly List<BudgetLine> _allBudgetLines = new();

        public RemaniementViewModel()
        {
            _dialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus
            };

            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand<BudgetLine>(async line => await OpenAddDialogAsync(line), line => line != null);
            OpenDetailsCommand = new RelayCommand<BudgetLine>(async line => await OpenDetailsDialogAsync(line), line => line != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Remaniement>(async rem => await DeleteAsync(rem));
            CloseDetailsCommand = new RelayCommand(_ => CloseDetailsDialog());

            LoadDataCommand.Execute(null);
        }

        #region Collections exposées

        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();
        public ObservableCollection<Remaniement> DetailRemaniements { get; } = new();

        #endregion

        #region Propriétés bindables

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Remaniement DialogRemaniement
        {
            get => _dialogRemaniement;
            set => SetProperty(ref _dialogRemaniement, value);
        }

        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                SetProperty(ref _selectedBudgetLine, value);
                if (value != null)
                {
                    DialogRemaniement.IdBudgetLine = value.Id;
                    OnPropertyChanged(nameof(DialogRemaniement));
                }
                OnPropertyChanged(nameof(DialogTitle));
            }
        }

        public string DialogTitle
        {
            get
            {
                if (SelectedBudgetLine?.Nommenclature != null)
                {
                    return $"Remaniement - {SelectedBudgetLine.Nommenclature.Intitule}";
                }
                return "Nouveau remaniement";
            }
        }

        /// <summary>
        /// 0 = Recette-Fonctionnement, 1 = Recette-Investissement,
        /// 2 = Dépense-Fonctionnement, 3 = Dépense-Investissement
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    ApplyFilter();
                }
            }
        }

        public int TotalRemaniements => BudgetLines.Sum(bl => bl.Remaniements?.Count ?? 0);
        public decimal TotalVariation => BudgetLines.Sum(bl => bl.VariationTotale);

        public bool IsDetailDialogOpen
        {
            get => _isDetailDialogOpen;
            set => SetProperty(ref _isDetailDialogOpen, value);
        }

        public BudgetLine? DetailBudgetLine
        {
            get => _detailBudgetLine;
            set => SetProperty(ref _detailBudgetLine, value);
        }

        #endregion

        #region Commandes

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenDetailsCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseDetailsCommand { get; }

        #endregion

        #region Chargement / filtrage

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var service = new RemaniementService();
                var budgetLines = await service.GetBudgetLinesForValidatedBudgetAsync();

                _allBudgetLines.Clear();
                _allBudgetLines.AddRange(budgetLines);

                ApplyFilter();
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

        private void ApplyFilter()
        {
            BudgetLines.Clear();

            if (!_allBudgetLines.Any())
                return;

            IEnumerable<BudgetLine> filtered = _allBudgetLines;

            switch (SelectedTabIndex)
            {
                case 0:
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature?.Nature == NatureType.Recette &&
                        bl.Nommenclature?.Section == SectionType.Fonctionnement);
                    break;
                case 1:
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature?.Nature == NatureType.Recette &&
                        bl.Nommenclature?.Section == SectionType.Investissement);
                    break;
                case 2:
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature?.Nature == NatureType.Depense &&
                        bl.Nommenclature?.Section == SectionType.Fonctionnement);
                    break;
                case 3:
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature?.Nature == NatureType.Depense &&
                        bl.Nommenclature?.Section == SectionType.Investissement);
                    break;
                default:
                    break;
            }

            foreach (var line in filtered
                .OrderBy(bl => bl.Nommenclature?.Chapitre)
                .ThenBy(bl => bl.Nommenclature?.Article)
                .ThenBy(bl => bl.Nommenclature?.Paragraphe)
                .ThenBy(bl => bl.Nommenclature?.SousParagraphe))
            {
                BudgetLines.Add(line);
            }

            OnPropertyChanged(nameof(TotalRemaniements));
            OnPropertyChanged(nameof(TotalVariation));
        }

        #endregion

        #region Dialog Remaniement

        private System.Threading.Tasks.Task OpenAddDialogAsync(BudgetLine? budgetLine)
        {
            if (budgetLine == null)
                return System.Threading.Tasks.Task.CompletedTask;

            SelectedBudgetLine = budgetLine;
            DialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus,
                IdBudgetLine = budgetLine.Id
            };

            IsDialogOpen = true;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private bool CanSave()
        {
            return DialogRemaniement != null &&
                   SelectedBudgetLine != null &&
                   DialogRemaniement.Montant > 0 &&
                   !string.IsNullOrWhiteSpace(DialogRemaniement.Motif);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                if (SelectedBudgetLine == null)
                {
                    MessageBox.Show("Veuillez sélectionner une ligne budgétaire.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var service = new RemaniementService();
                var budgetLineId = DialogRemaniement.IdBudgetLine;

                var (success, message, _) = await service.CreateRemaniementAsync(
                    DialogRemaniement,
                    DialogRemaniement.TypeRemaniement);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    IsDialogOpen = false;
                    await LoadDataAsync();
                    await RefreshDetailsIfNeeded(budgetLineId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelDialog()
        {
            IsDialogOpen = false;
            DialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus
            };
            SelectedBudgetLine = null;
        }

        #endregion

        #region Détails remaniements

        private async System.Threading.Tasks.Task OpenDetailsDialogAsync(BudgetLine? budgetLine)
        {
            if (budgetLine == null)
                return;

            DetailBudgetLine = budgetLine;
            await LoadDetailRemaniementsAsync(budgetLine.Id);
            IsDetailDialogOpen = true;
        }

        private async System.Threading.Tasks.Task LoadDetailRemaniementsAsync(int budgetLineId)
        {
            try
            {
                var service = new RemaniementService();
                var details = await service.GetRemaniementsByBudgetLineAsync(budgetLineId);

                DetailRemaniements.Clear();
                foreach (var rem in details.OrderByDescending(r => r.Date))
                {
                    DetailRemaniements.Add(rem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des détails : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDetailsDialog()
        {
            IsDetailDialogOpen = false;
            DetailRemaniements.Clear();
            DetailBudgetLine = null;
        }

        #endregion

        #region Suppression

        private async System.Threading.Tasks.Task DeleteAsync(Remaniement? remaniement)
        {
            if (remaniement == null)
                return;

            var typeText = remaniement.TypeRemaniement == TypeRemaniement.en_plus ? "en PLUS" : "en MOINS";

            var confirm = MessageBox.Show(
                $"Supprimer ce remaniement ?\n\nType : {typeText}\nMontant : {remaniement.Montant:N0} GNF\nMotif : {remaniement.Motif}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsLoading = true;

            try
            {
                var service = new RemaniementService();
                var (success, message) = await service.DeleteRemaniementAsync(remaniement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                    await RefreshDetailsIfNeeded(remaniement.IdBudgetLine);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task RefreshDetailsIfNeeded(int budgetLineId)
        {
            if (IsDetailDialogOpen && DetailBudgetLine?.Id == budgetLineId)
            {
                await LoadDetailRemaniementsAsync(budgetLineId);
            }
        }

        #endregion
    }
}

