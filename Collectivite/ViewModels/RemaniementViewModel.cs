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
    /// <summary>
    /// ViewModel pour la gestion des remaniements budgétaires
    /// </summary>
    public class RemaniementViewModel : ViewModelBase
    {
        private readonly RemaniementService _remaniementService;
        private bool _isLoading;
        private Remaniement? _selectedRemaniement;
        private bool _isDialogOpen;
        private Remaniement _dialogRemaniement;
        private BudgetLine? _selectedBudgetLine;
        private int _selectedTabIndex;

        // Collection principale non filtrée
        private ObservableCollection<Remaniement> _allRemaniements = new();

        public RemaniementViewModel(RemaniementService remaniementService)
        {
            _remaniementService = remaniementService;
            _dialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(async _ => await OpenAddDialogAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Remaniement>(async rem => await DeleteAsync(rem));

            // Charger les données au démarrage
            LoadDataCommand.Execute(null);
        }

        #region Properties

        /// <summary>
        /// Collection visible dans le DataGrid (filtrée selon l'onglet)
        /// </summary>
        public ObservableCollection<Remaniement> Remaniements { get; } = new();

        /// <summary>
        /// Lignes budgétaires sans enfants pour la sélection
        /// </summary>
        public ObservableCollection<BudgetLine> BudgetLinesSansEnfants { get; } = new();

        /// <summary>
        /// Indicateur de chargement
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Remaniement sélectionné dans le DataGrid
        /// </summary>
        public Remaniement? SelectedRemaniement
        {
            get => _selectedRemaniement;
            set => SetProperty(ref _selectedRemaniement, value);
        }

        /// <summary>
        /// Indique si le dialog est ouvert
        /// </summary>
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        /// <summary>
        /// Remaniement en cours d'édition dans le dialog
        /// </summary>
        public Remaniement DialogRemaniement
        {
            get => _dialogRemaniement;
            set => SetProperty(ref _dialogRemaniement, value);
        }

        /// <summary>
        /// Ligne budgétaire sélectionnée dans le dialog
        /// </summary>
        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                SetProperty(ref _selectedBudgetLine, value);
                if (value != null && DialogRemaniement != null)
                {
                    DialogRemaniement.IdBudgetLine = value.Id;
                    OnPropertyChanged(nameof(DialogRemaniement));
                }
            }
        }

        /// <summary>
        /// Titre du dialog
        /// </summary>
        public string DialogTitle => "Nouveau Remaniement";

        /// <summary>
        /// Index de l'onglet sélectionné (0 = En Plus, 1 = En Moins)
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    ApplyFilter(); // Rafraîchit la liste filtrée
                    OnPropertyChanged(nameof(RemaniementsEnPlus));
                    OnPropertyChanged(nameof(RemaniementsEnMoins));
                }
            }
        }

        /// <summary>
        /// Remaniements de type "en_plus" uniquement
        /// </summary>
        public ObservableCollection<Remaniement> RemaniementsEnPlus
        {
            get
            {
                return new ObservableCollection<Remaniement>(
                    _allRemaniements.Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                );
            }
        }

        /// <summary>
        /// Remaniements de type "en_moins" uniquement
        /// </summary>
        public ObservableCollection<Remaniement> RemaniementsEnMoins
        {
            get
            {
                return new ObservableCollection<Remaniement>(
                    _allRemaniements.Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                );
            }
        }

        /// <summary>
        /// Nombre total de remaniements en plus
        /// </summary>
        public int CountRemaniementsEnPlus => RemaniementsEnPlus.Count;

        /// <summary>
        /// Nombre total de remaniements en moins
        /// </summary>
        public int CountRemaniementsEnMoins => RemaniementsEnMoins.Count;

        /// <summary>
        /// Montant total des remaniements en plus
        /// </summary>
        public decimal TotalRemaniementsEnPlus
        {
            get
            {
                return (decimal)RemaniementsEnPlus.Sum(r => r.Montant);
            }
        }

        /// <summary>
        /// Montant total des remaniements en moins
        /// </summary>
        public decimal TotalRemaniementsEnMoins
        {
            get
            {
                return (decimal)RemaniementsEnMoins.Sum(r => r.Montant);
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Charge tous les remaniements depuis la base de données
        /// </summary>
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var remaniements = await _remaniementService.GetAllRemaniementsAsync();

                _allRemaniements.Clear();
                foreach (var r in remaniements)
                {
                    _allRemaniements.Add(r);
                }

                // Rafraîchir toutes les propriétés calculées
                OnPropertyChanged(nameof(RemaniementsEnPlus));
                OnPropertyChanged(nameof(RemaniementsEnMoins));
                OnPropertyChanged(nameof(CountRemaniementsEnPlus));
                OnPropertyChanged(nameof(CountRemaniementsEnMoins));
                OnPropertyChanged(nameof(TotalRemaniementsEnPlus));
                OnPropertyChanged(nameof(TotalRemaniementsEnMoins));

                ApplyFilter(); // Appliquer le filtre selon l'onglet actif
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

        /// <summary>
        /// Applique un filtre sur la collection Remaniements selon l'onglet sélectionné
        /// 0 = Remaniements en PLUS
        /// 1 = Remaniements en MOINS
        /// </summary>
        private void ApplyFilter()
        {
            Remaniements.Clear();

            if (_allRemaniements.Count == 0)
                return;

            var filtered = _allRemaniements.AsEnumerable();

            switch (SelectedTabIndex)
            {
                case 0: // Onglet "Remaniements en PLUS"
                    filtered = filtered.Where(r => r.TypeRemaniement == TypeRemaniement.en_plus);
                    break;

                case 1: // Onglet "Remaniements en MOINS"
                    filtered = filtered.Where(r => r.TypeRemaniement == TypeRemaniement.en_moins);
                    break;

                default:
                    // Par défaut, afficher tous
                    break;
            }

            foreach (var r in filtered.OrderByDescending(r => r.Date))
            {
                Remaniements.Add(r);
            }
        }

        /// <summary>
        /// Ouvre le dialog pour créer un nouveau remaniement
        /// </summary>
        private async System.Threading.Tasks.Task OpenAddDialogAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les lignes budgétaires disponibles
                var lines = await _remaniementService.GetBudgetLinesSansEnfantsAsync();

                BudgetLinesSansEnfants.Clear();
                foreach (var line in lines)
                {
                    BudgetLinesSansEnfants.Add(line);
                }

                // Initialiser un nouveau remaniement
                DialogRemaniement = new Remaniement
                {
                    Date = DateTime.Now,
                    Montant = 0,
                    Motif = "",
                    TypeRemaniement = TypeRemaniement.en_plus
                };

                SelectedBudgetLine = null;

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dialog : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Vérifie si le remaniement peut être enregistré
        /// </summary>
        private bool CanSave()
        {
            return DialogRemaniement != null &&
                   DialogRemaniement.Montant > 0 &&
                   !string.IsNullOrWhiteSpace(DialogRemaniement.Motif) &&
                   DialogRemaniement.IdBudgetLine > 0 &&
                   SelectedBudgetLine != null;
        }

        /// <summary>
        /// Enregistre le nouveau remaniement
        /// </summary>
        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                // Confirmation avant l'enregistrement
                var typeText = DialogRemaniement.TypeRemaniement == TypeRemaniement.en_plus
                    ? "Augmentation (+)"
                    : "Diminution (-)";

                var confirmation = MessageBox.Show(
                    $"Confirmer le remaniement ?\n\n" +
                    $"Type : {typeText}\n" +
                    $"Montant : {DialogRemaniement.Montant:N0} GNF\n" +
                    $"Ligne budgétaire : {SelectedBudgetLine?.Nommenclature?.Intitule}\n" +
                    $"Motif : {DialogRemaniement.Motif}",
                    "Confirmation du remaniement",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                {
                    IsLoading = false;
                    return;
                }

                // Créer le remaniement
                var (success, message, remaniement) = await _remaniementService.CreateRemaniementAsync(
                    DialogRemaniement,
                    DialogRemaniement.TypeRemaniement
                );

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDialogOpen = false;
                    await LoadDataAsync(); // Recharger les données
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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

        /// <summary>
        /// Annule et ferme le dialog
        /// </summary>
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

        /// <summary>
        /// Supprime un remaniement
        /// </summary>
        private async System.Threading.Tasks.Task DeleteAsync(Remaniement? remaniement)
        {
            if (remaniement == null)
                return;

            var typeText = remaniement.TypeRemaniement == TypeRemaniement.en_plus
                ? "en PLUS"
                : "en MOINS";

            var result = MessageBox.Show(
                $"⚠️ Supprimer ce remaniement ?\n\n" +
                $"Type : {typeText}\n" +
                $"Montant : {remaniement.Montant:N0} GNF\n" +
                $"Motif : {remaniement.Motif}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;

            try
            {
                var (success, message) = await _remaniementService.DeleteRemaniementAsync(remaniement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync(); // Recharger les données
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