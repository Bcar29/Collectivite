using Collectivite.Models;
using Collectivite.Services;
using System;
using Collectivite.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// Une seule commune existe par instance de l'application : cette page est donc
    /// un formulaire de profil unique (chargement/édition/sauvegarde), pas une liste CRUD.
    /// </summary>
    public class CommuneViewModel : ViewModelBase
    {
        private readonly CommuneService _communeService;
        private readonly CommunePdfService _communePdfService;
        private bool _isLoading;
        private Commune _currentCommune;
        private TypeCommuneItem? _selectedTypeCommuneItem;
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";

        public CommuneViewModel(CommuneService commune)
        {
            _communeService = commune;
            _communePdfService = new CommunePdfService();
            _currentCommune = new Commune
            {
                Nom = "",
                DateCreation = DateOnly.FromDateTime(DateTime.Now),
                CommuneType = Commune.TypeCommune.URBAINE
            };

            TypesCommuneDisponibles = new List<TypeCommuneItem>
            {
                new TypeCommuneItem(Commune.TypeCommune.URBAINE, "Commune Urbaine"),
                new TypeCommuneItem(Commune.TypeCommune.RURALE, "Commune Rurale")
            };
            _selectedTypeCommuneItem = TypesCommuneDisponibles.First();

            LoadCommuneCommand = new RelayCommand(async _ => await LoadCommuneAsync());
            SaveCommuneCommand = new RelayCommand(async _ => await SaveCommuneAsync(), _ => CanSaveCommune());
            CancelCommuneCommand = new RelayCommand(async _ => await LoadCommuneAsync());
            OpenDetailCommuneCommand = new RelayCommand(_ => OpenDetailCommune());
            ExportPdfCommand = new RelayCommand(async _ => await ExportCommuneToPdfAsync());
            ExportPdfWithPrintCommand = new RelayCommand(async _ => await ExportAndPrintCommuneAsync());

            LoadCommuneCommand.Execute(null);
        }

        #region Properties

        public bool CanViewCommune => SessionManager.HasPermission("Commune.View");
        public bool CanCreateCommune => SessionManager.HasPermission("Commune.Create");
        public bool CanEditCommune => SessionManager.HasPermission("Commune.Edit");

        /// <summary>
        /// L'utilisateur peut modifier le formulaire : création si aucune commune n'existe
        /// encore, édition sinon.
        /// </summary>
        public bool CanEditForm => IsNewCommune ? CanCreateCommune : CanEditCommune;

        public bool IsNewCommune => CurrentCommune.Id == 0;

        public string PageSubtitle => IsNewCommune
            ? "Aucune commune enregistrée : renseignez le profil ci-dessous."
            : "Profil de la commune gérée par cette application.";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Commune CurrentCommune
        {
            get => _currentCommune;
            set
            {
                if (SetProperty(ref _currentCommune, value))
                {
                    OnPropertyChanged(nameof(IsNewCommune));
                    OnPropertyChanged(nameof(PageSubtitle));
                    OnPropertyChanged(nameof(CanEditForm));
                    OnPropertyChanged(nameof(CurrentCommuneDateCreation));
                }
            }
        }

        public DateTime CurrentCommuneDateCreation
        {
            get => CurrentCommune.DateCreation.ToDateTime(TimeOnly.MinValue);
            set
            {
                CurrentCommune.DateCreation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public List<TypeCommuneItem> TypesCommuneDisponibles { get; }

        public TypeCommuneItem? SelectedTypeCommuneItem
        {
            get => _selectedTypeCommuneItem;
            set
            {
                if (SetProperty(ref _selectedTypeCommuneItem, value) && value != null)
                {
                    CurrentCommune.CommuneType = value.Valeur;
                    OnPropertyChanged(nameof(CurrentCommune));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand LoadCommuneCommand { get; }
        public ICommand SaveCommuneCommand { get; }
        public ICommand CancelCommuneCommand { get; }
        public ICommand OpenDetailCommuneCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportPdfWithPrintCommand { get; }

        #endregion

        #region Methods

        public async System.Threading.Tasks.Task LoadCommuneAsync()
        {
            if (!CanViewCommune)
            {
                NotificationService.ShowWarning("Accès refusé : vous n'avez pas la permission de consulter la commune.");
                return;
            }

            IsLoading = true;
            try
            {
                var communes = await _communeService.GetAllCommuneAsync();
                var existing = communes.FirstOrDefault();

                CurrentCommune = existing ?? new Commune
                {
                    Nom = "",
                    DateCreation = DateOnly.FromDateTime(DateTime.Now),
                    CommuneType = Commune.TypeCommune.URBAINE
                };

                SelectedTypeCommuneItem = TypesCommuneDisponibles
                    .FirstOrDefault(t => t.Valeur == CurrentCommune.CommuneType) ?? TypesCommuneDisponibles.First();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement de la commune : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenDetailCommune()
        {
            if (IsNewCommune)
            {
                NotificationService.ShowWarning("Veuillez d'abord enregistrer le profil de la commune.");
                return;
            }

            try
            {
                var detailPage = new Views.Pages.DetailCommunePage(CurrentCommune.Id);
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

        private async System.Threading.Tasks.Task ExportCommuneToPdfAsync()
        {
            if (IsNewCommune) return;

            IsLoading = true;
            try
            {
                var context = new AppDbContext();
                var detailCommuneService = new DetailCommuneService(context);
                var detailCommune = await detailCommuneService.GetDetailCommuneByIdAsync(CurrentCommune.Id);

                string pdfPath = _communePdfService.GenerateRapportCommune(CurrentCommune, detailCommune);

                NotificationService.ShowSuccess($"Le rapport PDF a été généré avec succès !\n\nEmplacement : {pdfPath}");

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

        private async System.Threading.Tasks.Task ExportAndPrintCommuneAsync()
        {
            if (IsNewCommune) return;

            IsLoading = true;
            try
            {
                var context = new AppDbContext();
                var detailCommuneService = new DetailCommuneService(context);
                var detailCommune = await detailCommuneService.GetDetailCommuneByIdAsync(CurrentCommune.Id);

                string pdfPath = _communePdfService.GenerateRapportCommune(CurrentCommune, detailCommune);

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
            return CanEditForm
                && !string.IsNullOrWhiteSpace(CurrentCommune.Nom)
                && !string.IsNullOrWhiteSpace(CurrentCommune.Region);
        }

        private async System.Threading.Tasks.Task SaveCommuneAsync()
        {
            if (IsNewCommune && !CanCreateCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Create");
                return;
            }

            if (!IsNewCommune && !CanEditCommune)
            {
                NotificationService.ShowWarning(_accessDeniedMessage + "\nPermission requise : Commune.Edit");
                return;
            }

            IsLoading = true;

            try
            {
                if (IsNewCommune)
                {
                    var (success, message, _) = await _communeService.CreateCommuneAsync(CurrentCommune);
                    if (success)
                    {
                        NotificationService.ShowSuccess(message);
                        await LoadCommuneAsync();
                    }
                    else
                    {
                        NotificationService.ShowError(message);
                    }
                }
                else
                {
                    var (success, message) = await _communeService.UpdateCommuneAsync(CurrentCommune);
                    if (success)
                    {
                        NotificationService.ShowSuccess("Commune mise à jour avec succès");
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

        #endregion
    }

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
