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
    public class ExpressionBesoinFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private ExpressionBesoin _expressionBesoin;
        private bool _isEditMode;
        private int? _expressionBesoinId;

        public ExpressionBesoinFormViewModel(int? expressionBesoinId = null)
        {
            _expressionBesoinId = expressionBesoinId;
            _isEditMode = expressionBesoinId.HasValue;

            _expressionBesoin = new ExpressionBesoin
            {
                DateCreation = DateTime.Now,
                Numero = "" // Sera généré dans LoadDataAsync
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddDetailCommand = new RelayCommand(_ => AddDetail());
            RemoveDetailCommand = new RelayCommand<DetailExpressionBesoin>(d => RemoveDetail(d));

            // Charger les données
            LoadDataCommand.Execute(null);
        }
        #region Properties

        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Nommenclature> Nommenclatures { get; } = new();
        public ObservableCollection<DetailExpressionBesoin> Details { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ExpressionBesoin ExpressionBesoin
        {
            get => _expressionBesoin;
            set => SetProperty(ref _expressionBesoin, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string PageTitle => IsEditMode ? "Modifier l'expression de besoin" : "Nouvelle expression de besoin";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }

        #endregion

        #region Methods
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les exercices
                var exerciceService = new ExerciceService();
                var exercices = await exerciceService.GetAllExerciceAsync();

                Exercices.Clear();
                foreach (var e in exercices)
                {
                    Exercices.Add(e);
                }

                // Charger les nomenclatures
                var exp = new ExpressionBesoinService();
                var nommenclatures = await exp.GetNommenclaturesAsync();

                Nommenclatures.Clear();
                foreach (var n in nommenclatures)
                {
                    Nommenclatures.Add(n);
                }

                // Si mode édition, charger l'expression de besoin
                if (_expressionBesoinId.HasValue)
                {
                    var expressionBesoinService = new ExpressionBesoinService();
                    var expressionBesoin = await expressionBesoinService.GetExpressionBesoinByIdAsync(_expressionBesoinId.Value);

                    if (expressionBesoin != null)
                    {
                        ExpressionBesoin = new ExpressionBesoin
                        {
                            Id = expressionBesoin.Id,
                            Numero = expressionBesoin.Numero,
                            DateCreation = expressionBesoin.DateCreation,
                            ExerciceId = expressionBesoin.ExerciceId
                        };

                        Details.Clear();
                        if (expressionBesoin.Details != null)
                        {
                            foreach (var detail in expressionBesoin.Details)
                            {
                                Details.Add(new DetailExpressionBesoin
                                {
                                    Id = detail.Id,
                                    NommenclatureId = detail.NommenclatureId,
                                    Designation = detail.Designation,
                                    Quantite = detail.Quantite
                                });
                            }
                        }
                    }
                }
                else
                {
                    // Mode création : générer le prochain numéro
                    var expressionBesoinService = new ExpressionBesoinService();
                    var nextNumero = await expressionBesoinService.GenerateNextNumeroAsync();
                    ExpressionBesoin.Numero = nextNumero;
                    OnPropertyChanged(nameof(ExpressionBesoin));
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
            return ExpressionBesoin != null &&
                   !string.IsNullOrWhiteSpace(ExpressionBesoin.Numero) &&
                   ExpressionBesoin.ExerciceId > 0 &&
                   Details.Count > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                var expressionBesoinService = new ExpressionBesoinService();
                var detailsList = Details.ToList();

                if (IsEditMode)
                {
                    var (success, message) = await expressionBesoinService.UpdateExpressionBesoinAsync(
                        ExpressionBesoin, detailsList);

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
                    var (success, message, expressionBesoin) = await expressionBesoinService.CreateExpressionBesoinAsync(
                        ExpressionBesoin, detailsList);

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
            NavigateBack();
        }

        private void NavigateBack()
        {
            NavigationService.Instance.GoBack();
        }

        private void AddDetail()
        {
            var newDetail = new DetailExpressionBesoin
            {
                Designation = "",
                Quantite = 1,
                NommenclatureId = 0
            };

            Details.Add(newDetail);
        }

        private void RemoveDetail(DetailExpressionBesoin? detail)
        {
            if (detail != null)
            {
                Details.Remove(detail);
            }
        }

        #endregion
    }
}