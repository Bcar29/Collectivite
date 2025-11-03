using Collectivite.Models;
using Collectivite.Services;
using System;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class CommuneViewModel : ViewModelBase
    {
        private readonly CommuneService _communeService;
        private bool _isLoading;
        private Commune? _selectedCommune;
        private bool _isDialogOpen;
        private Commune _dialogCommune;
        private bool _isEditMode;

        public CommuneViewModel(CommuneService commune)
        {
            _communeService = commune;
            _dialogCommune = new Commune
            {
                Nom = "",
                DateCreation = DateOnly.FromDateTime(DateTime.Now)
            };

            //commandes
            LoadCommuneCommand = new RelayCommand(async _ => await LoadCommuneAsync());
            OppenAddCommuneCommand = new RelayCommand(_ => OpenAddCommune());
            OppenEditCommuneCommand = new RelayCommand<Commune>(commune => OpenEditCommune(commune));
            SaveCommuneCommand = new RelayCommand(async _ => await SaveCommuneAsync(), _ => CanSaveCommune());
            CancelCommuneCommand = new RelayCommand(_ => CancelCommune());
            DeleteCommuneCommand = new RelayCommand<Commune>(async commune => await DeleteCommuneAsync(commune));

            // ✅ NOUVELLE COMMANDE : Ouvrir les détails
            OpenDetailCommuneCommand = new RelayCommand<Commune>(commune => OpenDetailCommune(commune));

            //charger les données au démarrage
            LoadCommuneCommand.Execute(null);
        }

        #region Properties 
        public ObservableCollection<Commune> Communes { get; } = [];

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Commune? SelectedCommune
        {
            get => _selectedCommune;
            set => SetProperty(ref _selectedCommune, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Commune DialogCommune
        {
            get => _dialogCommune;
            set => SetProperty(ref _dialogCommune, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier la commune" : "Ajouter une commune";

        public DateTime DialogCommuneDateCreation
        {
            get => DialogCommune.DateCreation.ToDateTime(TimeOnly.MinValue);
            set
            {
                DialogCommune.DateCreation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands
        public ICommand LoadCommuneCommand { get; }
        public ICommand OppenAddCommuneCommand { get; }
        public ICommand OppenEditCommuneCommand { get; }
        public ICommand SaveCommuneCommand { get; }
        public ICommand CancelCommuneCommand { get; }
        public ICommand DeleteCommuneCommand { get; }

        // ✅ NOUVELLE COMMANDE
        public ICommand OpenDetailCommuneCommand { get; }
        #endregion

        #region Methods

        public async System.Threading.Tasks.Task LoadCommuneAsync()
        {
            IsLoading = true;
            try
            {
                var communes = await _communeService.GetAllCommuneAsync();

                Communes.Clear();

                foreach (var commune in communes)
                {
                    Communes.Add(commune);
                }

                System.Diagnostics.Debug.WriteLine($"Communes chargées : {Communes.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des communes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddCommune()
        {
            IsEditMode = false;
            DialogCommune = new Commune
            {
                Nom = "",
                DateCreation = DateOnly.FromDateTime(DateTime.Now),
                DistanceCapitale = 0,
                DistanceChefLieuProvince = 0,
                DistanceChefLieuRegion = 0
            };

            OnPropertyChanged(nameof(DialogCommuneDateCreation));

            IsDialogOpen = true;
        }

        private void OpenEditCommune(Commune? commune)
        {
            if (commune == null)
                return;

            IsEditMode = true;
            DialogCommune = new Commune
            {
                Id = commune.Id,
                Nom = commune.Nom,
                DateCreation = commune.DateCreation,
                DistanceCapitale = commune.DistanceCapitale,
                DistanceChefLieuProvince = commune.DistanceChefLieuProvince,
                DistanceChefLieuRegion = commune.DistanceChefLieuRegion
            };

            OnPropertyChanged(nameof(DialogCommuneDateCreation));

            IsDialogOpen = true;
        }

        // ══════════════════════════════════════════════════════════
        // ✅ NOUVELLE MÉTHODE : OUVRIR LES DÉTAILS D'UNE COMMUNE
        // ══════════════════════════════════════════════════════════
        private static void OpenDetailCommune(Commune? commune)
        {
            if (commune == null) return;

            try
            {
                // Créer la page de détails avec le filtre de commune
                var detailPage = new Views.Pages.DetailCommunePage(commune.Id);

                // Naviguer vers la page
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow?.MainContentFrame != null)
                {
                    mainWindow.MainContentFrame.Navigate(detailPage);
                }
                else
                {
                    MessageBox.Show("Impossible de naviguer vers la page de détails.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des détails : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveCommune()
        {
            return !string.IsNullOrWhiteSpace(DialogCommune.Nom);
        }

        private async System.Threading.Tasks.Task SaveCommuneAsync()
        {
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _communeService.UpdateCommuneAsync(DialogCommune);
                    if (success)
                    {
                        MessageBox.Show("Commune mise à jour avec succès",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCommuneAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var (success, message, _) = await _communeService.CreateCommuneAsync(DialogCommune);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCommuneAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement de la commune : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelCommune()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteCommuneAsync(Commune? commune)
        {
            if (commune == null)
                return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer la commune {commune.Nom} ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _communeService.DeleteCommuneAsync(commune.Id);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCommuneAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsLoading = false;
            }
        }
        #endregion
    }
}