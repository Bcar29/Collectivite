using ClosedXML.Excel;
using Collectivite.Models;
using Collectivite.Services;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Collectivite.Services
{
    /// <summary>
    /// Service d'export pour le Livre Journal
    /// </summary>
    public class LivreJournalExportService
    {

        #region Export Excel

        /// <summary>
        /// Exporte le livre journal en Excel
        /// </summary>
        public string ExportToExcel(List<EcritureComptable> ecritures, DateTime? dateDebut = null, DateTime? dateFin = null)
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
            int idCommune = Properties.Settings.Default.CommuneId;
            var commune = CommuneService.Instance.CurrentCommune;
            var exercice = ExerciceService.Instance.CurrentExercice;

            // ✅ Utilisation des propriétés calculées
            string typeCommune = commune?.TypCommune ?? "URBAINE";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            int currentRow = 1;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE OFFICIEL
            // ═══════════════════════════════════════════════════════════

            // Ligne 1 : Ministère (gauche) et République (droite)
            worksheet.Cell(currentRow, 1).Value = "Ministère de l'Administration du Territoire et de la Décentralisation";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true).Font.SetFontSize(11);
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();

            worksheet.Cell(currentRow, 6).Value = "REPUBLIQUE GUINEE";
            worksheet.Cell(currentRow, 6).Style.Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Range(currentRow, 6, currentRow, 7).Merge();
            currentRow++;

            // Ligne 2 : Direction et Devise
            worksheet.Cell(currentRow, 1).Value = "Direction Générale des Collectivités Locales";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true).Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();

            worksheet.Cell(currentRow, 6).Value = "Travail-Justice-Solidarité";
            worksheet.Cell(currentRow, 6).Style.Font.SetItalic(true).Font.SetFontSize(10)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            worksheet.Range(currentRow, 6, currentRow, 7).Merge();
            currentRow++;

            // Ligne 3 : Région
            worksheet.Cell(currentRow, 1).Value = $"REGION ADMINISTRATIVE DE {region.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            currentRow++;

            // Ligne 4 : Préfecture
            worksheet.Cell(currentRow, 1).Value = $"PREFECTURE DE {prefecture.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            currentRow++;

            // Ligne 5 : Commune
            worksheet.Cell(currentRow, 1).Value = $"COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            worksheet.Cell(currentRow, 1).Style.Font.SetFontSize(10);
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            currentRow += 2;

            // ═══════════════════════════════════════════════════════════
            // TITRE PRINCIPAL
            // ═══════════════════════════════════════════════════════════

            // Titre : LIVRE JOURNAL
            worksheet.Cell(currentRow, 1).Value = "LIVRE JOURNAL";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(18)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Sous-titre avec bordures vertes : DE LA COMMUNE ... DE ...
            worksheet.Cell(currentRow, 1).Value = $"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetTopBorder(XLBorderStyleValues.Medium)
                .Border.SetTopBorderColor(XLColor.FromHtml("#228B22"))
                .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                .Border.SetBottomBorderColor(XLColor.FromHtml("#228B22"));
            worksheet.Row(currentRow).Height = 25;
            currentRow += 2;

            // Exercice
            worksheet.Cell(currentRow, 1).Value = $"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(16)
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Période si filtres appliqués
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
                worksheet.Range(currentRow, 1, currentRow, 7).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                currentRow++;
            }

            // Date d'édition
            worksheet.Cell(currentRow, 1).Value = $"Édité le {DateTime.Now:dd/MM/yyyy à HH:mm}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(9)
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTES DU TABLEAU
            // ═══════════════════════════════════════════════════════════

            int headerRow = currentRow;
            string[] headers = { "Date", "Compte Débit", "Compte Crédit", "Libellé Débit", "Libellé Crédit", "Débit", "Crédit" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 7);
            headerRange.Style
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1976D2"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            worksheet.Row(headerRow).Height = 25;
            currentRow++;

            // ═══════════════════════════════════════════════════════════
            // DONNÉES
            // ═══════════════════════════════════════════════════════════

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            var ecrituresTri = ecritures.OrderBy(e => e.DateEcriture).ThenBy(e => e.Id).ToList();

            foreach (var ecriture in ecrituresTri)
            {
                worksheet.Cell(currentRow, 1).Value = ecriture.DateEcriture.ToString("dd/MM/yyyy");
                worksheet.Cell(currentRow, 2).Value = ecriture.CompteDebit?.NumeroCompte ?? "";
                worksheet.Cell(currentRow, 3).Value = ecriture.CompteCredit?.NumeroCompte ?? "";
                worksheet.Cell(currentRow, 4).Value = ecriture.CompteDebit?.IntituleCompte ?? "";
                worksheet.Cell(currentRow, 5).Value = ecriture.CompteCredit?.IntituleCompte ?? "";
                worksheet.Cell(currentRow, 6).Value = ecriture.Montant;
                worksheet.Cell(currentRow, 7).Value = ecriture.Montant;

                worksheet.Cell(currentRow, 6).Style
                    .NumberFormat.SetFormat("#,##0")
                    .Font.SetFontColor(XLColor.FromHtml("#388E3C"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                worksheet.Cell(currentRow, 7).Style
                    .NumberFormat.SetFormat("#,##0")
                    .Font.SetFontColor(XLColor.FromHtml("#D32F2F"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                worksheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                worksheet.Cell(currentRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold(true);
                worksheet.Cell(currentRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold(true);

                worksheet.Range(currentRow, 1, currentRow, 7).Style
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                if ((currentRow - headerRow) % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 7).Style
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5"));
                }

                totalDebit += ecriture.Montant;
                totalCredit += ecriture.Montant;
                currentRow++;
            }

            // ═══════════════════════════════════════════════════════════
            // TOTAUX
            // ═══════════════════════════════════════════════════════════

            currentRow++;
            worksheet.Cell(currentRow, 1).Value = "TOTAUX";
            worksheet.Range(currentRow, 1, currentRow, 5).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 6).Value = totalDebit;
            worksheet.Cell(currentRow, 6).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.FromHtml("#388E3C"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 7).Value = totalCredit;
            worksheet.Cell(currentRow, 7).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.FromHtml("#D32F2F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Range(currentRow, 1, currentRow, 7).Style
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E3F2FD"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // ═══════════════════════════════════════════════════════════
            // ÉQUILIBRE
            // ═══════════════════════════════════════════════════════════

            currentRow += 2;
            decimal difference = totalDebit - totalCredit;
            bool isEquilibre = Math.Abs(difference) < 0.01m;

            worksheet.Cell(currentRow, 5).Value = "Différence :";
            worksheet.Cell(currentRow, 5).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            worksheet.Cell(currentRow, 6).Value = difference;
            worksheet.Cell(currentRow, 6).Style
                .NumberFormat.SetFormat("#,##0")
                .Font.SetBold(true)
                .Font.SetFontColor(isEquilibre ? XLColor.FromHtml("#388E3C") : XLColor.FromHtml("#D32F2F"));

            worksheet.Cell(currentRow, 7).Value = isEquilibre ? "✓ Équilibré" : "✗ Non équilibré";
            worksheet.Cell(currentRow, 7).Style
                .Font.SetBold(true)
                .Font.SetFontColor(isEquilibre ? XLColor.FromHtml("#388E3C") : XLColor.FromHtml("#D32F2F"));

            // ═══════════════════════════════════════════════════════════
            // AJUSTEMENT DES COLONNES
            // ═══════════════════════════════════════════════════════════

            worksheet.Column(1).Width = 12;
            worksheet.Column(2).Width = 14;
            worksheet.Column(3).Width = 14;
            worksheet.Column(4).Width = 30;
            worksheet.Column(5).Width = 30;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;

            workbook.SaveAs(tempPath);
            return tempPath;
        }

        #endregion

        #region Export PDF

        /// <summary>
        /// Exporte le livre journal en PDF
        /// </summary>
        public string ExportToPdf(List<EcritureComptable> ecritures, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"LivreJournal_{timestamp}.pdf";
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);

            var commune = CommuneService.Instance.CurrentCommune;
            var exercice = ExerciceService.Instance.CurrentExercice;
            var ecrituresTri = ecritures.OrderBy(e => e.DateEcriture).ThenBy(e => e.Id).ToList();

            decimal totalDebit = ecritures.Sum(e => e.Montant);
            decimal totalCredit = ecritures.Sum(e => e.Montant);
            decimal difference = totalDebit - totalCredit;
            bool isEquilibre = Math.Abs(difference) < 0.01m;

            // ✅ Utilisation des propriétés calculées
            string typeCommune = commune?.TypCommune ?? "";
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
                            // Ligne 1 : Ministère et République
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
                                    leftCol.Item().PaddingTop(8).Text($"REGION ADMINISTRATIVE DE {region}")
                                        .FontSize(9);
                                    leftCol.Item().Text($"PREFECTURE DE {prefecture}")
                                        .FontSize(9);
                                    leftCol.Item().Text($"COMMUNE {typeCommune.ToUpper()} DE {nomCommune}")
                                        .FontSize(9);
                                });

                                row.RelativeItem(1).AlignCenter().AlignMiddle().Text("⚜")
                                    .FontSize(30);

                                row.RelativeItem(2).AlignRight().Column(rightCol =>
                                {
                                    rightCol.Item().AlignRight().Text("REPUBLIQUE GUINEE")
                                        .FontSize(11).Bold();
                                    rightCol.Item().AlignRight().Text("Travail-Justice-Solidarité")
                                        .FontSize(10).Italic();
                                });
                            });

                            // Espacement
                            headerCol.Item().PaddingTop(15);

                            // Titre : LIVRE JOURNAL
                            headerCol.Item().AlignCenter().Text("LIVRE JOURNAL")
                                .FontSize(18).Bold();

                            // Sous-titre avec bordures vertes
                            headerCol.Item().PaddingVertical(5)
                                .BorderTop(2).BorderBottom(2).BorderColor(Colors.Green.Darken1)
                                .Padding(8).AlignCenter()
                                .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune}")
                                .FontSize(12);

                            // Exercice
                            headerCol.Item().PaddingTop(10).AlignCenter()
                                .Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                                .FontSize(14).Bold();

                            // Période si filtres
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
                        // CONTENU
                        // ═══════════════════════════════════════════════════════════
                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(70);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5)
                                        .Text("Date").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5)
                                        .Text("Débit").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5)
                                        .Text("Crédit").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5)
                                        .Text("Libellé Débit").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5)
                                        .Text("Libellé Crédit").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5).AlignRight()
                                        .Text("Débit").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background(Colors.Blue.Darken1).Padding(5).AlignRight()
                                        .Text("Crédit").FontColor(Colors.White).Bold().FontSize(9);
                                });

                                int rowIndex = 0;
                                foreach (var ecriture in ecrituresTri)
                                {
                                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(ecriture.DateEcriture.ToString("dd/MM/yyyy")).FontSize(9);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(ecriture.CompteDebit?.NumeroCompte ?? "").Bold().FontSize(9);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(ecriture.CompteCredit?.NumeroCompte ?? "").Bold().FontSize(9);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(ecriture.CompteDebit?.IntituleCompte ?? "").FontSize(8);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(ecriture.CompteCredit?.IntituleCompte ?? "").FontSize(8);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                        .Text(ecriture.Montant.ToString("N0")).FontColor(Colors.Green.Darken2).Bold().FontSize(9);

                                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                        .Text(ecriture.Montant.ToString("N0")).FontColor(Colors.Red.Darken2).Bold().FontSize(9);

                                    rowIndex++;
                                }

                                table.Cell().ColumnSpan(5).Background(Colors.Blue.Lighten4).Padding(6).AlignRight()
                                    .Text("TOTAUX").Bold().FontSize(10);

                                table.Cell().Background(Colors.Blue.Lighten4).Padding(6).AlignRight()
                                    .Text(totalDebit.ToString("N0")).FontColor(Colors.Green.Darken2).Bold().FontSize(10);

                                table.Cell().Background(Colors.Blue.Lighten4).Padding(6).AlignRight()
                                    .Text(totalCredit.ToString("N0")).FontColor(Colors.Red.Darken2).Bold().FontSize(10);
                            });

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