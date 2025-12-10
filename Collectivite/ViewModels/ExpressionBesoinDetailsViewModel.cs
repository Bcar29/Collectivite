using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class ExpressionBesoinDetailsViewModel : ViewModelBase
    {
        private bool _isLoading;
        private ExpressionBesoin? _expressionBesoin;
        private int _expressionBesoinId;

        public ExpressionBesoinDetailsViewModel(int expressionBesoinId)
        {
            _expressionBesoinId = expressionBesoinId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            GoBackCommand = new RelayCommand(_ => GoBack());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<DetailExpressionBesoin> Details { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ExpressionBesoin? ExpressionBesoin
        {
            get => _expressionBesoin;
            set => SetProperty(ref _expressionBesoin, value);
        }

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
                var service = new ExpressionBesoinService();
                var expressionBesoin = await service.GetExpressionBesoinByIdAsync(_expressionBesoinId);

                if (expressionBesoin != null)
                {
                    ExpressionBesoin = expressionBesoin;

                    Details.Clear();
                    if (expressionBesoin.Details != null)
                    {
                        foreach (var detail in expressionBesoin.Details)
                        {
                            Details.Add(detail);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Expression de besoin introuvable.", "Erreur",
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