using ClosedXML.Excel;
using Collectivite.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class TiersExportService
    {
        private static readonly Color HeaderBackground = Color.FromHex("#E3F2FD");
        private static readonly Color HeaderText = Color.FromHex("#0F172A");
        private static readonly Color BorderColor = Color.FromHex("#E2E8F0");

        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";

        public Task<byte[]> ExportDebiteursExcelAsync(List<TiersDebiteurDTO> debiteurs, TiersFiltreDTO? filtre = null)
        {
            return Task.FromResult(ExporterExcel(
                "Débiteurs",
                filtre,
                new[]
                {
                    "Nom / Raison Sociale","Type","Téléphone","Mandats",
                    "Total à payer","Total payé","Reste à payer","Taux","Statut","Dernier paiement"
                },
                debiteurs.Select(d => new object?[]
                {
                    d.NomComplet,
                    d.TypeTiers,
                    d.Telephone ?? "-",
                    d.NombreMandats,
                    d.TotalMontantAPayer,
                    d.TotalMontantPaye,
                    d.ResteAPayer,
                    d.TauxPaiementFormate,
                    d.Statut,
                    d.DateDernierPaiementFormate
                }).ToList(),
                new[] { 5, 6, 7 }
            ));
        }

        public Task<byte[]> ExportCreanciersExcelAsync(List<TiersCreancierDTO> creanciers, TiersFiltreDTO? filtre = null)
        {
            return Task.FromResult(ExporterExcel(
                "Créanciers",
                filtre,
                new[]
                {
                    "Nom / Raison Sociale","Type","Téléphone","Ordres",
                    "Total à encaisser","Total encaissé","Reste à encaisser","Taux","Statut","Dernier encaissement"
                },
                creanciers.Select(c => new object?[]
                {
                    c.NomComplet,
                    c.TypeTiers,
                    c.Telephone ?? "-",
                    c.NombreOrdresRecette,
                    c.TotalMontantAEncaisser,
                    c.TotalMontantEncaisse,
                    c.ResteAEncaisser,
                    c.TauxEncaissementFormate,
                    c.Statut,
                    c.DateDernierEncaissementFormate
                }).ToList(),
                new[] { 5, 6, 7 }
            ));
        }

        public async Task<byte[]> ExportDebiteursPdfAsync(List<TiersDebiteurDTO> debiteurs, TiersFiltreDTO? filtre = null)
        {
            return await ExporterPdfAsync(
                "Débiteurs",
                filtre,
                new[]
                {
                    "Nom / Raison Sociale","Type","Téléphone","Mandats",
                    "Total à payer","Total payé","Reste à payer","Taux","Statut","Dernier paiement"
                },
                debiteurs.Select(d => new[]
                {
                    d.NomComplet,
                    d.TypeTiers,
                    d.Telephone ?? "-",
                    d.NombreMandats.ToString(),
                    d.TotalMontantAPayerFormate,
                    d.TotalMontantPayeFormate,
                    d.ResteAPayerFormate,
                    d.TauxPaiementFormate,
                    d.Statut,
                    d.DateDernierPaiementFormate
                }).ToList());
        }

        public async Task<byte[]> ExportCreanciersPdfAsync(List<TiersCreancierDTO> creanciers, TiersFiltreDTO? filtre = null)
        {
            return await ExporterPdfAsync(
                "Créanciers",
                filtre,
                new[]
                {
                    "Nom / Raison Sociale","Type","Téléphone","Ordres",
                    "Total à encaisser","Total encaissé","Reste à encaisser","Taux","Statut","Dernier encaissement"
                },
                creanciers.Select(c => new[]
                {
                    c.NomComplet,
                    c.TypeTiers,
                    c.Telephone ?? "-",
                    c.NombreOrdresRecette.ToString(),
                    c.TotalMontantAEncaisserFormate,
                    c.TotalMontantEncaisseFormate,
                    c.ResteAEncaisserFormate,
                    c.TauxEncaissementFormate,
                    c.Statut,
                    c.DateDernierEncaissementFormate
                }).ToList());
        }

        private static byte[] ExporterExcel(string titre, TiersFiltreDTO? filtre, string[] entetes, List<object?[]> lignes, int[] colonnesMontant)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(titre);

            int row = 1;
            ws.Cell(row, 1).Value = $"Liste des {titre}";
            ws.Range(row, 1, row, entetes.Length).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row++;
            ws.Cell(row, 1).Value = ConstruireFiltreResume(filtre);
            ws.Range(row, 1, row, entetes.Length).Merge().Style
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.FromHtml("#64748B"));

            row += 2;
            for (int i = 0; i < entetes.Length; i++)
            {
                ws.Cell(row, i + 1).Value = entetes[i];
            }

            var headerRange = ws.Range(row, 1, row, entetes.Length);
            headerRange.Style.Font.SetBold();
            headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E3F2FD"));
            headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            headerRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            row++;
            foreach (var ligne in lignes)
            {
                for (int i = 0; i < entetes.Length; i++)
                {
                    var value = i < ligne.Length ? ligne[i] ?? string.Empty : string.Empty;
                    SetCellValue(ws.Cell(row, i + 1), value);
                }
                row++;
            }

            var dataRange = ws.Range(4, 1, Math.Max(4, row - 1), entetes.Length);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            foreach (var col in colonnesMontant)
            {
                ws.Column(col).Style.NumberFormat.Format = "#,##0";
                ws.Column(col).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetCellValue(IXLCell cell, object value)
        {
            switch (value)
            {
                case int intValue:
                    cell.SetValue(intValue);
                    break;
                case long longValue:
                    cell.SetValue(longValue);
                    break;
                case decimal decimalValue:
                    cell.SetValue(decimalValue);
                    break;
                case double doubleValue:
                    cell.SetValue(doubleValue);
                    break;
                case float floatValue:
                    cell.SetValue(floatValue);
                    break;
                case DateTime dateTimeValue:
                    cell.SetValue(dateTimeValue);
                    break;
                default:
                    cell.SetValue(value?.ToString() ?? string.Empty);
                    break;
            }
        }

        private static async Task<byte[]> ExporterPdfAsync(string titre, TiersFiltreDTO? filtre, string[] entetes, List<string[]> lignes)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var communeService = new CommuneService();
            var commune = await communeService.GetCommuneByIdWithRelationsAsync(Properties.Settings.Default.CommuneId);
            var exercice = ExerciceService.Instance.CurrentExercice;

            string typeCommune = commune?.TypCommune ?? "..........";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            var exerciceLabel = exercice?.Libelle ?? "Exercice en cours";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
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

                            row.RelativeItem(1).AlignCenter().Column(centerCol =>
                            {
                                centerCol.Item().Text("REPUBLIQUE DE GUINEE")
                                    .FontSize(10).Bold();
                                centerCol.Item().Text("Travail - Justice - Solidarité")
                                    .FontSize(9);

                                centerCol.Item().PaddingTop(5).Row(flag =>
                                {
                                    flag.ConstantItem(12).Height(6).Background(DrapeauRouge);
                                    flag.ConstantItem(12).Height(6).Background(DrapeauJaune);
                                    flag.ConstantItem(12).Height(6).Background(DrapeauVert);
                                });
                            });

                            row.RelativeItem(2).AlignRight().Column(rightCol =>
                            {
                                rightCol.Item().Text("Exercice")
                                    .FontSize(9);
                                rightCol.Item().Text(exerciceLabel)
                                    .FontSize(10).Bold();
                            });
                        });

                        col.Item().PaddingTop(10).Text($"Liste des {titre}")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(ConstruireFiltreResume(filtre))
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < entetes.Length; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var entete in entetes)
                            {
                                header.Cell().Background(HeaderBackground).Border(1).BorderColor(BorderColor)
                                    .Padding(4).AlignCenter().Text(entete).FontColor(HeaderText).SemiBold();
                            }
                        });

                        foreach (var ligne in lignes)
                        {
                            for (int i = 0; i < entetes.Length; i++)
                            {
                                var valeur = i < ligne.Length ? ligne[i] : string.Empty;
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(4).Text(valeur);
                            }
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string ConstruireFiltreResume(TiersFiltreDTO? filtre)
        {
            var recherche = string.IsNullOrWhiteSpace(filtre?.RechercheTexte) ? "Tous" : filtre!.RechercheTexte!;
            var statut = string.IsNullOrWhiteSpace(filtre?.Statut) ? "Tous" : filtre!.Statut!;
            var inclure = filtre?.IncluireSoldes == true ? "Oui" : "Non";
            return $"Recherche: {recherche} | Statut: {statut} | Inclure soldés: {inclure}";
        }
    }
}
