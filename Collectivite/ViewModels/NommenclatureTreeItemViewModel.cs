using Collectivite.Models;
using Collectivite.Utils;
using System.Collections.ObjectModel;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour représenter une nomenclature dans un TreeView
    /// </summary>
    public class NommenclatureTreeItemViewModel : ViewModelBase
    {
        private bool _isExpanded;
        private bool _isSelected;

        public NommenclatureTreeItemViewModel(Nommenclature nommenclature)
        {
            Nommenclature = nommenclature;
            Children = new ObservableCollection<NommenclatureTreeItemViewModel>();
        }

        /// <summary>
        /// La nomenclature elle-même
        /// </summary>
        public Nommenclature Nommenclature { get; }

        /// <summary>
        /// Les enfants de cette nomenclature
        /// </summary>
        public ObservableCollection<NommenclatureTreeItemViewModel> Children { get; }

        /// <summary>
        /// Indique si le nœud est déplié
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// Indique si le nœud est sélectionné
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Indique si le nœud a des enfants
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// Affichage du code complet (Chapitre-Article-Paragraphe-SousParagraphe)
        /// </summary>
        public string CodeComplet
        {
            get
            {
                var parts = new[]
                {
                    Nommenclature.Chapitre,
                    Nommenclature.Article,
                    Nommenclature.Paragraphe,
                    Nommenclature.SousParagraphe
                }.Where(p => !string.IsNullOrEmpty(p));

                return string.Join("-", parts);
            }
        }

        /// <summary>
        /// Niveau de profondeur dans l'arbre (0 = racine)
        /// </summary>
        public int Level { get; set; }
    }
}