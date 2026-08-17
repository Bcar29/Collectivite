using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Collectivite.ViewModels
{
    class BudgetPrimitifViewModel : ViewModelBase, IDisposable
    {
        private readonly BudgetPrimitifService _budgetPrimitifService;
        private readonly BudgetLineService _budgetLineService;
        private readonly ExerciceService _exerciceService;
        private bool _isLoading;
        private BudgetPrimitif? _selectedBudgetPrimitif;
        private bool _isDialogOpen;
        private BudgetPrimitif _dialogBudgetPrimitif;
        private bool _isEditMode;
        private bool _isValidationDialogOpen;
        private bool _isApprovalDialogOpen;
        private DateOnly _dateValidation = DateOnly.FromDateTime(DateTime.Now);
        private DateOnly _dateApprobation = DateOnly.FromDateTime(DateTime.Now);
        private BudgetPrimitif? _budgetToValidate;
        private BudgetPrimitif? _budgetToApprove;
        private byte[]? _fichierValidation;
        private string? _fileNameValidation;
        private bool _isDisposed;
        private Commune _commune;
        // 🆕 Pour la vue d'ensemble
        private int _selectedVueEnsembleTabIndex;
        private readonly List<BudgetLine> _allVueEnsembleLines = new();

        public BudgetPrimitifViewModel(BudgetPrimitifService budgetPrimitifService)
        {
            _budgetPrimitifService = budgetPrimitifService;
            _budgetLineService = new BudgetLineService();
            _exerciceService = ExerciceService.Instance;

            _dialogBudgetPrimitif = new BudgetPrimitif
            {
                DateApprobation = DateOnly.FromDateTime(DateTime.Now),
            };

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            //Commandes
            LoadBudgetPrimitifCommand = new RelayCommand(async _ => await LoadBudgetPrimitifAsync());
            //OppenAddBudgetPrimitifCommand = new RelayCommand(_ => OpenAddBudgetPrimitif());
            //OppenEditBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(budgetPrimitif => OppenEditBudgetPrimitif(budgetPrimitif));
            //SaveBudgetPrimitifCommand = new RelayCommand(async _ => await SaveBudgetPrimitifAsync(), _ => CanSaveBudgetPrimitif());
            //CancelBudgetPrimitifCommand = new RelayCommand(_ => CancelBudgetPrimitif());
            //DeleteBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(async budgetPrimitif => await DeleteBudgetPrimitifAsync(budgetPrimitif));
            OpenValidationDialogCommand = new RelayCommand<BudgetPrimitif>(budget => OpenValidationDialog(budget));
            OpenApprovalDialogCommand = new RelayCommand<BudgetPrimitif>(budget => OpenApprovalDialog(budget));
            ConfirmValidationCommand = new RelayCommand(async _ => await ConfirmValidationAsync(), _ => CanConfirmValidation());
            CancelValidationCommand = new RelayCommand(_ => CancelValidation());
            ConfirmApprovalCommand = new RelayCommand(async _ => await ConfirmApprovalAsync(), _ => CanConfirmApproval());
            CancelApprovalCommand = new RelayCommand(_ => CancelApproval());
            SelectFileCommand = new RelayCommand(_ => SelectFile());

            // 🆕 Commandes Vue d'Ensemble
            LoadVueEnsembleCommand = new RelayCommand(async _ => await LoadVueEnsembleAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());

            // Charger les données au démarrage
            LoadBudgetPrimitifCommand.Execute(null);
            LoadVueEnsembleCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<BudgetPrimitif> BudgetPrimitifs { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();

        // 🆕 Pour la vue d'ensemble
        public ObservableCollection<BudgetLine> VueEnsembleDisplayedLines { get; } = new();

        /// <summary>
        /// Permission fonctionnelle pour valider un budget primitif.
        /// </summary>
        public bool CanValidateBudget => SessionManager.HasPermission("Budget.Validate");

        /// <summary>
        /// Permission fonctionnelle pour approuver un budget primitif.
        /// </summary>
        public bool CanApproveBudget => SessionManager.HasPermission("Budget.Approve");

        // Permissions CRUD pour BudgetPrimitif (aligné avec la logique Commune)
        public bool CanViewBudgetPrimitif => SessionManager.HasPermission("BudgetPrimitif.View");
        public bool CanCreateBudgetPrimitif => SessionManager.HasPermission("BudgetPrimitif.Create");
        public bool CanEditBudgetPrimitif => SessionManager.HasPermission("BudgetPrimitif.Edit");
        public bool CanDeleteBudgetPrimitif => SessionManager.HasPermission("BudgetPrimitif.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BudgetPrimitif? SelectedBudgetPrimitif
        {
            get => _selectedBudgetPrimitif;
            set => SetProperty(ref _selectedBudgetPrimitif, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public BudgetPrimitif DialogBudgetPrimitif
        {
            get => _dialogBudgetPrimitif;
            set => SetProperty(ref _dialogBudgetPrimitif, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public DateTime DilogBudgetPrimitifDateApprobation
        {
            get => DialogBudgetPrimitif.DateApprobation.HasValue
                ? DialogBudgetPrimitif.DateApprobation.Value.ToDateTime(TimeOnly.MinValue)
                : DateTime.Now;
            set
            {
                DialogBudgetPrimitif.DateApprobation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public DateTime DilogBudgetPrimitifDateValidation
        {
            get => DialogBudgetPrimitif.DateValidation.HasValue ? DialogBudgetPrimitif.DateValidation.Value.ToDateTime(TimeOnly.MinValue) : DateTime.Now;
            set
            {
                DialogBudgetPrimitif.DateValidation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public bool IsValidationDialogOpen
        {
            get => _isValidationDialogOpen;
            set => SetProperty(ref _isValidationDialogOpen, value);
        }

        public DateOnly DateValidation
        {
            get => _dateValidation;
            set => SetProperty(ref _dateValidation, value);
        }

        public DateTime DateValidationDateTime
        {
            get => DateValidation.ToDateTime(TimeOnly.MinValue);
            set => DateValidation = DateOnly.FromDateTime(value);
        }

        public bool IsApprovalDialogOpen
        {
            get => _isApprovalDialogOpen;
            set => SetProperty(ref _isApprovalDialogOpen, value);
        }

        public DateOnly DateApprobation
        {
            get => _dateApprobation;
            set => SetProperty(ref _dateApprobation, value);
        }

        public DateTime DateApprobationDateTime
        {
            get => DateApprobation.ToDateTime(TimeOnly.MinValue);
            set => DateApprobation = DateOnly.FromDateTime(value);
        }

        public byte[]? FichierValidation
        {
            get => _fichierValidation;
            set => SetProperty(ref _fichierValidation, value);
        }

        public string? FileNameValidation
        {
            get => _fileNameValidation;
            set => SetProperty(ref _fileNameValidation, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier budget primitif" : "Ajouter budget primitif";

        // 🆕 Propriétés Vue d'Ensemble
        public int SelectedVueEnsembleTabIndex
        {
            get => _selectedVueEnsembleTabIndex;
            set
            {
                if (SetProperty(ref _selectedVueEnsembleTabIndex, value))
                {
                    ApplyVueEnsembleFilter();
                }
            }
        }

        public decimal TotalRecetteFonctionnement
        {
            get
            {
                return _budgetLineService.RecetteFonctionnementPrevu(_allVueEnsembleLines);
            }
        }


        public decimal TotalRecetteInvestissement
        {
            get
            {
                return _budgetLineService.RecetteInvestissementPrevu(_allVueEnsembleLines);
            }
        }
        public decimal TotalDepenseFonctionnement
        {
            get
            {
                return _budgetLineService.DepenseFonctionnementPrevu(_allVueEnsembleLines);
            }
        }

        public decimal TotalDepenseInvestissement
        {
            get
            {
                return _budgetLineService.DepenseInvestissementPrevu(_allVueEnsembleLines);
            }
        }
        public decimal TotalRecettes => TotalRecetteFonctionnement + TotalRecetteInvestissement;
        public decimal TotalDepenses => TotalDepenseFonctionnement + TotalDepenseInvestissement;
        public decimal Solde => TotalRecettes - TotalDepenses;

        #endregion

        #region Commands

        public ICommand LoadBudgetPrimitifCommand { get; }
        public ICommand OppenAddBudgetPrimitifCommand { get; }
        public ICommand OppenEditBudgetPrimitifCommand { get; }
        public ICommand SaveBudgetPrimitifCommand { get; }
        public ICommand CancelBudgetPrimitifCommand { get; }
        public ICommand DeleteBudgetPrimitifCommand { get; }
        public ICommand OpenValidationDialogCommand { get; }
        public ICommand ConfirmValidationCommand { get; }
        public ICommand CancelValidationCommand { get; }
        public ICommand OpenApprovalDialogCommand { get; }
        public ICommand ConfirmApprovalCommand { get; }
        public ICommand CancelApprovalCommand { get; }
        public ICommand SelectFileCommand { get; }

        // 🆕 Commandes Vue d'Ensemble
        public ICommand LoadVueEnsembleCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand PrintCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Gestionnaire pour recharger les données quand l'exercice change
        /// </summary>
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadBudgetPrimitifAsync();
                await LoadVueEnsembleAsync();
            });
        }

        public async Task LoadBudgetPrimitifAsync()
        {
            IsLoading = true;
            try
            {
                if (!CanViewBudgetPrimitif)
                {
                    BudgetPrimitifs.Clear();
                    NotificationService.ShowWarning("Accès refusé : vous n'avez pas la permission de consulter les budgets primitifs.");
                    return;
                }
                // Vérifier qu'un exercice est sélectionné
                if (_exerciceService.CurrentExercice == null)
                {
                    BudgetPrimitifs.Clear();
                    NotificationService.ShowInfo("Aucun exercice n'est sélectionné.");
                    return;
                }

                var budgetPrimitifs = await _budgetPrimitifService.GetAllBudgetPrimitifAsync();

                BudgetPrimitifs.Clear();
                foreach (var budget in budgetPrimitifs)
                {
                    BudgetPrimitifs.Add(budget);
                }

                OnPropertyChanged(nameof(BudgetPrimitifs));

                System.Diagnostics.Debug.WriteLine($"Chargé {budgetPrimitifs.Count} budgets pour l'exercice {_exerciceService.CurrentExercice.Libelle}");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement des budgets : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 🆕 Méthodes Vue d'Ensemble
        private async Task LoadVueEnsembleAsync()
        {
            IsLoading = true;
            try
            {
                if (_exerciceService.CurrentExercice == null)
                {
                    _allVueEnsembleLines.Clear();
                    VueEnsembleDisplayedLines.Clear();
                    RefreshVueEnsembleStatistics();
                    return;
                }

                var budgetLines = await _budgetPrimitifService.GetVueEnsemble();

                _allVueEnsembleLines.Clear();
                _allVueEnsembleLines.AddRange(budgetLines);

                ApplyVueEnsembleFilter();
                RefreshVueEnsembleStatistics();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement de la vue d'ensemble : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyVueEnsembleFilter()
        {
            VueEnsembleDisplayedLines.Clear();

            if (!_allVueEnsembleLines.Any())
                return;

            var filtered = _allVueEnsembleLines.AsEnumerable();

            switch (SelectedVueEnsembleTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature.Nature == NatureType.Recette &&
                        bl.Nommenclature.Section == SectionType.Fonctionnement);
                    break;
                case 1: // Recette - Investissement
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature.Nature == NatureType.Recette &&
                        bl.Nommenclature.Section == SectionType.Investissement);
                    break;
                case 2: // Dépense - Fonctionnement
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature.Nature == NatureType.Depense &&
                        bl.Nommenclature.Section == SectionType.Fonctionnement);
                    break;
                case 3: // Dépense - Investissement
                    filtered = filtered.Where(bl =>
                        bl.Nommenclature.Nature == NatureType.Depense &&
                        bl.Nommenclature.Section == SectionType.Investissement);
                    break;
            }

            foreach (var line in filtered.OrderBy(bl => bl.Nommenclature.Chapitre))
            {
                VueEnsembleDisplayedLines.Add(line);
            }
        }

        private void RefreshVueEnsembleStatistics()
        {
            OnPropertyChanged(nameof(TotalRecetteFonctionnement));
            OnPropertyChanged(nameof(TotalRecetteInvestissement));
            OnPropertyChanged(nameof(TotalDepenseFonctionnement));
            OnPropertyChanged(nameof(TotalDepenseInvestissement));
            OnPropertyChanged(nameof(TotalRecettes));
            OnPropertyChanged(nameof(TotalDepenses));
            OnPropertyChanged(nameof(Solde));
        }
        // Charger la commune
        public Commune Commune
        {
            get => _commune;
            set => SetProperty(ref _commune, value);
        }


        private async Task ExportToPdfAsync()
        {
            try
            {
                IsLoading = true;

                // Charger les infos de la commune avec relations
                var communeService = new CommuneService();
                var commune = await communeService.GetCommuneByIdWithRelationsAsync(
                    Properties.Settings.Default.CommuneId
                );
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"VueEnsemble_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Commune = commune;
                    await Task.Run(() => GeneratePdf(saveFileDialog.FileName,Commune));

                    NotificationService.ShowSuccess("Export PDF réalisé avec succès !");

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'export PDF : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GeneratePdf(string filePath, Commune _commune)
        {
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // ✅  l'en-tête  !
            PdfHeaderHelper.AjouterEnTeteOfficiel(
                document,
                _commune,
                titre: "SYNTHESE",
                sousTitre: "GESTION BUGETAIRE SYNTHESE",
                exercice: _exerciceService.CurrentExercice?.Libelle
            );

            // Police
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Vue d'Ensemble - Budget {_exerciceService.CurrentExercice?.Libelle}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // Tableau pour chaque section
            string[] sections = {
                "Recette - Fonctionnement",
                "Recette - Investissement",
                "Dépense - Fonctionnement",
                "Dépense - Investissement"
            };

            for (int i = 0; i < 4; i++)
            {
                var sectionLines = _allVueEnsembleLines.Where(bl =>
                {
                    return i switch
                    {
                        0 => bl.Nommenclature.Nature == NatureType.Recette && bl.Nommenclature.Section == SectionType.Fonctionnement,
                        1 => bl.Nommenclature.Nature == NatureType.Recette && bl.Nommenclature.Section == SectionType.Investissement,
                        2 => bl.Nommenclature.Nature == NatureType.Depense && bl.Nommenclature.Section == SectionType.Fonctionnement,
                        _ => bl.Nommenclature.Nature == NatureType.Depense && bl.Nommenclature.Section == SectionType.Investissement
                    };
                }).OrderBy(bl => bl.Nommenclature.Chapitre).ToList();

                if (sectionLines.Any())
                {
                    if (i > 0) document.NewPage();

                    // Bannière de section (cohérente avec les autres exports du module)
                    PdfHeaderHelper.AjouterBanniereSection(document, sections[i]);

                    // Table
                    PdfPTable table = new PdfPTable(3) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 15f, 55f, 30f });

                    // En-têtes
                    AddCell(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY);
                    AddCell(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY);
                    AddCell(table, "Montant Définitif", headerFont, BaseColor.LIGHT_GRAY);

                    // Données
                    decimal total = 0;
                    foreach (var line in sectionLines)
                    {
                        AddCell(table, line.Nommenclature.Chapitre ?? "", normalFont);
                        AddCell(table, line.Nommenclature.Intitule ?? "", normalFont);
                        AddCell(table, $"{line.MontantDefinitif:N0} GNF", normalFont);
                        total += line.MontantDefinitif;
                    }

                    // Total
                    AddCell(table, "", boldFont, BaseColor.LIGHT_GRAY);
                    AddCell(table, "TOTAL", boldFont, BaseColor.LIGHT_GRAY);
                    AddCell(table, $"{total:N0} GNF", boldFont, BaseColor.LIGHT_GRAY);

                    document.Add(table);
                }
            }

            // 🆕 Calcul des totaux avec les bonnes formules
            decimal totalRecetteFonctionnement = _budgetLineService.RecetteFonctionnementPrevu(_allVueEnsembleLines);
            decimal totalRecetteInvestissement = _budgetLineService.RecetteInvestissementPrevu(_allVueEnsembleLines);
            decimal totalRecetteReelInvestissement = _budgetLineService.TotalRecetteReelInvestissementPrevu(_allVueEnsembleLines);
            decimal totalGeneralRecetteReel = _budgetLineService.TotalGeneralRecetteReelPrevu(_allVueEnsembleLines);

            decimal totalDepenseFonctionnement = _budgetLineService.DepenseFonctionnementPrevu(_allVueEnsembleLines);
            decimal totalDepenseReelFonctionnement = _budgetLineService.TotalDepenseReelFonctionnementPrevu(_allVueEnsembleLines);
            decimal totalDepenseInvestissement = _budgetLineService.DepenseInvestissementPrevu(_allVueEnsembleLines);
            decimal totalGeneralDepenseReel = _budgetLineService.TotalGeneralDepenseReelPrevu(_allVueEnsembleLines);

            decimal prelevement = totalRecetteFonctionnement * 0.6m;
            decimal solde = totalGeneralRecetteReel - totalGeneralDepenseReel;

            // Synthèse générale
            document.NewPage();
            Paragraph syntheseTitle = new Paragraph("Synthèse Générale", titleFont);
            syntheseTitle.Alignment = Element.ALIGN_CENTER;
            syntheseTitle.SpacingAfter = 20;
            document.Add(syntheseTitle);

            PdfPTable summaryTable = new PdfPTable(2) { WidthPercentage = 70, HorizontalAlignment = Element.ALIGN_CENTER };
            summaryTable.SetWidths(new float[] { 65f, 35f });

            // 🆕 Section RECETTES
            AddCell(summaryTable, "═══ RECETTES ═══", headerFont, new BaseColor(200, 230, 201));
            AddCell(summaryTable, "", headerFont, new BaseColor(200, 230, 201));

            AddCell(summaryTable, "Total Recettes de Fonctionnement", boldFont);
            AddCell(summaryTable, $"{totalRecetteFonctionnement:N0} GNF", normalFont);

            AddCell(summaryTable, "Total Recettes d'Investissement", boldFont);
            AddCell(summaryTable, $"{totalRecetteInvestissement:N0} GNF", normalFont);

            AddCell(summaryTable, "Prélèvement (60% des Recettes Fonctionnement)", boldFont);
            AddCell(summaryTable, $"{prelevement:N0} GNF", normalFont);

            AddCell(summaryTable, "Total Recettes Réelles d'Investissement", boldFont);
            AddCell(summaryTable, $"{totalRecetteReelInvestissement:N0} GNF", normalFont);

            // Ligne vide
            PdfPCell emptyCell = new PdfPCell(new Phrase(" ")) { Colspan = 2, Border = 0 };
            summaryTable.AddCell(emptyCell);

            AddCell(summaryTable, "TOTAL GÉNÉRAL DES RECETTES RÉELLES", headerFont, new BaseColor(144, 238, 144));
            AddCell(summaryTable, $"{totalGeneralRecetteReel:N0} GNF", headerFont, new BaseColor(144, 238, 144));

            // Ligne vide séparation
            summaryTable.AddCell(emptyCell);

            // 🆕 Section DÉPENSES
            AddCell(summaryTable, "═══ DÉPENSES ═══", headerFont, new BaseColor(255, 205, 210));
            AddCell(summaryTable, "", headerFont, new BaseColor(255, 205, 210));

            AddCell(summaryTable, "Total Dépenses de Fonctionnement", boldFont);
            AddCell(summaryTable, $"{totalDepenseFonctionnement:N0} GNF", normalFont);

            AddCell(summaryTable, "Total Dépenses Réelles de Fonctionnement", boldFont);
            AddCell(summaryTable, $"{totalDepenseReelFonctionnement:N0} GNF", normalFont);

            AddCell(summaryTable, "Total Dépenses d'Investissement", boldFont);
            AddCell(summaryTable, $"{totalDepenseInvestissement:N0} GNF", normalFont);

            // Ligne vide
            summaryTable.AddCell(emptyCell);

            AddCell(summaryTable, "TOTAL GÉNÉRAL DES DÉPENSES RÉELLES", headerFont, new BaseColor(255, 182, 193));
            AddCell(summaryTable, $"{totalGeneralDepenseReel:N0} GNF", headerFont, new BaseColor(255, 182, 193));

            // Ligne vide séparation
            summaryTable.AddCell(emptyCell);

            // 🆕 Section SOLDE
            var soldeColor = solde >= 0 ? new BaseColor(144, 238, 144) : new BaseColor(255, 99, 71);
            AddCell(summaryTable, "═══ SOLDE BUDGÉTAIRE ═══", headerFont, soldeColor);
            AddCell(summaryTable, $"{solde:N0} GNF", headerFont, soldeColor);

            document.Add(summaryTable);

            // 🆕 Note explicative
            document.Add(new Paragraph(" "));
            Paragraph note = new Paragraph("Note : Le prélèvement de 60% des recettes de fonctionnement est transféré à l'investissement.", normalFont);
            note.Alignment = Element.ALIGN_LEFT;
            note.SpacingBefore = 10;
            document.Add(note);

            var exerciceCourant = ExerciceService.Instance.CurrentExercice;
            // ✅ Pied de page en une ligne
            PdfHeaderHelper.AjouterPiedDePage(document, $"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}", "Synthèse", exerciceCourant.Libelle);

            document.Close();
            writer.Close();
        }
        private void AddCell(PdfPTable table, string text, iTextSharp.text.Font font, BaseColor? backgroundColor = null)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.HorizontalAlignment = Element.ALIGN_LEFT;
            if (backgroundColor != null)
            {
                cell.BackgroundColor = backgroundColor;
            }
            table.AddCell(cell);
        }

        /// <summary>
        /// Imprime la vue d'ensemble du budget primitif (génère un PDF temporaire et l'ouvre pour impression)
        /// </summary>
        private async Task PrintAsync()
        {
            if (_exerciceService.CurrentExercice == null)
            {
                NotificationService.ShowInfo("Aucun exercice n'est sélectionné.");
                return;
            }

            if (!_allVueEnsembleLines.Any())
            {
                NotificationService.ShowInfo("Aucune donnée à imprimer.");
                return;
            }

            try
            {
                IsLoading = true;

                // Charger la commune si nécessaire
                if (Commune == null)
                {
                    var communeService = new CommuneService();
                    Commune = await communeService.GetCommuneByIdWithRelationsAsync(
                        Properties.Settings.Default.CommuneId
                    );
                }

                // Créer un fichier temporaire
                string tempFileName = $"VueEnsemble_{_exerciceService.CurrentExercice?.Libelle}_{Guid.NewGuid():N}.pdf";
                string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Générer le PDF (utilise Task.Run pour éviter le deadlock WPF)
                await Task.Run(() => GeneratePdf(tempPath, Commune));

                // Ouvrir le PDF avec l'application par défaut
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                NotificationService.ShowInfo(
                    "Le document s'ouvre dans votre lecteur PDF.\n\n" +
                    "Utilisez Ctrl+P ou le menu Fichier → Imprimer pour lancer l'impression.");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'impression : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Reste des méthodes inchangées...
        //public void OpenAddBudgetPrimitif()
        //{
        //    MessageBox.Show(
        //        "Les budgets primitifs sont créés automatiquement lors de la création d'un exercice.\n\n" +
        //        "Pour créer un nouveau budget primitif, veuillez créer un nouvel exercice.",
        //        "Information",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Information);
        //}

        //private void OppenEditBudgetPrimitif(BudgetPrimitif? budgetPrimitif)
        //{
        //    if (budgetPrimitif == null)
        //        return;

        //    MessageBox.Show(
        //        "La modification du budget primitif n'est plus disponible.\n" +
        //        "Veuillez utiliser le bouton d'approbation pour approuver le budget.",
        //        "Information",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Information);
        //}

        //private bool CanSaveBudgetPrimitif()
        //{
        //    return !string.IsNullOrWhiteSpace(DialogBudgetPrimitif.MontantTotal.ToString());
        //}

        //private async Task SaveBudgetPrimitifAsync()
        //{
        //    try
        //    {
        //        if (IsEditMode)
        //        {
        //            if (!CanEditBudgetPrimitif)
        //            {
        //                MessageBox.Show(
        //                    "Accès refusé : permission requise BudgetPrimitif.Edit",
        //                    "Accès refusé",
        //                    MessageBoxButton.OK,
        //                    MessageBoxImage.Warning);
        //                return;
        //            }

        //            var (success, message) = await _budgetPrimitifService.UpdateBudgetPrimitifAsync(DialogBudgetPrimitif);
        //            if (success)
        //            {
        //                MessageBox.Show(
        //                    "Budget mis à jour avec succès.",
        //                    "Succès",
        //                    MessageBoxButton.OK,
        //                    MessageBoxImage.Information);
        //                await LoadBudgetPrimitifAsync();
        //                IsDialogOpen = false;
        //            }
        //            else
        //            {
        //                MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }
        //        else
        //        {
        //            if (!CanCreateBudgetPrimitif)
        //            {
        //                MessageBox.Show(
        //                    "Accès refusé : permission requise BudgetPrimitif.Create",
        //                    "Accès refusé",
        //                    MessageBoxButton.OK,
        //                    MessageBoxImage.Warning);
        //                return;
        //            }

        //            var (success, message, _) = await _budgetPrimitifService.CreateBudgetPrimitifAsync(DialogBudgetPrimitif);
        //            if (success)
        //            {
        //                MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        //                await LoadBudgetPrimitifAsync();
        //                IsDialogOpen = false;
        //            }
        //            else
        //            {
        //                MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(
        //            $"Erreur lors de l'enregistrement du budget : {ex.Message}",
        //            "Erreur",
        //            MessageBoxButton.OK,
        //            MessageBoxImage.Error);
        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}

        //private void CancelBudgetPrimitif()
        //{
        //    IsDialogOpen = false;
        //}

        //private async Task DeleteBudgetPrimitifAsync(BudgetPrimitif? budgetPrimitif)
        //{
        //    if (budgetPrimitif == null) return;

        //    if (!CanDeleteBudgetPrimitif)
        //    {
        //        MessageBox.Show(
        //            "Accès refusé : permission requise BudgetPrimitif.Delete",
        //            "Accès refusé",
        //            MessageBoxButton.OK,
        //            MessageBoxImage.Warning);
        //        return;
        //    }

        //    var result = MessageBox.Show(
        //        $"Êtes-vous sûr de vouloir supprimer le budget de '{budgetPrimitif.Exercice.Libelle}' ?",
        //        "Confirmation de suppression",
        //        MessageBoxButton.YesNo,
        //        MessageBoxImage.Question);

        //    if (result == MessageBoxResult.Yes)
        //    {
        //        IsLoading = true;
        //        var (success, message) = await _budgetPrimitifService.DeleteBudgetPrimitifAsync(budgetPrimitif.Id);
        //        if (success)
        //        {
        //            MessageBox.Show(
        //                "Budget supprimé avec succès.",
        //                "Succès",
        //                MessageBoxButton.OK,
        //                MessageBoxImage.Information);
        //            await LoadBudgetPrimitifAsync();
        //        }
        //        else
        //        {
        //            MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        IsLoading = false;
        //    }
        //}

        private void OpenApprovalDialog(BudgetPrimitif? budget)
        {
            if (budget == null) return;

            if (!CanApproveBudget)
            {
                NotificationService.ShowWarning(
                    "Vous n'avez pas la permission d'approuver le budget primitif.\n" +
                    "Veuillez contacter l'administrateur de votre commune (Maire).");
                return;
            }

            if (budget.Status == BudgetPrimitif.Statusbudget.APPROVED || budget.Status == BudgetPrimitif.Statusbudget.VALIDATED)
            {
                NotificationService.ShowInfo(
                    $"Ce budget est déjà approuvé.\n\n" +
                    $"Date d'approbation : {budget.DateApprobation?.ToString("dd/MM/yyyy") ?? "N/A"}");
                return;
            }

            if (budget.Status != BudgetPrimitif.Statusbudget.DRAFT)
            {
                NotificationService.ShowWarning("Ce budget ne peut pas être approuvé. Il doit être en mode DRAFT.");
                return;
            }

            _budgetToApprove = budget;
            DateApprobation = DateOnly.FromDateTime(DateTime.Now);
            IsApprovalDialogOpen = true;
        }

        private void OpenValidationDialog(BudgetPrimitif? budget)
        {
            if (budget == null) return;

            if (!CanValidateBudget)
            {
                NotificationService.ShowWarning(
                    "Vous n'avez pas la permission de valider le budget primitif.\n" +
                    "Veuillez contacter l'administrateur de votre commune (Maire).");
                return;
            }

            if (budget.Status == BudgetPrimitif.Statusbudget.VALIDATED)
            {
                NotificationService.ShowInfo(
                    $"Ce budget est déjà validé.\n\n" +
                    $"Date de validation : {budget.DateValidation?.ToString("dd/MM/yyyy") ?? "N/A"}");
                return;
            }

            if (budget.Status != BudgetPrimitif.Statusbudget.APPROVED)
            {
                NotificationService.ShowWarning("Ce budget doit être approuvé avant d'être validé.");
                return;
            }

            _budgetToValidate = budget;

            var today = DateOnly.FromDateTime(DateTime.Now);
            DateValidation = budget.DateApprobation.HasValue && today >= budget.DateApprobation.Value
                ? today
                : (budget.DateApprobation ?? today);

            FichierValidation = null;
            FileNameValidation = null;

            IsValidationDialogOpen = true;
        }

        private bool CanConfirmValidation()
        {
            return _budgetToValidate != null && CanValidateBudget && FichierValidation != null;
        }

        private bool CanConfirmApproval()
        {
            return _budgetToApprove != null && CanApproveBudget;
        }

        private async Task ConfirmApprovalAsync()
        {
            if (_budgetToApprove == null) return;

            IsLoading = true;
            IsApprovalDialogOpen = false;

            try
            {
                var (success, message) = await _budgetPrimitifService.ApprouverBudgetPrimitif(
                    _budgetToApprove.Id,
                    DateApprobation);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    await LoadBudgetPrimitifAsync();
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _budgetToApprove = null;
            }
        }

        private void CancelApproval()
        {
            IsApprovalDialogOpen = false;
            _budgetToApprove = null;
        }

        private async Task ConfirmValidationAsync()
        {
            if (_budgetToValidate == null) return;

            IsLoading = true;
            IsValidationDialogOpen = false;

            try
            {
                var (success, message) = await _budgetPrimitifService.ValiderBudgetPrimitif(
                    _budgetToValidate.Id,
                    DateValidation,
                    FichierValidation,
                    FileNameValidation);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    await LoadBudgetPrimitifAsync();
                    FichierValidation = null;
                    FileNameValidation = null;
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _budgetToValidate = null;
            }
        }

        private void SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers PDF|*.pdf|Tous les fichiers|*.*",
                Title = "Sélectionner le fichier de validation"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    FileNameValidation = System.IO.Path.GetFileName(openFileDialog.FileName);
                    FichierValidation = File.ReadAllBytes(openFileDialog.FileName);
                    OnPropertyChanged(nameof(FileNameValidation));
                }
                catch (Exception ex)
                {
                    NotificationService.ShowError($"Erreur lors de la lecture du fichier : {ex.Message}");
                }
            }
        }

        private void CancelValidation()
        {
            IsValidationDialogOpen = false;
            _budgetToValidate = null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _exerciceService.ExerciceChanged -= OnExerciceChanged;
                _isDisposed = true;
            }
        }

        #endregion
    }
}