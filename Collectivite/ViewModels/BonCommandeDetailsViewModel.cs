using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectivite.ViewModels
{
    public class BonCommandeDetailsViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande? _bonCommande;
        private int _bonCommandeId;
        private Commune _commune;

        // Service d'export PDF
        private readonly BonCommandePdfExporter _pdfExporter;

        public BonCommandeDetailsViewModel(int bonCommandeId)
        {
            _bonCommandeId = bonCommandeId;
            _pdfExporter = new BonCommandePdfExporter();

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            GoBackCommand = new RelayCommand(_ => GoBack());
            PrintCommand = new RelayCommand(_ => Print(), _ => CanExport());
            ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync(), _ => CanExport());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<DetailBonCommande> Details { get; } = new();
        //public ObservableCollection<Engagement> Engagements { get; } = new();

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
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les infos de la commune avec relations
                var communeService = new CommuneService();
                var commune = await communeService.GetCommuneByIdWithRelationsAsync(
                    Properties.Settings.Default.CommuneId
                );

                var service = new BonCommandeService();
                var bonCommande = await service.GetBonCommandeByIdAsync(_bonCommandeId);

                if (bonCommande != null)
                {
                    Commune = commune?? new Commune();
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
                    //Engagements.Clear();
                    //if (bonCommande.Engagements != null)
                    //{
                    //    foreach (var engagement in bonCommande.Engagements)
                    //    {
                    //        Engagements.Add(engagement);
                    //    }
                    //}

                    OnPropertyChanged(nameof(MontantTotal));
                }
                else
                {
                    NotificationService.ShowWarning("Bon de commande introuvable.");
                    GoBack();
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

        /// <summary>
        /// Vérifie si l'export/impression est possible
        /// </summary>
        private bool CanExport()
        {
            return BonCommande != null && !IsLoading;
        }

        /// <summary>
        /// Exporte le bon de commande en PDF
        /// </summary>
        private async System.Threading.Tasks.Task ExportPdfAsync()
        {
            if (BonCommande == null)
            {
                NotificationService.ShowWarning("Aucun bon de commande à exporter.");
                return;
            }

            try
            {
                // Boîte de dialogue pour choisir l'emplacement
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Title = "Exporter le Bon de Commande en PDF",
                    Filter = "Fichiers PDF (*.pdf)|*.pdf",
                    FileName = $"BonCommande_{BonCommande.Numero}_{DateTime.Now:yyyyMMdd}.pdf",
                    DefaultExt = ".pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // Exporter le PDF
                    byte[] pdfBytes = await _pdfExporter.ExporterAsync(
                        BonCommande,
                        Commune,
                        Details.ToList()
                    );

                    // Sauvegarder le fichier
                    await System.IO.File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);

                    // Demander si l'utilisateur veut ouvrir le fichier
                    var result = MessageBox.Show(
                        $"Export PDF réussi !\n\nFichier enregistré : {saveDialog.FileName}\n\nVoulez-vous ouvrir le fichier ?",
                        "Export terminé",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
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

        /// <summary>
        /// Imprime le bon de commande
        /// </summary>
        private void Print()
        {
            if (BonCommande == null)
            {
                NotificationService.ShowWarning("Aucun bon de commande à imprimer.");
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // Créer le FlowDocument pour l'impression
                    FlowDocument document = CreatePrintDocument();
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PageWidth = printDialog.PrintableAreaWidth;
                    document.PagePadding = new Thickness(40);
                    document.ColumnWidth = double.PositiveInfinity;

                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, $"Bon de Commande N° {BonCommande.Numero}");
                }
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

        /// <summary>
        /// Crée le FlowDocument pour l'impression
        /// </summary>
        private FlowDocument CreatePrintDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Arial");
            doc.FontSize = 11;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE OFFICIEL
            // ═══════════════════════════════════════════════════════════
            Table headerTable = new Table();
            headerTable.CellSpacing = 0;
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(250) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(250) });

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();

            // Cellule gauche - Ministère
            TableCell leftCell = new TableCell();
            leftCell.Blocks.Add(new Paragraph(new Bold(new Run("Ministère de l'Administration du Territoire"))) { FontSize = 9, Margin = new Thickness(0) });
            leftCell.Blocks.Add(new Paragraph(new Bold(new Run("et de la Décentralisation"))) { FontSize = 9, Margin = new Thickness(0) });
            leftCell.Blocks.Add(new Paragraph(new Run("Direction Générale des Collectivités Locales")) { FontSize = 8, Margin = new Thickness(0, 5, 0, 0) });

            Paragraph geoPara = new Paragraph { FontSize = 8, Margin = new Thickness(0, 10, 0, 0) };
            geoPara.Inlines.Add(new Run("REGION ADMINISTRATIVE DE "));
            geoPara.Inlines.Add(new Bold(new Run(Commune?.RegionCommune ?? "...")));
            geoPara.Inlines.Add(new LineBreak());
            geoPara.Inlines.Add(new Run("PREFECTURE DE "));
            geoPara.Inlines.Add(new Bold(new Run(Commune?.PrefectureCommune ?? "...")));
            geoPara.Inlines.Add(new LineBreak());
            geoPara.Inlines.Add(new Run("COMMUNE "));
            geoPara.Inlines.Add(new Bold(new Run(Commune?.TypCommune ?? "...")));
            geoPara.Inlines.Add(new Run(" DE "));
            geoPara.Inlines.Add(new Bold(new Run(Commune?.NomCommune ?? "...")));
            leftCell.Blocks.Add(geoPara);
            headerRow.Cells.Add(leftCell);

            // Cellule centrale vide (pour le drapeau, non imprimable facilement)
            TableCell centerCell = new TableCell();
            centerCell.TextAlignment = TextAlignment.Center;
            headerRow.Cells.Add(centerCell);

            // Cellule droite - République
            TableCell rightCell = new TableCell();
            rightCell.TextAlignment = TextAlignment.Right;
            rightCell.Blocks.Add(new Paragraph(new Bold(new Run("REPUBLIQUE DE GUINEE"))) { FontSize = 10, Margin = new Thickness(0) });
            rightCell.Blocks.Add(new Paragraph(new Italic(new Run("Travail - Justice - Solidarité"))) { FontSize = 9, Margin = new Thickness(0) });
            headerRow.Cells.Add(rightCell);

            headerGroup.Rows.Add(headerRow);
            headerTable.RowGroups.Add(headerGroup);
            doc.Blocks.Add(headerTable);

            // ═══════════════════════════════════════════════════════════
            // TITRE
            // ═══════════════════════════════════════════════════════════
            Paragraph title = new Paragraph(new Bold(new Run("BON DE COMMANDE")));
            title.FontSize = 18;
            title.TextAlignment = TextAlignment.Center;
            title.Margin = new Thickness(0, 20, 0, 10);
            doc.Blocks.Add(title);

            // Bandeau commune
            Paragraph bandeau = new Paragraph();
            bandeau.TextAlignment = TextAlignment.Center;
            bandeau.Background = new SolidColorBrush(Color.FromRgb(209, 250, 229));
            bandeau.Padding = new Thickness(10);
            bandeau.FontWeight = FontWeights.SemiBold;
            bandeau.Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70));
            bandeau.Inlines.Add(new Run($"DE LA COMMUNE {Commune?.TypCommune} DE {Commune?.NomCommune}"));
            doc.Blocks.Add(bandeau);

            // Exercice
            Paragraph exercice = new Paragraph(new Bold(new Run(BonCommande?.ExpressionBesoin?.Exercice?.Libelle ?? DateTime.Now.Year.ToString())));
            exercice.TextAlignment = TextAlignment.Center;
            exercice.FontSize = 12;
            exercice.Margin = new Thickness(0, 10, 0, 20);
            doc.Blocks.Add(exercice);

            // ═══════════════════════════════════════════════════════════
            // INFORMATIONS GÉNÉRALES
            // ═══════════════════════════════════════════════════════════
            doc.Blocks.Add(CreateSectionTitle("Informations Générales"));
            doc.Blocks.Add(CreateInfoParagraph("N° Bon de Commande", BonCommande?.Numero ?? "-"));
            doc.Blocks.Add(CreateInfoParagraph("Date de Création", BonCommande?.DateCreation.ToString("dd/MM/yyyy") ?? "-"));
            doc.Blocks.Add(CreateInfoParagraph("Expression de Besoin", BonCommande?.ExpressionBesoin?.Numero ?? "-"));
            doc.Blocks.Add(CreateInfoParagraph("Exercice", BonCommande?.ExpressionBesoin?.Exercice?.Libelle ?? "-"));

            // ═══════════════════════════════════════════════════════════
            // TABLEAU DES DÉTAILS
            // ═══════════════════════════════════════════════════════════
            doc.Blocks.Add(CreateSectionTitle("Détails du Bon de Commande"));

            if (Details.Any())
            {
                Table detailsTable = new Table();
                detailsTable.CellSpacing = 0;
                detailsTable.BorderBrush = Brushes.Gray;
                detailsTable.BorderThickness = new Thickness(1);

                detailsTable.Columns.Add(new TableColumn { Width = new GridLength(40) });
                detailsTable.Columns.Add(new TableColumn { Width = new GridLength(250) });
                detailsTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
                detailsTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
                detailsTable.Columns.Add(new TableColumn { Width = new GridLength(100) });

                TableRowGroup tableGroup = new TableRowGroup();

                // En-tête du tableau
                TableRow headerRowTable = new TableRow();
                headerRowTable.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                headerRowTable.Foreground = Brushes.White;

                headerRowTable.Cells.Add(CreateTableCell("#", true));
                headerRowTable.Cells.Add(CreateTableCell("Désignation", true));
                headerRowTable.Cells.Add(CreateTableCell("Qté", true));
                headerRowTable.Cells.Add(CreateTableCell("Prix Unit.", true));
                headerRowTable.Cells.Add(CreateTableCell("Total", true));

                tableGroup.Rows.Add(headerRowTable);

                // Lignes de données
                int index = 1;
                foreach (var detail in Details)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Background = index % 2 == 0 ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : Brushes.White;

                    dataRow.Cells.Add(CreateTableCell(index.ToString(), false, TextAlignment.Center));
                    dataRow.Cells.Add(CreateTableCell(detail.Designation ?? "-", false));
                    dataRow.Cells.Add(CreateTableCell(detail.Quantite.ToString(), false, TextAlignment.Center));
                    dataRow.Cells.Add(CreateTableCell($"{detail.PrixUnitaire:N0} GNF", false, TextAlignment.Right));
                    dataRow.Cells.Add(CreateTableCell($"{detail.Total:N0} GNF", false, TextAlignment.Right));

                    tableGroup.Rows.Add(dataRow);
                    index++;
                }

                detailsTable.RowGroups.Add(tableGroup);
                doc.Blocks.Add(detailsTable);

                // Montant total
                Paragraph totalPara = new Paragraph();
                totalPara.TextAlignment = TextAlignment.Right;
                totalPara.Margin = new Thickness(0, 15, 0, 0);
                totalPara.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                totalPara.Padding = new Thickness(15);
                totalPara.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                totalPara.BorderThickness = new Thickness(2);
                totalPara.Inlines.Add(new Bold(new Run("MONTANT TOTAL : ")));
                totalPara.Inlines.Add(new Bold(new Run($"{MontantTotal:N0} GNF") { FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60)) }));
                doc.Blocks.Add(totalPara);
            }

            // ═══════════════════════════════════════════════════════════
            // SIGNATURE
            // ═══════════════════════════════════════════════════════════
            Paragraph signature = new Paragraph();
            signature.Margin = new Thickness(0, 40, 0, 0);
            signature.TextAlignment = TextAlignment.Right;
            signature.Inlines.Add(new Run($"{Commune?.NomCommune} Le ....../....../......"));
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new Bold(new Run("L'Ordonateur :")));
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new Run("M./Mme : ........................................."));
            doc.Blocks.Add(signature);

            return doc;
        }

        private Paragraph CreateSectionTitle(string title)
        {
            Paragraph para = new Paragraph(new Bold(new Run(title)));
            para.FontSize = 13;
            para.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            para.Margin = new Thickness(0, 15, 0, 8);
            para.BorderBrush = Brushes.LightGray;
            para.BorderThickness = new Thickness(0, 0, 0, 1);
            return para;
        }

        private Paragraph CreateInfoParagraph(string label, string value)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(0, 2, 0, 2);
            para.Inlines.Add(new Bold(new Run($"{label} : ")));
            para.Inlines.Add(new Run(value ?? ""));
            return para;
        }

        private TableCell CreateTableCell(string text, bool isHeader, TextAlignment alignment = TextAlignment.Left)
        {
            TableCell cell = new TableCell();
            cell.BorderBrush = Brushes.LightGray;
            cell.BorderThickness = new Thickness(0, 0, 1, 1);
            cell.Padding = new Thickness(5);

            Paragraph para = new Paragraph(isHeader ? new Bold(new Run(text)) : new Run(text));
            para.TextAlignment = alignment;
            para.FontSize = isHeader ? 9 : 10;
            cell.Blocks.Add(para);

            return cell;
        }

        private void GoBack()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}