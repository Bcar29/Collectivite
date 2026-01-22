using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class FactureDetailsViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Facture? _facture;
        private readonly int _factureId;
        private Commune _commune = new();

        public FactureDetailsViewModel(int factureId)
        {
            _factureId = factureId;
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            GoBackCommand = new RelayCommand(_ => GoBack());

            LoadDataCommand.Execute(null);
        }

        public ObservableCollection<DetailsFacture> Details { get; } = new();

        public Commune Commune
        {
            get => _commune;
            set => SetProperty(ref _commune, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Facture? Facture
        {
            get => _facture;
            set => SetProperty(ref _facture, value);
        }

        public double MontantTotal => Details.Sum(d => d.MontantTotal);

        public ICommand LoadDataCommand { get; }
        public ICommand GoBackCommand { get; }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var communeService = new CommuneService();
                Commune = await communeService.GetCommuneByIdWithRelationsAsync(
                    Properties.Settings.Default.CommuneId
                );

                var factureService = new FactureService();
                var facture = await factureService.GetFactureByIdAsync(_factureId);

                if (facture == null)
                {
                    MessageBox.Show("Facture introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GoBack();
                    return;
                }

                Facture = facture;

                Details.Clear();
                if (facture.Details != null)
                {
                    foreach (var detail in facture.Details)
                    {
                        Details.Add(detail);
                    }
                }

                OnPropertyChanged(nameof(MontantTotal));
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

        private void GoBack()
        {
            NavigationService.Instance.GoBack();
        }
    }
}
