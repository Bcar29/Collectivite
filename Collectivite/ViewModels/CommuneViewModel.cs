using Collectivite.Models;
using Collectivite.Services;
using System;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace Collectivite.ViewModels
{
    public class CommuneViewModel : ViewModelBase
    {
        private readonly CommuneService _communeService;
        private readonly CommunePdfService _communePdfService;
        private bool _isLoading;
        private Commune? _selectedCommune;
        private bool _isDialogOpen;
        private Commune _dialogCommune;
        private bool _isEditMode;
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";

        // ✅ NOUVEAU : Pour gérer le TypeCommune
        private TypeCommuneItem? _selectedTypeCommuneItem;

        public CommuneViewModel(CommuneService commune)
        {
            _communeService = commune;
            _communePdfService = new CommunePdfService();
            _dialogCommune = new Commune
            {
                Nom = "",
                DateCreation = DateOnly.FromDateTime(DateTime.Now)
            };

            // ✅ NOUVEAU : Initialiser la liste des types de commune
            TypesCommuneDisponibles = new List<TypeCommuneItem>
            {
                new TypeCommuneItem(Commune.TypeCommune.URBAINE, "Commune Urbaine"),
                new TypeCommuneItem(Commune.TypeCommune.RURALE, "Commune Rurale")
            };

            // ✅ NOUVEAU : Sélectionner URBAINE par défaut
            _selectedTypeCommuneItem = TypesCommuneDisponibles.First();

            //commandes
            LoadCommuneCommand = new RelayCommand(async _ => await LoadCommuneAsync());
            OppenAddCommuneCommand = new RelayCommand(_ => OpenAddCommune());
            OppenEditCommuneCommand = new RelayCommand<Commune>(commune => OpenEditCommune(commune));
            SaveCommuneCommand = new RelayCommand(async _ => await SaveCommuneAsync(), _ => CanSaveCommune());
            CancelCommuneCommand = new RelayCommand(_ => CancelCommune());
            DeleteCommuneCommand = new RelayCommand<Commune>(async commune => await DeleteCommuneAsync(commune));

            // ✅ NOUVELLE COMMANDE : Ouvrir les détails
            OpenDetailCommuneCommand = new RelayCommand<Commune>(commune => OpenDetailCommune(commune));

            // ✅ NOUVELLES COMMANDES : Export PDF
            ExportPdfCommand = new RelayCommand<Commune>(async commune => await ExportCommuneToPdfAsync(commune));
            ExportPdfWithPrintCommand = new RelayCommand<Commune>(async commune => await ExportAndPrintCommuneAsync(commune));

            //charger les données au démarrage
            LoadCommuneCommand.Execute(null);
        }

        #region Properties 
        public ObservableCollection<Commune> Communes { get; } = new ObservableCollection<Commune>();

        // Permissions dynamiques
        public bool CanViewCommune => SessionManager.HasPermission("Commune.View");
        public bool CanCreateCommune => SessionManager.HasPermission("Commune.Create");
        public bool CanEditCommune => SessionManager.HasPermission("Commune.Edit");
        public bool CanDeleteCommune => SessionManager.HasPermission("Commune.Delete");

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

        // ══════════════════════════════════════════════════════════
        // ✅ NOUVELLES PROPRIÉTÉS : GESTION DU TYPE DE COMMUNE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Liste de tous les types de commune disponibles avec libellés
        /// </summary>
        public List<TypeCommuneItem> TypesCommuneDisponibles { get; }

        /// <summary>
        /// Type de commune sélectionné dans le ComboBox
        /// </summary>
        public TypeCommuneItem? SelectedTypeCommuneItem
        {
            get => _selectedTypeCommuneItem;
            set
            {
                if (SetProperty(ref _selectedTypeCommuneItem, value))
                {
                    // Mettre à jour automatiquement le DialogCommune
                    if (DialogCommune != null && value != null)
                    {
                        DialogCommune.CommuneType = value.Valeur;
                        OnPropertyChanged(nameof(DialogCommune));
                    }
                }
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

        // ✅ NOUVELLES COMMANDES : Export PDF
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportPdfWithPrintCommand { get; }
        #endregion

        #region Methods

        public async System.Threading.Tasks.Task LoadCommuneAsync()
        {
            if (!CanViewCommune)
            {
                NotificationService.ShowWarning("Accès refusé : vous n'avez pas la permission de consulter les communes.");
                Communes.Clear();
                return;
            }

            IsLoading = true;
            try
            {
                var communes = await _communeService.GetAllCommuneAsync();

                Communes.Clear();

                foreach (var commune in communes)
                {
                    Communes.Add(commune);
                }

            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement des communes dans viewmodèle: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddCommune()
        {
            if (!CanCreateCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Create");
                return;
            }

            IsEditMode = false;
            DialogCommune = new Commune
            {
                Nom = "",
                DateCreation = DateOnly.FromDateTime(DateTime.Now),
                DistanceCapitale = 0,
                DistanceChefLieuProvince = 0,
                DistanceChefLieuRegion = 0,
                CommuneType = Commune.TypeCommune.URBAINE // ✅ Valeur par défaut
            };

            // ✅ NOUVEAU : Sélectionner URBAINE par défaut dans le ComboBox
            SelectedTypeCommuneItem = TypesCommuneDisponibles
                .FirstOrDefault(t => t.Valeur == Commune.TypeCommune.URBAINE);

            OnPropertyChanged(nameof(DialogCommuneDateCreation));

            IsDialogOpen = true;
        }

        private void OpenEditCommune(Commune? commune)
        {
            if (commune == null)
                return;

            if (!CanEditCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Edit");
                return;
            }

            IsEditMode = true;
            DialogCommune = new Commune
            {
                Id = commune.Id,
                Nom = commune.Nom,
                Region = commune.Region,
                Prefecture = commune.Prefecture,
                DateCreation = commune.DateCreation,
                DistanceCapitale = commune.DistanceCapitale,
                DistanceChefLieuProvince = commune.DistanceChefLieuProvince,
                DistanceChefLieuRegion = commune.DistanceChefLieuRegion,
                CommuneType = commune.CommuneType // ✅ Récupérer le type existant
            };

            // ✅ NOUVEAU : Sélectionner le type correspondant dans le ComboBox
            SelectedTypeCommuneItem = TypesCommuneDisponibles
                .FirstOrDefault(t => t.Valeur == commune.CommuneType);

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
                    NotificationService.ShowError("Impossible de naviguer vers la page de détails.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'ouverture des détails : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        // ✅ NOUVELLES MÉTHODES : EXPORT PDF
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Exporte une commune en PDF (enregistrement uniquement)
        /// </summary>
        private async System.Threading.Tasks.Task ExportCommuneToPdfAsync(Commune? commune)
        {
            if (commune == null) return;

            IsLoading = true;
            try
            {
                // Charger les détails de la commune
                var context = new AppDbContext();
                var detailCommuneService = new DetailCommuneService(context);
                var detailCommune = await detailCommuneService.GetDetailCommuneByIdAsync(commune.Id);

                // Générer le PDF
                string pdfPath = _communePdfService.GenerateRapportCommune(commune, detailCommune);

                NotificationService.ShowSuccess($"Le rapport PDF a été généré avec succès !\n\nEmplacement : {pdfPath}");

                // Demander si l'utilisateur veut ouvrir le fichier
                var result = MessageBox.Show(
                    "Voulez-vous ouvrir le fichier PDF maintenant ?",
                    "Ouvrir le PDF",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _communePdfService.OpenPdf(pdfPath);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'export PDF : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Exporte une commune en PDF et ouvre directement pour impression
        /// </summary>
        private async System.Threading.Tasks.Task ExportAndPrintCommuneAsync(Commune? commune)
        {
            if (commune == null) return;

            IsLoading = true;
            try
            {
                // Charger les détails de la commune
                var context = new AppDbContext();
                var detailCommuneService = new DetailCommuneService(context);
                var detailCommune = await detailCommuneService.GetDetailCommuneByIdAsync(commune.Id);

                // Générer le PDF
                string pdfPath = _communePdfService.GenerateRapportCommune(commune, detailCommune);

                // Ouvrir automatiquement le PDF pour impression
                _communePdfService.OpenPdf(pdfPath);

                NotificationService.ShowSuccess("Le rapport PDF a été généré et ouvert pour impression.");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'export PDF : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSaveCommune()
        {
            return !string.IsNullOrWhiteSpace(DialogCommune.Nom) && !string.IsNullOrWhiteSpace(DialogCommune.Region);
        }

        private async System.Threading.Tasks.Task SaveCommuneAsync()
        {
            if (IsEditMode && !CanEditCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Edit");
                return;
            }

            if (!IsEditMode && !CanCreateCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Create");
                return;
            }

            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _communeService.UpdateCommuneAsync(DialogCommune);
                    if (success)
                    {
                        NotificationService.ShowSuccess("Commune mise à jour avec succès");
                        IsDialogOpen = false;
                        await LoadCommuneAsync();
                    }
                    else
                    {
                        NotificationService.ShowError(message);
                    }
                }
                else
                {
                    var (success, message, _) = await _communeService.CreateCommuneAsync(DialogCommune);
                    if (success)
                    {
                        NotificationService.ShowSuccess(message);
                        IsDialogOpen = false;
                        await LoadCommuneAsync();
                    }
                    else
                    {
                        NotificationService.ShowError(message);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'enregistrement de la commune : {ex.Message}");
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

            if (!CanDeleteCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Delete");
                return;
            }

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
                    NotificationService.ShowSuccess(message);
                    await LoadCommuneAsync();
                }
                else
                {
                    NotificationService.ShowError(message);
                }

                IsLoading = false;
            }
        }
        #endregion
    }

    // ══════════════════════════════════════════════════════════
    // ✅ NOUVELLE CLASSE : ITEM POUR LE COMBOBOX
    // ══════════════════════════════════════════════════════════
    /// <summary>
    /// Classe pour afficher les types de commune avec un libellé personnalisé
    /// </summary>
    public class TypeCommuneItem
    {
        public Commune.TypeCommune Valeur { get; set; }
        public string Libelle { get; set; } = string.Empty;

        public TypeCommuneItem(Commune.TypeCommune valeur, string libelle)
        {
            Valeur = valeur;
            Libelle = libelle;
        }
    }
}