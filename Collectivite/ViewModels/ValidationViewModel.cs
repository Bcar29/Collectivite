using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class ValidationViewModel : ObservableObject
    {
        private readonly ValidationService _validationService;

        #region Collections

        [ObservableProperty]
        private ObservableCollection<Engagement> _engagementsNonValides = new();

        [ObservableProperty]
        private ObservableCollection<Mandat> _mandatsNonValides = new();

        [ObservableProperty]
        private ObservableCollection<OrdreRecette> _ordresRecetteNonValides = new();

        #endregion

        #region Sélection

        [ObservableProperty]
        private Engagement? _engagementSelectionne;

        [ObservableProperty]
        private Mandat? _mandatSelectionne;

        [ObservableProperty]
        private OrdreRecette? _ordreRecetteSelectionne;

        [ObservableProperty]
        private int _ongletActif = 0;

        #endregion

        #region État

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        [ObservableProperty]
        private string _messageSucces = string.Empty;

        #endregion

        #region Dialogue de rejet

        [ObservableProperty]
        private bool _isRejetDialogOpen;

        [ObservableProperty]
        private string _motifRejet = string.Empty;

        [ObservableProperty]
        private string _rejetDialogTitre = string.Empty;

        private string _typeRejet = string.Empty;
        private int _idRejet = 0;

        #endregion

        #region Compteurs

        public int CountEngagements => EngagementsNonValides.Count;
        public int CountMandats => MandatsNonValides.Count;
        public int CountOrdresRecette => OrdresRecetteNonValides.Count;
        public int CountTotal => CountEngagements + CountMandats + CountOrdresRecette;

        public decimal TotalEngagements => EngagementsNonValides.Sum(e => e.MontantEngagement);
        public decimal TotalMandats => MandatsNonValides.Sum(m => m.MontantNet);
        public decimal TotalOrdresRecette => OrdresRecetteNonValides.Sum(o => o.MontantOrdre);

        #endregion

        public ValidationViewModel()
        {
            _validationService = new ValidationService();
        }

        #region Chargement des données

        [RelayCommand]
        public async Task InitialiserAsync()
        {
            await ChargerDonneesAsync();
        }

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                // Charger les engagements non validés
                var engagements = await _validationService.GetEngagementsNonValidesAsync();
                EngagementsNonValides = new ObservableCollection<Engagement>(engagements);

                // Charger les mandats non validés
                var mandats = await _validationService.GetMandatsNonValidesAsync();
                MandatsNonValides = new ObservableCollection<Mandat>(mandats);

                // Charger les ordres de recette non validés
                var ordresRecette = await _validationService.GetOrdresRecetteNonValidesAsync();
                OrdresRecetteNonValides = new ObservableCollection<OrdreRecette>(ordresRecette);

                // Notifier les compteurs
                NotifierCompteurs();
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NotifierCompteurs()
        {
            OnPropertyChanged(nameof(CountEngagements));
            OnPropertyChanged(nameof(CountMandats));
            OnPropertyChanged(nameof(CountOrdresRecette));
            OnPropertyChanged(nameof(CountTotal));
            OnPropertyChanged(nameof(TotalEngagements));
            OnPropertyChanged(nameof(TotalMandats));
            OnPropertyChanged(nameof(TotalOrdresRecette));
        }

        #endregion

        #region Validation Engagement

        [RelayCommand]
        public async Task ValiderEngagementAsync(Engagement? engagement)
        {
            if (engagement == null) return;

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider cet engagement.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider l'engagement ?\n\n" +
                $"Objet : {engagement.Objet}\n" +
                $"Montant : {engagement.MontantEngagement:N0} GNF\n" +
                $"Tiers : {engagement.Tiers?.NomComplet ?? "N/A"}",
                "Confirmation de validation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var (success, message) = await _validationService.ValiderEngagementAsync(engagement.Id);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    await ChargerDonneesAsync();
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void OuvrirRejetEngagement(Engagement? engagement)
        {
            if (engagement == null) return;

            if (!SessionManager.HasPermission("Validation.rejet"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de rejeter cet engagement.");
                return;
            }

            _typeRejet = "Engagement";
            _idRejet = engagement.Id;
            RejetDialogTitre = $"Rejeter l'engagement - {engagement.Objet}";
            MotifRejet = string.Empty;
            IsRejetDialogOpen = true;
        }

        #endregion

        #region Validation Mandat

        [RelayCommand]
        public async Task ValiderMandatAsync(Mandat? mandat)
        {
            if (mandat == null) return;

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider ce mandat.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider le mandat ?\n\n" +
                $"N° : {mandat.NumeroMandat}\n" +
                $"Objet : {mandat.Objet}\n" +
                $"Montant Net : {mandat.MontantNet:N0} GNF\n" +
                $"Bénéficiaire : {mandat.Engagement?.Tiers?.NomComplet ?? "N/A"}",
                "Confirmation de validation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var (success, message) = await _validationService.ValiderMandatAsync(mandat.Id);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    await ChargerDonneesAsync();
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void OuvrirRejetMandat(Mandat? mandat)
        {
            if (mandat == null) return;

            if (!SessionManager.HasPermission("Validation.rejet"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de rejeter ce mandat.");
                return;
            }

            _typeRejet = "Mandat";
            _idRejet = mandat.Id;
            RejetDialogTitre = $"Rejeter le mandat N° {mandat.NumeroMandat}";
            MotifRejet = string.Empty;
            IsRejetDialogOpen = true;
        }

        #endregion

        #region Validation Ordre de Recette

        [RelayCommand]
        public async Task ValiderOrdreRecetteAsync(OrdreRecette? ordreRecette)
        {
            if (ordreRecette == null) return;

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider cet ordre de recette.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider l'ordre de recette ?\n\n" +
                $"N° : {ordreRecette.NumeroOrdre}\n" +
                $"Montant : {ordreRecette.MontantOrdre:N0} GNF\n" +
                $"Débiteur : {ordreRecette.Tiers?.NomComplet ?? "N/A"}",
                "Confirmation de validation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var (success, message) = await _validationService.ValiderOrdreRecetteAsync(ordreRecette.Id);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    await ChargerDonneesAsync();
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void OuvrirRejetOrdreRecette(OrdreRecette? ordreRecette)
        {
            if (ordreRecette == null) return;

            if (!SessionManager.HasPermission("Validation.rejet"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de rejeter cet ordre de recette.");
                return;
            }

            _typeRejet = "OrdreRecette";
            _idRejet = ordreRecette.Id;
            RejetDialogTitre = $"Rejeter l'ordre de recette N° {ordreRecette.NumeroOrdre}";
            MotifRejet = string.Empty;
            IsRejetDialogOpen = true;
        }

        #endregion

        #region Dialogue de rejet

        [RelayCommand]
        public async Task ConfirmerRejetAsync()
        {
            if (string.IsNullOrWhiteSpace(MotifRejet))
            {
                NotificationService.ShowWarning("Veuillez saisir un motif de rejet.");
                return;
            }

            if (!SessionManager.HasPermission("Validation.rejet"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de rejeter.");
                return;
            }

            try
            {
                IsLoading = true;
                (bool success, string message) result;

                switch (_typeRejet)
                {
                    case "Engagement":
                        result = await _validationService.RejeterEngagementAsync(_idRejet, MotifRejet);
                        break;
                    case "Mandat":
                        result = await _validationService.RejeterMandatAsync(_idRejet, MotifRejet);
                        break;
                    case "OrdreRecette":
                        result = await _validationService.RejeterOrdreRecetteAsync(_idRejet, MotifRejet);
                        break;
                    default:
                        result = (false, "Type inconnu");
                        break;
                }

                if (result.success)
                {
                    NotificationService.ShowSuccess(result.message);
                    IsRejetDialogOpen = false;
                    await ChargerDonneesAsync();
                }
                else
                {
                    NotificationService.ShowWarning(result.message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void FermerRejetDialog()
        {
            IsRejetDialogOpen = false;
            MotifRejet = string.Empty;
        }

        #endregion

        #region Validation en masse

        [RelayCommand]
        public async Task ValiderTousEngagementsAsync()
        {
            if (!EngagementsNonValides.Any())
            {
                NotificationService.ShowInfo("Aucun engagement à valider.");
                return;
            }

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider les engagements.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider tous les {CountEngagements} engagement(s) en attente ?\n\n" +
                $"Montant total : {TotalEngagements:N0} GNF",
                "Validation en masse",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                int validated = 0;

                foreach (var engagement in EngagementsNonValides.ToList())
                {
                    var (success, _) = await _validationService.ValiderEngagementAsync(engagement.Id);
                    if (success) validated++;
                }

                NotificationService.ShowSuccess($"{validated} engagement(s) validé(s) avec succès.");
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ValiderTousMandatsAsync()
        {
            if (!MandatsNonValides.Any())
            {
                NotificationService.ShowInfo("Aucun mandat à valider.");
                return;
            }

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider les mandats.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider tous les {CountMandats} mandat(s) en attente ?\n\n" +
                $"Montant total : {TotalMandats:N0} GNF",
                "Validation en masse",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                int validated = 0;
                int errors = 0;

                foreach (var mandat in MandatsNonValides.ToList())
                {
                    var (success, _) = await _validationService.ValiderMandatAsync(mandat.Id);
                    if (success) validated++;
                    else errors++;
                }

                NotificationService.ShowInfo($"{validated} mandat(s) validé(s).\n{errors} erreur(s) (engagement non validé).");
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ValiderTousOrdresRecetteAsync()
        {
            if (!OrdresRecetteNonValides.Any())
            {
                NotificationService.ShowInfo("Aucun ordre de recette à valider.");
                return;
            }

            if (!SessionManager.HasPermission("Valider.validate"))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission de valider les ordres de recette.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous valider tous les {CountOrdresRecette} ordre(s) de recette en attente ?\n\n" +
                $"Montant total : {TotalOrdresRecette:N0} GNF",
                "Validation en masse",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                int validated = 0;

                foreach (var ordre in OrdresRecetteNonValides.ToList())
                {
                    var (success, _) = await _validationService.ValiderOrdreRecetteAsync(ordre.Id);
                    if (success) validated++;
                }

                NotificationService.ShowSuccess($"{validated} ordre(s) de recette validé(s) avec succès.");
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}