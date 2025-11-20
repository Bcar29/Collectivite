using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class MandatDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private int _mandatId;
        private Mandat? _mandat;

        public MandatDetailViewModel(int mandatId)
        {
            _mandatId = mandatId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenEditPageCommand = new RelayCommand(_ => OpenEditPage());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
            MarquerPayeCommand = new RelayCommand(async _ => await MarquerCommePaye());
            AnnulerPaiementCommand = new RelayCommand(async _ => await AnnulerPaiement());
            RetourCommand = new RelayCommand(_ => RetourListe());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Mandat? Mandat
        {
            get => _mandat;
            set => SetProperty(ref _mandat, value);
        }

        public bool EstPaye => Mandat?.DatePaiement != null;
        public string StatutPaiement => EstPaye ? "✅ Payé" : "⏳ Non payé";
        public string StatutCouleur => EstPaye ? "#4CAF50" : "#FF9800";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenEditPageCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MarquerPayeCommand { get; }
        public ICommand AnnulerPaiementCommand { get; }
        public ICommand RetourCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var mandat = await mandatService.GetMandatByIdAsync(_mandatId);

                if (mandat != null)
                {
                    Mandat = mandat;
                    OnPropertyChanged(nameof(EstPaye));
                    OnPropertyChanged(nameof(StatutPaiement));
                    OnPropertyChanged(nameof(StatutCouleur));
                }
                else
                {
                    MessageBox.Show("Mandat introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    RetourListe();
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

        private void OpenEditPage()
        {
            if (Mandat == null) return;

            var editPage = new Views.Pages.MandatFormPage(Mandat.Id);
            Application.Current.MainWindow.Content = editPage;
        }

        private async System.Threading.Tasks.Task DeleteAsync()
        {
            if (Mandat == null) return;

            var result = MessageBox.Show(
                $"⚠️ Supprimer le mandat '{Mandat.NumeroMandat}' ?\n\n" +
                $"Montant : {Mandat.MontantNet:N0} GNF\n" +
                $"Objet : {Mandat.Objet}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.DeleteMandatAsync(Mandat.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    RetourListe();
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

        private async System.Threading.Tasks.Task MarquerCommePaye()
        {
            if (Mandat == null) return;

            if (Mandat.DatePaiement != null)
            {
                MessageBox.Show("Ce mandat a déjà été payé.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Marquer le mandat '{Mandat.NumeroMandat}' comme payé ?\n\n" +
                $"Montant : {Mandat.MontantNet:N0} GNF",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.MarquerCommePaye(Mandat.Id, DateTime.Now);

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

        private async System.Threading.Tasks.Task AnnulerPaiement()
        {
            if (Mandat == null) return;

            if (Mandat.DatePaiement == null)
            {
                MessageBox.Show("Ce mandat n'a pas encore été payé.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Annuler le paiement du mandat '{Mandat.NumeroMandat}' ?\n\n" +
                $"Montant : {Mandat.MontantNet:N0} GNF\n" +
                $"Date de paiement : {Mandat.DatePaiement:dd/MM/yyyy}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.AnnulerPaiement(Mandat.Id);

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

        private void RetourListe()
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

        #endregion
    }
}