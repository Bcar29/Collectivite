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
        private readonly ExerciceService _exerciceService;
        private readonly AuditService _auditService;
        private readonly AuthService _authService;

        public ExpressionBesoinFormViewModel(AuthService authService, AuditService auditService,int? expressionBesoinId = null)
        {
            _expressionBesoinId = expressionBesoinId;
            _isEditMode = expressionBesoinId.HasValue;
            _exerciceService = ExerciceService.Instance;
            _authService = authService;
            _auditService = auditService;

            _expressionBesoin = new ExpressionBesoin
            {
                DateCreation = DateTime.Now,
                Numero = "" ,// Sera généré dans LoadDataAsync
                ExerciceId = _exerciceService.CurrentExercice?.Id ?? 0,
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
                NotificationService.ShowError($"Erreur lors du chargement : {ex.Message}");
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
                    var (success, message, eb) = await expressionBesoinService.UpdateExpressionBesoinAsync(
                        ExpressionBesoin, detailsList);

                    if (success)
                        NotificationService.ShowSuccess(message);
                    else
                        NotificationService.ShowWarning(message);

                    if (success)
                    {
                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                        await _auditService.LogAsync(
                                   "Expression besoin modifié ",
                                   $"{eb?.Numero}  {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                   username);
                        NavigateBack();
                    }
                }
                else
                {
                    var (success, message, expressionBesoin) = await expressionBesoinService.CreateExpressionBesoinAsync(
                        ExpressionBesoin, detailsList);

                    if (success)
                        NotificationService.ShowSuccess(message);
                    else
                        NotificationService.ShowWarning(message);

                    if (success)
                    {
                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                        await _auditService.LogAsync(
                                   "Expression Besoin ajouté ",
                                   $"{expressionBesoin?.Id}  {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                   username);
                        NavigateBack();
                    }
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

        private void Cancel()
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            NavigationService.Instance.NavigateTo(new Views.Pages.ExpressionBesoinListPage(_authService));

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