using ClosedXML.Excel;
using Collectivite.Models;
using Collectivite.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service d'export pour le Livre Journal
    /// </summary>
    public class LivreJournalExportService
    {
        private readonly CommuneService _communeService;
        // Couleurs du drapeau guinéen
        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";
        public LivreJournalExportService()
        {
            _communeService = new CommuneService();
        }

        #region Export Excel

        /// <summary>
        /// Exporte le livre journal en Excel avec format identique à la page WPF
        /// </summary>
        public async Task<string> ExportToExcel(int idCommune, List<EcritureComptable> ecritures, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"LivreJournal_{timestamp}.xlsx";
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Livre Journal");

            // Configuration de la page
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;

            // Récupérer les données de la commune
            var commune = await _communeService.GetCommuneByIdWithRelationsAsync(idCommune);
            var exercice = ExerciceService.Instance.CurrentExercice;

            string typeCommune = commune?.TypCommune ?? "URBAINE";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            int currentRow = 1;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE OFFICIEL
            // ═══════════════════════════════════════════════════════════

            worksheet.Cell(currentRow, 1).Value = "Ministère de l'Administration du Territoire et de la Décentralisation";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true).Font.SetFontSize(11);
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();

            worksheet.Cell(currentRow, 5).Value = "REPUBLIQUE GUINEE";
            worksheet.Cell(currentRow, 5).Style.Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Range(currentRow, 5, currentRow, 6).Merge();
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Direction Générale des Collectivités Locales";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true).Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();

            worksheet.Cell(currentRow, 5).Value = "Travail-Justice-Solidarité";
            worksheet.Cell(currentRow, 5).Style.Font.SetItalic(true).Font.SetFontSize(10)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Range(currentRow, 5, currentRow, 6).Merge();
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"REGION ADMINISTRATIVE DE {region.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"PREFECTURE DE {prefecture.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            currentRow += 2;

            // ═══════════════════════════════════════════════════════════
            // TITRE PRINCIPAL
            // ═══════════════════════════════════════════════════════════

            worksheet.Cell(currentRow, 1).Value = "LIVRE JOURNAL";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(18)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetTopBorder(XLBorderStyleValues.Medium)
                .Border.SetTopBorderColor(XLColor.FromHtml("#228B22"))
                .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                .Border.SetBottomBorderColor(XLColor.FromHtml("#228B22"));
            worksheet.Row(currentRow).Height = 25;
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = $"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(16)
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            if (dateDebut.HasValue || dateFin.HasValue)
            {
                string periode = "Période : ";
                if (dateDebut.HasValue && dateFin.HasValue)
                    periode += $"du {dateDebut.Value:dd/MM/yyyy} au {dateFin.Value:dd/MM/yyyy}";
                else if (dateDebut.HasValue)
                    periode += $"à partir du {dateDebut.Value:dd/MM/yyyy}";
                else if (dateFin.HasValue)
                    periode += $"jusqu'au {dateFin.Value:dd/MM/yyyy}";

                worksheet.Cell(currentRow, 1).Value = periode;
                worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                currentRow++;
            }

            worksheet.Cell(currentRow, 1).Value = $"Édité le {DateTime.Now:dd/MM/yyyy à HH:mm}";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(9)
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTES DU TABLEAU (format identique à la page WPF)
            // ═══════════════════════════════════════════════════════════

            int headerRow = currentRow;
            string[] headers = { "Débit", "Crédit", "Libellés", "Débit", "Crédit", "Date" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
            headerRange.Style
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1976D2"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            worksheet.Row(headerRow).Height = 30;
            currentRow++;

            // ═══════════════════════════════════════════════════════════
            // DONNÉES (2 lignes par écriture comme dans la page WPF)
            // ═══════════════════════════════════════════════════════════

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            var ecrituresTri = ecritures.OrderBy(e => e.DateEcriture).ThenBy(e => e.Id).ToList();
            int ecritureIndex = 0;

            foreach (var ecriture in ecrituresTri)
            {
                int startRow = currentRow;
                var bgColor = ecritureIndex % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F5F5F5");

                // ─────────────────────────────────────────────────────────
                // LIGNE 1 : Débit (compte + libellé + montant)
                // ─────────────────────────────────────────────────────────
                worksheet.Cell(currentRow, 1).Value = ecriture.CompteDebit?.NumeroCompte ?? "";
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                worksheet.Cell(currentRow, 2).Value = ""; // Vide pour crédit

                worksheet.Cell(currentRow, 3).Value = ecriture.CompteDebit?.IntituleCompte ?? "";

                worksheet.Cell(currentRow, 4).Value = ecriture.Montant;
                worksheet.Cell(currentRow, 4).Style
                    .NumberFormat.SetFormat("#,##0")
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.FromHtml("#388E3C"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                worksheet.Cell(currentRow, 5).Value = ""; // Vide pour crédit

                // Date (sera fusionnée avec la ligne 2)
                worksheet.Cell(currentRow, 6).Value = ecriture.DateEcriture.ToString("dd/MM/yyyy");
                worksheet.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Appliquer couleur de fond ligne 1
                worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.SetBackgroundColor(bgColor);
                currentRow++;

                // ─────────────────────────────────────────────────────────
                // LIGNE 2 : Crédit (compte + libellé + montant)
                // ─────────────────────────────────────────────────────────
                worksheet.Cell(currentRow, 1).Value = ""; // Vide pour débit

                worksheet.Cell(currentRow, 2).Value = ecriture.CompteCredit?.NumeroCompte ?? "";
                worksheet.Cell(currentRow, 2).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                worksheet.Cell(currentRow, 3).Value = ecriture.CompteCredit?.IntituleCompte ?? "";

                worksheet.Cell(currentRow, 4).Value = ""; // Vide pour débit

                worksheet.Cell(currentRow, 5).Value = ecriture.Montant;
                worksheet.Cell(currentRow, 5).Style
                    .NumberFormat.SetFormat("#,##0")
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.FromHtml("#D32F2F"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                worksheet.Cell(currentRow, 6).Value = ""; // Date déjà sur ligne 1

                // Appliquer couleur de fond ligne 2
                worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.SetBackgroundColor(bgColor);

                // ─────────────────────────────────────────────────────────
                // Fusionner la cellule Date sur les 2 lignes
                // ─────────────────────────────────────────────────────────
                worksheet.Range(startRow, 6, currentRow, 6).Merge();
                worksheet.Cell(startRow, 6).Style
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Bordures pour les 2 lignes de l'écriture
                worksheet.Range(startRow, 1, currentRow, 6).Style
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // Bordure de séparation entre écritures
                worksheet.Range(currentRow, 1, currentRow, 6).Style
                    .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                    .Border.SetBottomBorderColor(XLColor.FromHtml("#BDBDBD"));

                totalDebit += ecriture.Montant;
                totalCredit += ecriture.Montant;
                ecritureIndex++;
                currentRow++;
            }

            // ═══════════════════════════════════════════════════════════
            // TOTAUX
            // ═══════════════════════════════════════════════════════════

            currentRow++;
            worksheet.Cell(currentRow, 1).Value = "TOTAUX";
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 4).Value = totalDebit;
            worksheet.Cell(currentRow, 4).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.FromHtml("#388E3C"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 5).Value = totalCredit;
            worksheet.Cell(currentRow, 5).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.FromHtml("#D32F2F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Range(currentRow, 1, currentRow, 6).Style
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E3F2FD"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // ═══════════════════════════════════════════════════════════
            // ÉQUILIBRE
            // ═══════════════════════════════════════════════════════════

            currentRow += 2;
            decimal difference = totalDebit - totalCredit;
            bool isEquilibre = Math.Abs(difference) < 0.01m;

            worksheet.Cell(currentRow, 3).Value = "Différence :";
            worksheet.Cell(currentRow, 3).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 4).Value = difference;
            worksheet.Cell(currentRow, 4).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(isEquilibre ? XLColor.FromHtml("#388E3C") : XLColor.FromHtml("#D32F2F"));

            worksheet.Cell(currentRow, 5).Value = isEquilibre ? "✓ Équilibré" : "✗ Non équilibré";
            worksheet.Range(currentRow, 5, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 5).Style
                .Font.SetBold(true)
                .Font.SetFontColor(isEquilibre ? XLColor.FromHtml("#388E3C") : XLColor.FromHtml("#D32F2F"));

            // ═══════════════════════════════════════════════════════════
            // AJUSTEMENT DES COLONNES
            // ═══════════════════════════════════════════════════════════

            worksheet.Column(1).Width = 12;  // Débit (compte)
            worksheet.Column(2).Width = 12;  // Crédit (compte)
            worksheet.Column(3).Width = 40;  // Libellés
            worksheet.Column(4).Width = 15;  // Débit (montant)
            worksheet.Column(5).Width = 15;  // Crédit (montant)
            worksheet.Column(6).Width = 12;  // Date

            workbook.SaveAs(tempPath);
            return tempPath;
        }

        #endregion

        #region Export PDF

        /// <summary>
        /// Exporte le livre journal en PDF avec format identique à la page WPF
        /// </summary>
        public async Task<string> ExportToPdf(int idCommune, List<EcritureComptable> ecritures, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"LivreJournal_{timestamp}.pdf";
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);
            
            var commune = await _communeService.GetCommuneByIdWithRelationsAsync(idCommune);
            var exercice = ExerciceService.Instance.CurrentExercice;
            var ecrituresTri = ecritures.OrderBy(e => e.DateEcriture).ThenBy(e => e.Id).ToList();

            decimal totalDebit = ecritures.Sum(e => e.Montant);
            decimal totalCredit = ecritures.Sum(e => e.Montant);
            decimal difference = totalDebit - totalCredit;
            bool isEquilibre = Math.Abs(difference) < 0.01m;

            string typeCommune = commune.TypCommune;
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ═══════════════════════════════════════════════════════════
                    // EN-TÊTE OFFICIEL
                    // ═══════════════════════════════════════════════════════════
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            row.RelativeItem(2).Column(leftCol =>
                            {
                                leftCol.Item().Text("Ministère de l'Administration du Territoire")
                                    .FontSize(10).Bold();
                                leftCol.Item().Text("et de la Décentralisation")
                                    .FontSize(10).Bold();
                                leftCol.Item().PaddingTop(3).Text("Direction Générale des Collectivités Locales")
                                    .FontSize(9).Bold();
                                leftCol.Item().PaddingTop(8).Text($"REGION ADMINISTRATIVE DE {region.ToUpper()}")
                                    .FontSize(9);
                                leftCol.Item().Text($"PREFECTURE DE {prefecture.ToUpper()}")
                                    .FontSize(9);
                                leftCol.Item().Text($"COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                                    .FontSize(9);
                            });

                            // Colonne centrale : Drapeau guinéen
                            row.RelativeItem(1).AlignCenter().AlignMiddle().Element(c =>
                            {
                                c.Border(1).BorderColor("#424242").Table(flag =>
                                {
                                    flag.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(18);
                                        cols.ConstantColumn(18);
                                        cols.ConstantColumn(18);
                                    });

                                    flag.Cell().Height(36).Background(DrapeauRouge);
                                    flag.Cell().Height(36).Background(DrapeauJaune);
                                    flag.Cell().Height(36).Background(DrapeauVert);
                                });
                            });

                            row.RelativeItem(2).AlignRight().Column(rightCol =>
                            {
                                rightCol.Item().AlignRight().Text("REPUBLIQUE GUINEE")
                                    .FontSize(11).Bold();
                                rightCol.Item().AlignRight().Text("Travail-Justice-Solidarité")
                                    .FontSize(10).Italic();
                            });
                        });

                        headerCol.Item().PaddingTop(15);

                        headerCol.Item().AlignCenter().Text("LIVRE JOURNAL")
                            .FontSize(18).Bold();

                        headerCol.Item().PaddingVertical(5)
                            .BorderTop(2).BorderBottom(2).BorderColor(Colors.Green.Darken1)
                            .Padding(8).AlignCenter()
                            .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                            .FontSize(12);

                        headerCol.Item().PaddingTop(10).AlignCenter()
                            .Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                            .FontSize(14).Bold();

                        if (dateDebut.HasValue || dateFin.HasValue)
                        {
                            string periode = "Période : ";
                            if (dateDebut.HasValue && dateFin.HasValue)
                                periode += $"du {dateDebut.Value:dd/MM/yyyy} au {dateFin.Value:dd/MM/yyyy}";
                            else if (dateDebut.HasValue)
                                periode += $"à partir du {dateDebut.Value:dd/MM/yyyy}";
                            else if (dateFin.HasValue)
                                periode += $"jusqu'au {dateFin.Value:dd/MM/yyyy}";

                            headerCol.Item().AlignCenter().Text(periode).FontSize(10).Italic();
                        }

                        headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // ═══════════════════════════════════════════════════════════
                    // CONTENU (format identique à la page WPF)
                    // ═══════════════════════════════════════════════════════════
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            // Colonnes identiques à la page WPF
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);   // Débit (compte)
                                columns.ConstantColumn(60);   // Crédit (compte)
                                columns.RelativeColumn(3);    // Libellés
                                columns.ConstantColumn(80);   // Débit (montant)
                                columns.ConstantColumn(80);   // Crédit (montant)
                                columns.ConstantColumn(70);   // Date
                            });

                            // En-têtes
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Débit").FontColor(Colors.White).Bold().FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Crédit").FontColor(Colors.White).Bold().FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Libellés").FontColor(Colors.White).Bold().FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Débit").FontColor(Colors.White).Bold().FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Crédit").FontColor(Colors.White).Bold().FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).AlignCenter()
                                    .Text("Date").FontColor(Colors.White).Bold().FontSize(10);
                            });

                            // Données (2 lignes par écriture comme dans la page WPF)
                            int ecritureIndex = 0;
                            foreach (var ecriture in ecrituresTri)
                            {
                                var bgColor = ecritureIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                // ─────────────────────────────────────────────────────────
                                // LIGNE 1 : Débit
                                // ─────────────────────────────────────────────────────────

                                // Compte Débit
                                table.Cell().Background(bgColor).BorderBottom(0).Padding(4).AlignCenter()
                                    .Text(ecriture.CompteDebit?.NumeroCompte ?? "").Bold().FontSize(10);

                                // Compte Crédit (vide)
                                table.Cell().Background(bgColor).BorderBottom(0).Padding(4)
                                    .Text("");

                                // Libellé Débit
                                table.Cell().Background(bgColor).BorderBottom(0).Padding(4)
                                    .Text(ecriture.CompteDebit?.IntituleCompte ?? "").FontSize(9);

                                // Montant Débit
                                table.Cell().Background(bgColor).BorderBottom(0).Padding(4).AlignRight()
                                    .Text(ecriture.Montant.ToString("N0")).FontColor(Colors.Green.Darken2).Bold().FontSize(10);

                                // Montant Crédit (vide)
                                table.Cell().Background(bgColor).BorderBottom(0).Padding(4)
                                    .Text("");

                                // Date (sera sur ligne 1 avec RowSpan)
                                table.Cell().RowSpan(2).Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                    .Padding(4).AlignCenter().AlignMiddle()
                                    .Text(ecriture.DateEcriture.ToString("dd/MM/yyyy")).FontSize(9);

                                // ─────────────────────────────────────────────────────────
                                // LIGNE 2 : Crédit
                                // ─────────────────────────────────────────────────────────

                                // Compte Débit (vide)
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                                    .Text("");

                                // Compte Crédit
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignCenter()
                                    .Text(ecriture.CompteCredit?.NumeroCompte ?? "").Bold().FontSize(10);

                                // Libellé Crédit
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                                    .Text(ecriture.CompteCredit?.IntituleCompte ?? "").FontSize(9);

                                // Montant Débit (vide)
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                                    .Text("");

                                // Montant Crédit
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                                    .Text(ecriture.Montant.ToString("N0")).FontColor(Colors.Red.Darken2).Bold().FontSize(10);

                                ecritureIndex++;
                            }

                            // ─────────────────────────────────────────────────────────
                            // LIGNE DES TOTAUX
                            // ─────────────────────────────────────────────────────────
                            table.Cell().ColumnSpan(3).Background(Colors.Blue.Lighten4).Padding(8).AlignRight()
                                .Text("TOTAUX").Bold().FontSize(11);

                            table.Cell().Background(Colors.Blue.Lighten4).Padding(8).AlignRight()
                                .Text(totalDebit.ToString("N0")).FontColor(Colors.Green.Darken2).Bold().FontSize(11);

                            table.Cell().Background(Colors.Blue.Lighten4).Padding(8).AlignRight()
                                .Text(totalCredit.ToString("N0")).FontColor(Colors.Red.Darken2).Bold().FontSize(11);

                            table.Cell().Background(Colors.Blue.Lighten4).Padding(8)
                                .Text("");
                        });

                        // Encadré Équilibre
                        col.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem();

                            var borderColor = isEquilibre ? Colors.Green.Darken1 : Colors.Red.Darken1;
                            var bgColorEquilibre = isEquilibre ? Colors.Green.Lighten4 : Colors.Red.Lighten4;
                            var textColor = isEquilibre ? Colors.Green.Darken2 : Colors.Red.Darken2;

                            row.ConstantItem(300).Border(1).BorderColor(borderColor)
                                .Background(bgColorEquilibre).Padding(10).Column(eqCol =>
                                {
                                    eqCol.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Différence :").Bold();
                                        r.ConstantItem(100).AlignRight()
                                            .Text(difference.ToString("N0")).FontColor(textColor).Bold();
                                    });

                                    string statusText = isEquilibre ? "✓ Journal Équilibré" : "✗ Journal Non Équilibré";
                                    eqCol.Item().PaddingTop(5).AlignCenter()
                                        .Text(statusText).FontSize(12).Bold().FontColor(textColor);
                                });
                        });
                    });

                    // ═══════════════════════════════════════════════════════════
                    // PIED DE PAGE
                    // ═══════════════════════════════════════════════════════════
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text($"Édité le {DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(8).Italic();

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8));
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });

                        row.RelativeItem().AlignRight()
                            .Text($"Nombre d'écritures : {ecritures.Count}").FontSize(8).Italic();
                    });
                });
            });

            document.GeneratePdf(tempPath);
            return tempPath;
        }

        #endregion

        #region Méthodes utilitaires

        public void OpenFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var process = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            Process.Start(process);
        }

        public void PrintPreview(string filePath)
        {
            OpenFile(filePath);
        }

        #endregion
    }
}