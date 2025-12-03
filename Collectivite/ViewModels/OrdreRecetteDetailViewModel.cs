using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class OrdreRecetteDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private OrdreRecette? _ordreRecette;

        public OrdreRecetteDetailViewModel(int ordreRecetteId)
        {
            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync(ordreRecetteId));
            NavigateBackCommand = new RelayCommand(_ => NavigateBack());
            NavigateToEditCommand = new RelayCommand(_ => NavigateToEdit());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
            PrintCommand = new RelayCommand(_ => Print());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public OrdreRecette? OrdreRecette
        {
            get => _ordreRecette;
            set => SetProperty(ref _ordreRecette, value);
        }

        public string NumeroOrdre => OrdreRecette?.NumeroOrdre ?? "N/A";
        public string DateOrdre => OrdreRecette?.DateOrdre.ToString("dd/MM/yyyy") ?? "N/A";
        public string Exercice => OrdreRecette?.Exercice?.Libelle ?? "N/A";
        public string Commune => OrdreRecette?.Commune?.Nom ?? "N/A";
        public string LigneBudgetaire => OrdreRecette?.BudgetLine?.Nommenclature?.Intitule ?? "N/A";
        public string Chapitre => OrdreRecette?.BudgetLine?.Nommenclature?.Chapitre ?? "N/A";
        public string Article => OrdreRecette?.BudgetLine?.Nommenclature?.Article ?? "N/A";
        public string Comptable => OrdreRecette?.Comptable ?? "N/A";
        public new string Tiers => OrdreRecette?.Tiers?.Nom ?? "Non spécifié";
        public string Motifs => OrdreRecette?.Motifs ?? "Aucun motif";
        public string MontantChiffres => OrdreRecette != null ? $"{OrdreRecette.MontantOrdre:N0} GNF" : "0 GNF";
        public string MontantLettres => OrdreRecette?.MontantOrdreLettre ?? "";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateToEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PrintCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync(int ordreRecetteId)
        {
            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();
                var ordre = await ordreRecetteService.GetOrdreRecetteByIdAsync(ordreRecetteId);

                if (ordre != null)
                {
                    OrdreRecette = ordre;

                    // Notifier tous les changements
                    OnPropertyChanged(nameof(NumeroOrdre));
                    OnPropertyChanged(nameof(DateOrdre));
                    OnPropertyChanged(nameof(Exercice));
                    OnPropertyChanged(nameof(Commune));
                    OnPropertyChanged(nameof(LigneBudgetaire));
                    OnPropertyChanged(nameof(Chapitre));
                    OnPropertyChanged(nameof(Article));
                    OnPropertyChanged(nameof(Comptable));
                    OnPropertyChanged(nameof(Tiers));
                    OnPropertyChanged(nameof(Motifs));
                    OnPropertyChanged(nameof(MontantChiffres));
                    OnPropertyChanged(nameof(MontantLettres));
                }
                else
                {
                    MessageBox.Show("Ordre de recette introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigateBack();
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

        private void NavigateBack()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null && frame.CanGoBack)
                {
                    frame.GoBack();
                }
            }
        }

        private void NavigateToEdit()
        {
            if (OrdreRecette == null) return;

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.Navigate(new Views.Pages.OrdreRecetteFormPage(OrdreRecette.Id));
                }
            }
        }

        private async System.Threading.Tasks.Task DeleteAsync()
        {
            if (OrdreRecette == null) return;

            var result = MessageBox.Show(
                $"⚠️ Supprimer l'ordre de recette '{OrdreRecette.NumeroOrdre}' ?\n\n" +
                $"Montant : {OrdreRecette.MontantOrdre:N0} GNF\n" +
                $"Date : {OrdreRecette.DateOrdre:dd/MM/yyyy}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();
                var (success, message) = await ordreRecetteService.DeleteOrdreRecetteAsync(OrdreRecette.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    NavigateBack();
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

        private void Print()
        {
            MessageBox.Show("Fonctionnalité d'impression en cours de développement.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);

            // TODO: Implémenter l'impression/export PDF
        }

        #endregion
    }
}