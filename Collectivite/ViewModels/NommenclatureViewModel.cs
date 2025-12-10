using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;

namespace Collectivite.ViewModels
{
    class NommenclatureViewModel : ViewModelBase
    {
        private readonly NommenclatureService _nommenclatureService;
        private bool _isLoading;
        private NommenclatureTreeItemViewModel? _selectedNommenclatureTree;
        private bool _isDialogOpen;
        private Nommenclature _dialogNommenclature;
        private bool _isEditMode;
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";

        public NommenclatureViewModel(NommenclatureService nommenclature)
        {
            _nommenclatureService = nommenclature;
            _dialogNommenclature = new Nommenclature
            {
                Nature = NatureType.Recette,
                Section = SectionType.Fonctionnement
            };

            // Commandes
            LoadNommenclatureCommand = new RelayCommand(async _ => await LoadNommenclatureAsync());
            OppenAddNommenclatureCommand = new RelayCommand(_ => OpenAddNommenclature());
            OppenEditNommenclatureCommand = new RelayCommand<Nommenclature>(nommenclature => OppenEditNommenclature(nommenclature));
            SaveNommenclatureCommand = new RelayCommand(async _ => await SaveNommenclatureAsync(), _ => CanSaveNommenclature());
            CancelNommenclatureCommand = new RelayCommand(_ => CancelNommenclature());
            DeleteNommenclatureCommand = new RelayCommand<Nommenclature>(async nommenclature => await DeleteNommenclatureAsync(nommenclature));

            // Charger les données au démarrage
            LoadNommenclatureCommand.Execute(null);
        }

        #region Properties

        // Collection plate pour le ComboBox des parents dans le dialog
        public ObservableCollection<Nommenclature> Nommenclatures { get; } = new();

        // Collections hiérarchiques pour les TreeViews
        public ObservableCollection<NommenclatureTreeItemViewModel> RecetteFonctionnementTree { get; } = new();
        public ObservableCollection<NommenclatureTreeItemViewModel> RecetteInvestissementTree { get; } = new();
        public ObservableCollection<NommenclatureTreeItemViewModel> DepenseFonctionnementTree { get; } = new();
        public ObservableCollection<NommenclatureTreeItemViewModel> DepenseInvestissementTree { get; } = new();

        public bool CanViewNommenclature => SessionManager.HasPermission("Nommenclature.View");
        public bool CanCreateNommenclature => SessionManager.HasPermission("Nommenclature.Create");
        public bool CanEditNommenclature => SessionManager.HasPermission("Nommenclature.Edit");
        public bool CanDeleteNommenclature => SessionManager.HasPermission("Nommenclature.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public NommenclatureTreeItemViewModel? SelectedNommenclatureTree
        {
            get => _selectedNommenclatureTree;
            set => SetProperty(ref _selectedNommenclatureTree, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Nommenclature DialogNommenclature
        {
            get => _dialogNommenclature;
            set => SetProperty(ref _dialogNommenclature, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier la nomenclature" : "Ajouter une nomenclature";

        #endregion

        #region Commands

        public ICommand LoadNommenclatureCommand { get; }
        public ICommand OppenAddNommenclatureCommand { get; }
        public ICommand OppenEditNommenclatureCommand { get; }
        public ICommand SaveNommenclatureCommand { get; }
        public ICommand CancelNommenclatureCommand { get; }
        public ICommand DeleteNommenclatureCommand { get; }

        #endregion

        #region Methods

        public async System.Threading.Tasks.Task LoadNommenclatureAsync()
        {
            if (!CanViewNommenclature)
            {
                MessageBox.Show(
                    "Accès refusé : vous n'avez pas la permission de consulter les nomenclatures.",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Nommenclatures.Clear();
                RecetteFonctionnementTree.Clear();
                RecetteInvestissementTree.Clear();
                DepenseFonctionnementTree.Clear();
                DepenseInvestissementTree.Clear();
                return;
            }

            IsLoading = true;
            try
            {
                var nommenclatures = await _nommenclatureService.GetAllNommenclatureAsync();

                // Mettre à jour la collection plate
                Nommenclatures.Clear();
                foreach (var nommenclature in nommenclatures)
                {
                    Nommenclatures.Add(nommenclature);
                }

                // Construire les arbres hiérarchiques
                BuildHierarchicalTrees(nommenclatures);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des nomenclatures : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Construit les arbres hiérarchiques pour chaque combinaison Nature/Section
        /// </summary>
        private void BuildHierarchicalTrees(List<Nommenclature> allNomenclatures)
        {
            // Recette - Fonctionnement
            var recetteFonctionnement = allNomenclatures
                .Where(n => n.Nature == NatureType.Recette && n.Section == SectionType.Fonctionnement)
                .ToList();
            BuildTree(recetteFonctionnement, RecetteFonctionnementTree);

            // Recette - Investissement
            var recetteInvestissement = allNomenclatures
                .Where(n => n.Nature == NatureType.Recette && n.Section == SectionType.Investissement)
                .ToList();
            BuildTree(recetteInvestissement, RecetteInvestissementTree);

            // Dépense - Fonctionnement
            var depenseFonctionnement = allNomenclatures
                .Where(n => n.Nature == NatureType.Depense && n.Section == SectionType.Fonctionnement)
                .ToList();
            BuildTree(depenseFonctionnement, DepenseFonctionnementTree);

            // Dépense - Investissement
            var depenseInvestissement = allNomenclatures
                .Where(n => n.Nature == NatureType.Depense && n.Section == SectionType.Investissement)
                .ToList();
            BuildTree(depenseInvestissement, DepenseInvestissementTree);
        }

        /// <summary>
        /// Construit un arbre hiérarchique à partir d'une liste de nomenclatures
        /// </summary>
        private void BuildTree(List<Nommenclature> nomenclatures, ObservableCollection<NommenclatureTreeItemViewModel> targetCollection)
        {
            targetCollection.Clear();

            // Utiliser TreeHelper pour construire l'arbre
            var tree = TreeHelper.BuildTree(
                nomenclatures,
                n => n.Id,
                n => n.ParentId,
                items => items.OrderBy(n => n.Chapitre)
                              .ThenBy(n => n.Article)
                              .ThenBy(n => n.Paragraphe)
                              .ThenBy(n => n.SousParagraphe)
            );

            // Convertir les TreeNode en NommenclatureTreeItemViewModel
            foreach (var node in tree)
            {
                var viewModel = CreateTreeItemViewModel(node, 0);
                targetCollection.Add(viewModel);
            }
        }

        /// <summary>
        /// Crée récursivement les ViewModels pour l'arbre
        /// </summary>
        private NommenclatureTreeItemViewModel CreateTreeItemViewModel(TreeNode<Nommenclature> node, int level)
        {
            var viewModel = new NommenclatureTreeItemViewModel(node.Data)
            {
                Level = level,
                IsExpanded = level == 0 // Déplier automatiquement le premier niveau
            };

            foreach (var childNode in node.Children)
            {
                var childViewModel = CreateTreeItemViewModel(childNode, level + 1);
                viewModel.Children.Add(childViewModel);
            }

            return viewModel;
        }

        private void OpenAddNommenclature()
        {
            if (!CanCreateNommenclature)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Nommenclature.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogNommenclature = new Nommenclature();
           
            IsEditMode = false;
            IsDialogOpen = true;
        }

        private void OppenEditNommenclature(Nommenclature? nommenclature)
        {
            if (nommenclature == null)
                return;

            if (!CanEditNommenclature)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Nommenclature.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsEditMode = true;
            DialogNommenclature = new Nommenclature
            {
                Id = nommenclature.Id,
                Chapitre = nommenclature.Chapitre,
                Article = nommenclature.Article,
                Paragraphe = nommenclature.Paragraphe,
                SousParagraphe = nommenclature.SousParagraphe,
                Intitule = nommenclature.Intitule,
                Nature = nommenclature.Nature,
                Section = nommenclature.Section,
                ParentId = nommenclature.ParentId
            };
            IsDialogOpen = true;
        }

        private bool CanSaveNommenclature()
        {
            return !string.IsNullOrWhiteSpace(DialogNommenclature.Intitule);
        }

        private async System.Threading.Tasks.Task SaveNommenclatureAsync()
        {
            if (IsEditMode && !CanEditNommenclature)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Nommenclature.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsEditMode && !CanCreateNommenclature)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Nommenclature.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _nommenclatureService.UpdateNommenclatureAsync(DialogNommenclature);
                    if (success)
                    {
                        MessageBox.Show("Nomenclature mise à jour avec succès",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadNommenclatureAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var (success, message, _) = await _nommenclatureService.CreateNommenclatureAsync(DialogNommenclature);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadNommenclatureAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement de la nomenclature : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelNommenclature()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteNommenclatureAsync(Nommenclature? nommenclature)
        {
            if (nommenclature == null)
                return;

            if (!CanDeleteNommenclature)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Nommenclature.Delete",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer la nomenclature '{nommenclature.Intitule}' ?\n\n" +
                "⚠️ Attention : Tous les éléments enfants seront également supprimés !",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var (success, message) = await _nommenclatureService.DeleteNommenclatureAsync(nommenclature.Id);
                if (success)
                {
                    MessageBox.Show("Nomenclature supprimée avec succès",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadNommenclatureAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                IsLoading = false;
            }
        }

        #endregion
    }
}