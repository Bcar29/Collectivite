using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectivite.ViewModels
{
    public class MandatDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private int _mandatId;
        private Mandat? _mandat;
        private Mouvement? _mouvement;
        private List<Mouvement>? _tousLesMouvements;

        public MandatDetailViewModel(int mandatId)
        {
            _mandatId = mandatId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            PrintCommand = new RelayCommand(_ => Print());
            ExportPdfCommand = new RelayCommand(_ => ExportPdf());
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
            set
            {
                if (SetProperty(ref _mandat, value))
                {
                    OnPropertyChanged(nameof(EstPaye));
                    OnPropertyChanged(nameof(EstNonPaye));
                    OnPropertyChanged(nameof(IsMandatValide));
                    OnPropertyChanged(nameof(EtatBackground));
                    OnPropertyChanged(nameof(StatutBackground));
                }
            }
        }

        public Mouvement? Mouvement
        {
            get => _mouvement;
            set
            {
                if (SetProperty(ref _mouvement, value))
                {
                    OnPropertyChanged(nameof(ModePaiement));
                    OnPropertyChanged(nameof(ModePaiementIcon));
                    OnPropertyChanged(nameof(EstVirement));
                    OnPropertyChanged(nameof(EstCheque));
                    OnPropertyChanged(nameof(EstEspece));
                    OnPropertyChanged(nameof(HasMouvement));
                }
            }
        }

        public List<Mouvement>? TousLesMouvements
        {
            get => _tousLesMouvements;
            set => SetProperty(ref _tousLesMouvements, value);
        }

        // Propriété pour vérifier si des mouvements existent
        public bool HasMouvement => Mouvement != null || (TousLesMouvements != null && TousLesMouvements.Any());

        // Utiliser le Status du mandat
        public bool EstPaye => Mandat?.Status == Mandat.StatutMandat.Payé || Mandat?.Status == Mandat.StatutMandat.Partiel;
        public bool EstNonPaye => Mandat?.Status == Mandat.StatutMandat.Non_Payé;
        public bool IsMandatValide => Mandat?.Etat == Mandat.EtatMandat.Validé;

        public Brush EtatBackground
        {
            get
            {
                if (Mandat == null) return new SolidColorBrush(Colors.Gray);

                return Mandat.Etat switch
                {
                    Mandat.EtatMandat.Validé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    Mandat.EtatMandat.Non_Validé => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        public Brush StatutBackground
        {
            get
            {
                if (Mandat == null) return new SolidColorBrush(Colors.Gray);

                return Mandat.Status switch
                {
                    Mandat.StatutMandat.Payé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    Mandat.StatutMandat.Partiel => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Bleu
                    Mandat.StatutMandat.Non_Payé => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rouge
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Propriétés pour le mode de paiement (visible seulement si un Mouvement existe)
        public string ModePaiement
        {
            get
            {
                if (Mouvement == null) return "";

                if (!string.IsNullOrEmpty(Mouvement.RefVirement))
                    return "Virement bancaire";
                else if (!string.IsNullOrEmpty(Mouvement.RefChèque))
                    return "Chèque";
                else
                    return "Espèces";
            }
        }

        public string ModePaiementIcon
        {
            get
            {
                if (Mouvement == null) return "Cash";

                if (!string.IsNullOrEmpty(Mouvement.RefVirement))
                    return "BankTransfer";
                else if (!string.IsNullOrEmpty(Mouvement.RefChèque))
                    return "CheckDecagram";
                else
                    return "Cash";
            }
        }

        public bool EstVirement => !string.IsNullOrEmpty(Mouvement?.RefVirement);
        public bool EstCheque => !string.IsNullOrEmpty(Mouvement?.RefChèque);
        public bool EstEspece => Mouvement != null && string.IsNullOrEmpty(Mouvement.RefVirement) && string.IsNullOrEmpty(Mouvement.RefChèque);

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }
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

                    // Charger TOUS les mouvements associés à ce mandat
                    using (var context = new AppDbContext())
                    {
                        var mouvementService = new MouvementService(context);

                        // Récupérer tous les mouvements
                        var mouvements = await mouvementService.GetMouvementsByMandatIdAsync(_mandatId);
                        TousLesMouvements = mouvements;

                        // Le mouvement principal est le plus récent
                        Mouvement = mouvements.FirstOrDefault();

                        // Debug
                        if (mouvements.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Trouvé {mouvements.Count} mouvement(s) pour le mandat {_mandatId}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Aucun mouvement trouvé pour le mandat {_mandatId}");
                        }
                    }
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
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Print()
        {
            // TODO: Implémenter l'impression
            MessageBox.Show("Fonctionnalité d'impression en cours de développement.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPdf()
        {
            // TODO: Implémenter l'export PDF
            MessageBox.Show("Fonctionnalité d'export PDF en cours de développement.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RetourListe()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}