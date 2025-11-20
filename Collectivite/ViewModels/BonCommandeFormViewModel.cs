using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class BonCommandeFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande _bonCommande;
        private bool _isEditMode;
        private string _fichierName;
        private int? _bonCommandeId;

        public BonCommandeFormViewModel(int? bonCommandeId = null)
        {
            _bonCommandeId = bonCommandeId;
            _isEditMode = bonCommandeId.HasValue;

            _bonCommande = new BonCommande
            {
                DateCreation = DateTime.Now,
                Numero = $"BC-{DateTime.Now:yyyyMMdd}-001"
            };
            _fichierName = string.Empty;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            ChooseFileCommand = new RelayCommand(_ => ChooseFile());
            AddDetailCommand = new RelayCommand(_ => AddDetail());
            RemoveDetailCommand = new RelayCommand<DetailBonCommande>(d => RemoveDetail(d));
            RecalculerDetailCommand = new RelayCommand<DetailBonCommande>(d => RecalculerDetail(d));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Engagement> Engagements { get; } = new();
        public ObservableCollection<DetailBonCommande> Details { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BonCommande BonCommande
        {
            get => _bonCommande;
            set => SetProperty(ref _bonCommande, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string FichierName
        {
            get => _fichierName;
            set => SetProperty(ref _fichierName, value);
        }

        public string PageTitle => IsEditMode ? "Modifier le bon de commande" : "Nouveau bon de commande";

        public double MontantTotal => Details.Sum(d => d.Total);

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChooseFileCommand { get; }
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }
        public ICommand RecalculerDetailCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les engagements
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetAllEngagementsAsync();

                Engagements.Clear();
                foreach (var e in engagements)
                {
                    Engagements.Add(e);
                }

                // Si mode édition, charger le bon de commande
                if (_bonCommandeId.HasValue)
                {
                    var bonCommandeService = new BonCommandeService();
                    var bonCommande = await bonCommandeService.GetBonCommandeByIdAsync(_bonCommandeId.Value);

                    if (bonCommande != null)
                    {
                        BonCommande = new BonCommande
                        {
                            Id = bonCommande.Id,
                            Numero = bonCommande.Numero,
                            DateCreation = bonCommande.DateCreation,
                            EngagementId = bonCommande.EngagementId,
                            FichierJoin = bonCommande.FichierJoin
                        };

                        Details.Clear();
                        if (bonCommande.Details != null)
                        {
                            foreach (var detail in bonCommande.Details)
                            {
                                Details.Add(new DetailBonCommande
                                {
                                    Id = detail.Id,
                                    Designation = detail.Designation,
                                    Quantite = detail.Quantite,
                                    PrixUnitaire = detail.PrixUnitaire
                                });
                            }
                        }

                        FichierName = bonCommande.FichierJoin != null ? "Fichier existant" : string.Empty;
                    }
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

        private bool CanSave()
        {
            return BonCommande != null &&
                   !string.IsNullOrWhiteSpace(BonCommande.Numero) &&
                   BonCommande.EngagementId > 0 &&
                   Details.Count > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                var bonCommandeService = new BonCommandeService();
                var detailsList = Details.ToList();

                if (IsEditMode)
                {
                    var (success, message) = await bonCommandeService.UpdateBonCommandeAsync(
                        BonCommande, detailsList);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
                    }
                }
                else
                {
                    var (success, message, bonCommande) = await bonCommandeService.CreateBonCommandeAsync(
                        BonCommande, detailsList);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
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

        private void Cancel()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment annuler ? Les modifications non enregistrées seront perdues.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigateBack();
            }
        }

        private void NavigateBack()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.GoBack();
                }
            }
        }

        private void ChooseFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner un fichier",
                    Filter = "Tous les fichiers (*.*)|*.*|Documents PDF (*.pdf)|*.pdf",
                    FilterIndex = 2
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    if (fileBytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Le fichier est trop volumineux (max 5 MB).",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    BonCommande.FichierJoin = fileBytes;
                    FichierName = Path.GetFileName(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Ajouter une ligne de détail
        private void AddDetail()
        {
            var newDetail = new DetailBonCommande
            {
                Designation = "",
                Quantite = 1,
                PrixUnitaire = 0
            };

            Details.Add(newDetail);
            OnPropertyChanged(nameof(MontantTotal));
        }

        // ✅ Supprimer une ligne de détail
        private void RemoveDetail(DetailBonCommande? detail)
        {
            if (detail != null)
            {
                Details.Remove(detail);
                OnPropertyChanged(nameof(MontantTotal));
            }
        }

        // ✅ Recalculer le total d'une ligne
        private void RecalculerDetail(DetailBonCommande? detail)
        {
            if (detail != null)
            {
                OnPropertyChanged(nameof(MontantTotal));
            }
        }

        #endregion
    }
}