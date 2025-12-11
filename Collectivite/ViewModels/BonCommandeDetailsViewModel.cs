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
    public class BonCommandeDetailsViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande? _bonCommande;
        private int _bonCommandeId;

        public BonCommandeDetailsViewModel(int bonCommandeId)
        {
            _bonCommandeId = bonCommandeId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            GoBackCommand = new RelayCommand(_ => GoBack());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<DetailBonCommande> Details { get; } = new();
        public ObservableCollection<Engagement> Engagements { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BonCommande? BonCommande
        {
            get => _bonCommande;
            set => SetProperty(ref _bonCommande, value);
        }

        public double MontantTotal => Details.Sum(d => d.Total);

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand GoBackCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var service = new BonCommandeService();
                var bonCommande = await service.GetBonCommandeByIdAsync(_bonCommandeId);

                if (bonCommande != null)
                {
                    BonCommande = bonCommande;

                    // Charger les détails
                    Details.Clear();
                    if (bonCommande.Details != null)
                    {
                        foreach (var detail in bonCommande.Details)
                        {
                            Details.Add(detail);
                        }
                    }

                    // Charger les engagements
                    Engagements.Clear();
                    if (bonCommande.Engagements != null)
                    {
                        foreach (var engagement in bonCommande.Engagements)
                        {
                            Engagements.Add(engagement);
                        }
                    }

                    OnPropertyChanged(nameof(MontantTotal));
                }
                else
                {
                    MessageBox.Show("Bon de commande introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GoBack();
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

        private void GoBack()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}