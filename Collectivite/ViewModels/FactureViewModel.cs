using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Collectivite.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using System.Diagnostics;

namespace Collectivite.ViewModels
{
    public class FactureViewModel : ViewModelBase
    {
        // private readonly string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";
        private bool _isLoading;
        private Facture? _selectedFacture;
        private bool _isDialogOpen;
        private Facture _dialogFacture;
        private bool _isEditMode;
        private string _fichierName;
        private DetailsFacture? _selectedDetail;

        public FactureViewModel()
        {
            _dialogFacture = new Facture
            {
                DateFacture = DateTime.Now,
                DateEcheance = DateTime.Now.AddDays(30),
                Status = StatusFact.impayee,
                MontantHT = 0,
                TauxTVA = 0,
                MontantTTC = 0,
                NumeroFacture = "",
                Description = ""
            };
            _fichierName = string.Empty;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Facture>(f => OpenEditDialog(f));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Facture>(async f => await DeleteAsync(f));
            ChangeStatusCommand = new RelayCommand<Facture>(async f => await ChangeStatusAsync(f));
            ChooseFileCommand = new RelayCommand(_ => ChooseFile());

            // Commandes pour les détails
            AddDetailCommand = new RelayCommand(_ => AddDetail());
            RemoveDetailCommand = new RelayCommand<DetailsFacture>(d => RemoveDetail(d));
            // Dans le constructeur
            RecalculerMontantDetailCommand = new RelayCommand<DetailsFacture>(d => RecalculerMontantDetail(d));
            OnMontantsChangedCommand = new RelayCommand(_ => OnMontantsChanged());
            // Dans le constructeur
            ExportPdfCommand = new RelayCommand<Facture>(f => ExportToPdf(f));
            ExportExcelCommand = new RelayCommand<Facture>(f => ExportToExcel(f));
            ImprimerCommand = new RelayCommand<Facture>(f => Imprimer(f));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Permissions

        public bool CanViewFacture => SessionManager.HasPermission("Facture.View");
        public bool CanCreateFacture => SessionManager.HasPermission("Facture.Create");
        public bool CanEditFacture => SessionManager.HasPermission("Facture.Edit");
        public bool CanDeleteFacture => SessionManager.HasPermission("Facture.Delete");

        #endregion

        #region Properties

        public ObservableCollection<Facture> Factures { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Contrats> ContratsList { get; } = new();
        public ObservableCollection<DetailsFacture> DialogDetails { get; } = new();


        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Facture? SelectedFacture
        {
            get => _selectedFacture;
            set => SetProperty(ref _selectedFacture, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Facture DialogFacture
        {
            get => _dialogFacture;
            set
            {
                if (SetProperty(ref _dialogFacture, value))
                {
                    OnPropertyChanged(nameof(MontantTVA));
                    OnPropertyChanged(nameof(MontantTTCCalcule));
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string FichierName
        {
            get => _fichierName;
            set => SetProperty(ref _fichierName, value);
        }

        public DetailsFacture? SelectedDetail
        {
            get => _selectedDetail;
            set => SetProperty(ref _selectedDetail, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier la facture" : "Nouvelle facture";

        // ✅ Propriétés calculées automatiquement
        public double MontantTVA
        {
            get
            {
                if (DialogFacture == null) return 0;
                return DialogFacture.MontantHT * (DialogFacture.TauxTVA / 100);
            }
        }

        public double MontantTTCCalcule
        {
            get
            {
                if (DialogFacture == null) return 0;
                return DialogFacture.MontantHT + MontantTVA;
            }
        }

        public double TotalDetails => DialogDetails.Sum(d => d.MontantTotal);

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ChangeStatusCommand { get; }
        public ICommand ChooseFileCommand { get; }
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }
        // Dans la région #region Commands
        public ICommand RecalculerMontantDetailCommand { get; }
        public ICommand OnMontantsChangedCommand { get; }
        // Dans la région #region Commands
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ImprimerCommand { get; }

        #endregion

        #region Methods
        #region Export et Impression

/// <summary>
/// Exporte une facture en PDF
/// </summary>
private void ExportToPdf(Facture? facture)
{
    if (facture == null) return;

    try
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Fichier PDF (*.pdf)|*.pdf",
            FileName = $"Facture_{facture.NumeroFacture}_{DateTime.Now:yyyyMMdd}.pdf"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            // Créer le document PDF
            Document document = new Document(PageSize.A4, 50, 50, 50, 50);
            PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));
            
            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

            // Titre
            Paragraph title = new Paragraph($"FACTURE N° {facture.NumeroFacture}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            // Informations de la facture
            PdfPTable infoTable = new PdfPTable(2);
            infoTable.WidthPercentage = 100;
            infoTable.SpacingAfter = 20;

            AddInfoRow(infoTable, "Date de facture:", facture.DateFacture.ToString("dd/MM/yyyy"), normalFont);
            AddInfoRow(infoTable, "Date d'échéance:", facture.DateEcheance.ToString("dd/MM/yyyy"), normalFont);
            AddInfoRow(infoTable, "Tiers:", facture.Tiers?.Nom ?? "N/A", normalFont);
            AddInfoRow(infoTable, "Exercice:", facture.Exercice?.Libelle ?? "N/A", normalFont);
            AddInfoRow(infoTable, "Statut:", facture.Status.ToString().ToUpper(), normalFont);
            if (facture.Contrats != null)
                AddInfoRow(infoTable, "Contrat:", facture.Contrats.NumeroContrat, normalFont);

            document.Add(infoTable);

            // Description
            if (!string.IsNullOrWhiteSpace(facture.Description))
            {
                Paragraph desc = new Paragraph($"Description: {facture.Description}", normalFont);
                desc.SpacingAfter = 15;
                document.Add(desc);
            }

            // Tableau des détails
            Paragraph detailsTitle = new Paragraph("DÉTAILS DE LA FACTURE", headerFont);
            detailsTitle.SpacingAfter = 10;
            document.Add(detailsTitle);

            PdfPTable detailsTable = new PdfPTable(4);
            detailsTable.WidthPercentage = 100;
            detailsTable.SetWidths(new float[] { 3f, 1f, 1.5f, 1.5f });
            detailsTable.SpacingAfter = 20;

            // En-têtes
            AddTableHeader(detailsTable, "Libellé", headerFont);
            AddTableHeader(detailsTable, "Quantité", headerFont);
            AddTableHeader(detailsTable, "Prix Unit.", headerFont);
            AddTableHeader(detailsTable, "Total", headerFont);

            // Lignes de détails
            if (facture.Details != null)
            {
                foreach (var detail in facture.Details)
                {
                    AddTableCell(detailsTable, detail.Libelle, normalFont);
                    AddTableCell(detailsTable, detail.Quantite.ToString("N0"), normalFont, Element.ALIGN_CENTER);
                    AddTableCell(detailsTable, $"{detail.PrixUnitaire:N0} GNF", normalFont, Element.ALIGN_RIGHT);
                    AddTableCell(detailsTable, $"{detail.MontantTotal:N0} GNF", normalFont, Element.ALIGN_RIGHT);
                }
            }

            document.Add(detailsTable);

            // Totaux
            PdfPTable totalTable = new PdfPTable(2);
            totalTable.WidthPercentage = 40;
            totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;

            AddTotalRow(totalTable, "Montant HT:", $"{facture.MontantHT:N0} GNF", normalFont);
            AddTotalRow(totalTable, $"TVA ({facture.TauxTVA}%):", $"{(facture.MontantTTC - facture.MontantHT):N0} GNF", normalFont);
            
            PdfPCell totalLabel = new PdfPCell(new Phrase("TOTAL TTC:", headerFont));
            totalLabel.Border = Rectangle.NO_BORDER;
            totalLabel.BackgroundColor = new BaseColor(227, 242, 253);
            totalLabel.Padding = 8;
            totalTable.AddCell(totalLabel);

            PdfPCell totalValue = new PdfPCell(new Phrase($"{facture.MontantTTC:N0} GNF", headerFont));
            totalValue.Border = Rectangle.NO_BORDER;
            totalValue.BackgroundColor = new BaseColor(227, 242, 253);
            totalValue.HorizontalAlignment = Element.ALIGN_RIGHT;
            totalValue.Padding = 8;
            totalTable.AddCell(totalValue);

            document.Add(totalTable);

            // Footer
            Paragraph footer = new Paragraph($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", smallFont);
            footer.Alignment = Element.ALIGN_CENTER;
            footer.SpacingBefore = 30;
            document.Add(footer);

            document.Close();

            MessageBox.Show($"PDF exporté avec succès :\n{saveFileDialog.FileName}", 
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

            // Ouvrir le fichier
            Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}", 
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

// Méthodes helper pour PDF
private void AddInfoRow(PdfPTable table, string label, string value, iTextSharp.text.Font font)
{
    PdfPCell labelCell = new PdfPCell(new Phrase(label, font));
    labelCell.Border = Rectangle.NO_BORDER;
    labelCell.Padding = 5;
    table.AddCell(labelCell);

    PdfPCell valueCell = new PdfPCell(new Phrase(value, font));
    valueCell.Border = Rectangle.NO_BORDER;
    valueCell.Padding = 5;
    table.AddCell(valueCell);
}

        private void AddTableHeader(PdfPTable table, string text, iTextSharp.text.Font font)
        {
            // ✅ Créer une police blanche pour le texte
            var whiteFont = new iTextSharp.text.Font(font.BaseFont, font.Size, font.Style, BaseColor.WHITE);

            PdfPCell cell = new PdfPCell(new Phrase(text, whiteFont));
            cell.BackgroundColor = new BaseColor(25, 118, 210);
            cell.Padding = 8;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private void AddTableCell(PdfPTable table, string text, iTextSharp.text.Font font, int alignment = Element.ALIGN_LEFT)
{
    PdfPCell cell = new PdfPCell(new Phrase(text, font));
    cell.Padding = 5;
    cell.HorizontalAlignment = alignment;
    table.AddCell(cell);
}

private void AddTotalRow(PdfPTable table, string label, string value, iTextSharp.text.Font font)
{
    PdfPCell labelCell = new PdfPCell(new Phrase(label, font));
    labelCell.Border = Rectangle.NO_BORDER;
    labelCell.Padding = 5;
    table.AddCell(labelCell);

    PdfPCell valueCell = new PdfPCell(new Phrase(value, font));
    valueCell.Border = Rectangle.NO_BORDER;
    valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
    valueCell.Padding = 5;
    table.AddCell(valueCell);
}

/// <summary>
/// Exporte une facture en Excel
/// </summary>
private void ExportToExcel(Facture? facture)
{
    if (facture == null) return;

    try
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Fichier Excel (*.xlsx)|*.xlsx",
            FileName = $"Facture_{facture.NumeroFacture}_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Facture");

                // Titre
                worksheet.Cell(1, 1).Value = $"FACTURE N° {facture.NumeroFacture}";
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 4).Merge();
                worksheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Informations
                int row = 3;
                worksheet.Cell(row, 1).Value = "Date de facture:";
                worksheet.Cell(row, 2).Value = facture.DateFacture.ToString("dd/MM/yyyy");
                row++;

                worksheet.Cell(row, 1).Value = "Date d'échéance:";
                worksheet.Cell(row, 2).Value = facture.DateEcheance.ToString("dd/MM/yyyy");
                row++;

                worksheet.Cell(row, 1).Value = "Tiers:";
                worksheet.Cell(row, 2).Value = facture.Tiers?.Nom ?? "N/A";
                row++;

                worksheet.Cell(row, 1).Value = "Exercice:";
                worksheet.Cell(row, 2).Value = facture.Exercice?.Libelle ?? "N/A";
                row++;

                worksheet.Cell(row, 1).Value = "Statut:";
                worksheet.Cell(row, 2).Value = facture.Status.ToString().ToUpper();
                row++;

                if (facture.Contrats != null)
                {
                    worksheet.Cell(row, 1).Value = "Contrat:";
                    worksheet.Cell(row, 2).Value = facture.Contrats.NumeroContrat;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Description:";
                worksheet.Cell(row, 2).Value = facture.Description;
                row += 2;

                // En-têtes des détails
                worksheet.Cell(row, 1).Value = "Libellé";
                worksheet.Cell(row, 2).Value = "Quantité";
                worksheet.Cell(row, 3).Value = "Prix Unitaire";
                worksheet.Cell(row, 4).Value = "Montant Total";

                var headerRange = worksheet.Range(row, 1, row, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;

                // Détails
                if (facture.Details != null)
                {
                    foreach (var detail in facture.Details)
                    {
                        worksheet.Cell(row, 1).Value = detail.Libelle;
                        worksheet.Cell(row, 2).Value = detail.Quantite;
                        worksheet.Cell(row, 3).Value = detail.PrixUnitaire;
                        worksheet.Cell(row, 4).Value = detail.MontantTotal;
                        
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                        row++;
                    }
                }

                row++;

                // Totaux
                worksheet.Cell(row, 3).Value = "Montant HT:";
                worksheet.Cell(row, 4).Value = facture.MontantHT;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                row++;

                worksheet.Cell(row, 3).Value = $"TVA ({facture.TauxTVA}%):";
                worksheet.Cell(row, 4).Value = facture.MontantTTC - facture.MontantHT;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                row++;

                worksheet.Cell(row, 3).Value = "TOTAL TTC:";
                worksheet.Cell(row, 4).Value = facture.MontantTTC;
                worksheet.Cell(row, 3).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");

                // Ajuster les colonnes
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(saveFileDialog.FileName);

                MessageBox.Show($"Excel exporté avec succès :\n{saveFileDialog.FileName}", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Erreur lors de l'export Excel : {ex.Message}", 
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

/// <summary>
/// Imprime une facture
/// </summary>
private void Imprimer(Facture? facture)
{
    if (facture == null) return;

    try
    {
        // Créer un PDF temporaire
        string tempFile = Path.Combine(Path.GetTempPath(), $"Facture_{facture.NumeroFacture}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        // Réutiliser la méthode d'export PDF avec un chemin temporaire
        var saveFileDialog = new SaveFileDialog
        {
            FileName = tempFile
        };

        // Générer le PDF (code similaire à ExportToPdf mais sans dialog)
        Document document = new Document(PageSize.A4, 50, 50, 50, 50);
        PdfWriter.GetInstance(document, new FileStream(tempFile, FileMode.Create));
        
        document.Open();

        // ... (même code que ExportToPdf) ...
        // Pour simplifier, on peut appeler ExportToPdf et ensuite imprimer

        document.Close();

        // Imprimer le PDF
        ProcessStartInfo info = new ProcessStartInfo
        {
            Verb = "print",
            FileName = tempFile,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        };

        Process.Start(info);

        MessageBox.Show("Document envoyé à l'imprimante.", 
            "Impression", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Erreur lors de l'impression : {ex.Message}", 
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

#endregion

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                if (!CanViewFacture)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les factures.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Factures.Clear();
                    IsLoading = false;
                    return;
                }

                var factureService = new FactureService();
                var factures = await factureService.GetAllFacturesAsync();

                Factures.Clear();
                foreach (var f in factures)
                {
                    Factures.Add(f);
                }

                // Charger les données pour les listes
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }

                // ✅ Nouveau code (créer le contexte et le passer)
                using (var context = new AppDbContext())
                {
                    var exerciceService = new ExerciceService();
                    var exercices = await exerciceService.GetAllExerciceAsync();

                    Exercices.Clear();
                    foreach (var ex in exercices.Where(e => !e.EstCloture))
                    {
                        Exercices.Add(ex);
                    }
                }

                //// Charger les contrats
                //var contratService = new ContratsService();
                //var contrats = await contratService.GetAllContratsAsync();

                //ContratsList.Clear();
                //foreach (var c in contrats)
                //{
                //    ContratsList.Add(c);
                //}
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

        private void OpenAddDialog()
        {
            if (!CanCreateFacture)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des factures.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEditMode = false;

            DialogFacture = new Facture
            {
                DateFacture = DateTime.Now,
                DateEcheance = DateTime.Now.AddDays(30),
                Status = StatusFact.impayee,
                MontantHT = 0,
                TauxTVA = 18, // TVA par défaut
                MontantTTC = 0,
                NumeroFacture = $"FACT-{DateTime.Now:yyyyMMdd}-{Factures.Count + 1:D4}",
                Description = ""
            };

            DialogDetails.Clear();
            FichierName = string.Empty;

            IsDialogOpen = true;
        }

        private void OpenEditDialog(Facture? facture)
        {
            if (facture == null) return;
            if (!CanEditFacture)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier les factures.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEditMode = true;

            DialogFacture = new Facture
            {
                Id = facture.Id,
                NumeroFacture = facture.NumeroFacture,
                DateFacture = facture.DateFacture,
                MontantHT = facture.MontantHT,
                TauxTVA = facture.TauxTVA,
                MontantTTC = facture.MontantTTC,
                DateEcheance = facture.DateEcheance,
                Description = facture.Description,
                TiersId = facture.TiersId,
                ExerciceId = facture.ExerciceId,
                ContratId = facture.ContratId,
                Status = facture.Status,
                FichierJoin = facture.FichierJoin
            };

            DialogDetails.Clear();
            if (facture.Details != null)
            {
                foreach (var detail in facture.Details)
                {
                    DialogDetails.Add(new DetailsFacture
                    {
                        Id = detail.Id,
                        Libelle = detail.Libelle,
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire,
                        MontantTotal = detail.MontantTotal
                    });
                }
            }

            FichierName = facture.FichierJoin != null ? "Fichier existant" : string.Empty;

            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return DialogFacture != null &&
                   !string.IsNullOrWhiteSpace(DialogFacture.NumeroFacture) &&
                   DialogFacture.TiersId > 0 &&
                   DialogFacture.ExerciceId > 0 &&
                   DialogDetails.Count > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            // Vérifier permissions
            if (IsEditMode)
            {
                if (!CanEditFacture)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier les factures.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                if (!CanCreateFacture)
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des factures.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            IsLoading = true;

            try
            {
                // ✅ IMPORTANT : Recalculer tous les montants avant sauvegarde
                foreach (var detail in DialogDetails)
                {
                    detail.MontantTotal = detail.Quantite * detail.PrixUnitaire;
                }

                // Calculer le total et mettre à jour MontantHT
                DialogFacture.MontantHT = DialogDetails.Sum(d => d.MontantTotal);

                // Calculer automatiquement le MontantTTC
                DialogFacture.MontantTTC = MontantTTCCalcule;

                var factureService = new FactureService();
                var detailsList = DialogDetails.ToList();

                if (IsEditMode)
                {
                    var (success, message) = await factureService.UpdateFactureAsync(
                        DialogFacture, detailsList);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
                    }
                }
                else
                {
                    var (success, message, facture) = await factureService.CreateFactureAsync(
                        DialogFacture, detailsList);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
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

        private void CancelDialog()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteAsync(Facture? facture)
        {
            if (facture == null) return;

            if (!CanDeleteFacture)
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de supprimer les factures.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ Supprimer la facture '{facture.NumeroFacture}' ?\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var factureService = new FactureService();
                var (success, message) = await factureService.DeleteFactureAsync(facture.Id);

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

        private async System.Threading.Tasks.Task ChangeStatusAsync(Facture? facture)
        {
            if (facture == null) return;

            // Dialog pour choisir le nouveau statut
            var newStatus = facture.Status == StatusFact.impayee
                ? StatusFact.payee
                : StatusFact.impayee;

            var result = MessageBox.Show(
                $"Changer le statut de '{facture.NumeroFacture}' en '{newStatus}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var factureService = new FactureService();
                var (success, message) = await factureService.ChangeStatusAsync(facture.Id, newStatus);

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

        private void ChooseFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner un fichier",
                    Filter = "Tous les fichiers (*.*)|*.*|Documents PDF (*.pdf)|*.pdf",
                    FilterIndex = 2
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    if (fileBytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Le fichier est trop volumineux (max 5 MB).",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DialogFacture.FichierJoin = fileBytes;
                    FichierName = Path.GetFileName(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Gestion des détails
        private void AddDetail()
        {
            var newDetail = new DetailsFacture
            {
                Libelle = "",
                Quantite = 1,
                PrixUnitaire = 0,
                MontantTotal = 0
            };

            DialogDetails.Add(newDetail);
            OnPropertyChanged(nameof(TotalDetails));
        }

        private void RemoveDetail(DetailsFacture? detail)
        {
            if (detail != null)
            {
                DialogDetails.Remove(detail);
                OnPropertyChanged(nameof(TotalDetails));
            }
        }

        // ✅ Recalculer le MontantTotal d'un détail
        // ✅ Recalculer le MontantTotal d'un détail
        public void RecalculerMontantDetail(DetailsFacture detail)
        {
            if (detail != null)
            {
                // ✅ CALCUL AUTOMATIQUE
                detail.MontantTotal = detail.Quantite * detail.PrixUnitaire;

                OnPropertyChanged(nameof(TotalDetails));

                // Mettre à jour MontantHT de la facture
                DialogFacture.MontantHT = TotalDetails;
                OnPropertyChanged(nameof(DialogFacture));
                OnPropertyChanged(nameof(MontantTVA));
                OnPropertyChanged(nameof(MontantTTCCalcule));
            }
        }

        // ✅ Méthode à appeler quand Quantité ou PrixUnitaire change
        public void OnDetailPropertyChanged(DetailsFacture detail)
        {
            RecalculerMontantDetail(detail);
        }
        // ✅ Recalculer quand MontantHT ou TauxTVA change
        public void OnMontantsChanged()
        {
            OnPropertyChanged(nameof(MontantTVA));
            OnPropertyChanged(nameof(MontantTTCCalcule));
        }

        #endregion
    }
}