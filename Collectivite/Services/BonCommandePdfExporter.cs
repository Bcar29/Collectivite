using Collectivite.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service d'export PDF pour les Bons de Commande
    /// </summary>
    public class BonCommandePdfExporter
    {
        // ═══════════════════════════════════════════════════════════
        // PALETTE DE COULEURS
        // ═══════════════════════════════════════════════════════════
        private static readonly string BleuPrimaire = "#1976D2";
        private static readonly string VertEmeraude = "#059669";
        private static readonly string VertFonce = "#065F46";
        private static readonly string VertSucces = "#388E3C";
        private static readonly string GrisArdoise = "#1E293B";
        private static readonly string GrisFonce = "#475569";
        private static readonly string GrisTexte = "#64748B";
        private static readonly string FondBleuClair = "#E3F2FD";
        private static readonly string FondVertClair = "#D1FAE5";
        private static readonly string FondVertPale = "#E8F5E9";
        private static readonly string FondGrisClair = "#F5F5F5";
        private static readonly string GrisBordure = "#E0E0E0";
        private static readonly string Blanc = "#FFFFFF";

        // Couleurs du drapeau guinéen
        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";

        /// <summary>
        /// Exporte un Bon de Commande en PDF (version asynchrone)
        /// </summary>
        public async Task<byte[]> ExporterAsync(BonCommande bonCommande, Commune commune, List<DetailBonCommande> details)
        {
            return await Task.Run(() => Exporter(bonCommande, commune, details));
        }

        /// <summary>
        /// Exporte un Bon de Commande en PDF (version synchrone)
        /// </summary>
        public byte[] Exporter(BonCommande bonCommande, Commune commune, List<DetailBonCommande> details)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Calculer le montant total
            double montantTotal = details?.Sum(d => d.Total) ?? 0;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // EN-TÊTE
                    page.Header().Element(c => ComposeHeader(c, bonCommande, commune));

                    // CONTENU
                    page.Content().Element(c => ComposeContent(c, bonCommande, commune, details, montantTotal));

                    // PIED DE PAGE
                    page.Footer().Element(c => ComposeFooter(c, bonCommande));
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Compose l'en-tête du document
        /// </summary>
        private void ComposeHeader(IContainer container, BonCommande bonCommande, Commune commune)
        {
            container.Column(column =>
            {
                // ═══════════════════════════════════════════════════════════
                // EN-TÊTE OFFICIEL (3 colonnes)
                // ═══════════════════════════════════════════════════════════
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(4);
                    });

                    // Colonne gauche : Ministère + Infos géographiques
                    table.Cell().Column(col =>
                    {
                        col.Item().Text("Ministère de l'Administration du Territoire")
                            .FontSize(9).Bold().FontColor(GrisArdoise);
                        col.Item().Text("et de la Décentralisation")
                            .FontSize(9).Bold().FontColor(GrisArdoise);
                        col.Item().Text("Direction Générale des Collectivités Locales")
                            .FontSize(8).SemiBold().FontColor(GrisFonce);

                        col.Item().PaddingTop(8).Column(inner =>
                        {
                            inner.Item().Text(text =>
                            {
                                text.Span("REGION ADMINISTRATIVE DE ").FontSize(8).FontColor(GrisTexte);
                                text.Span(commune?.RegionCommune ?? "....................").FontSize(8).Bold().FontColor(GrisArdoise);
                            });
                            inner.Item().Text(text =>
                            {
                                text.Span("PREFECTURE DE ").FontSize(8).FontColor(GrisTexte);
                                text.Span(commune?.PrefectureCommune ?? "....................").FontSize(8).Bold().FontColor(GrisArdoise);
                            });
                            inner.Item().Text(text =>
                            {
                                text.Span("COMMUNE ").FontSize(8).FontColor(GrisTexte);
                                text.Span(commune?.TypCommune ?? "..........").FontSize(8).Bold().FontColor(GrisArdoise);
                                text.Span(" DE ").FontSize(8).FontColor(GrisTexte);
                                text.Span(commune?.NomCommune ?? "....................").FontSize(8).Bold().FontColor(GrisArdoise);
                            });
                        });
                    });

                    // Colonne centrale : Drapeau guinéen
                    table.Cell().AlignCenter().AlignTop().PaddingTop(5).Element(c =>
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

                    // Colonne droite : République
                    table.Cell().AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text("REPUBLIQUE DE GUINEE")
                            .FontSize(9).Bold().FontColor(GrisArdoise);
                        col.Item().AlignRight().Text("Travail - Justice - Solidarité")
                            .FontSize(8).Italic().FontColor(GrisFonce);
                    });
                });

                column.Item().PaddingTop(15);

                // ═══════════════════════════════════════════════════════════
                // TITRE DU DOCUMENT
                // ═══════════════════════════════════════════════════════════
                column.Item().AlignCenter().Text("BON DE COMMANDE")
                    .FontSize(18).Bold().FontColor(GrisArdoise);

                column.Item().PaddingTop(8);

                // Bandeau vert avec commune
                column.Item().AlignCenter().Element(c =>
                {
                    c.MinWidth(300).Background(FondVertClair)
                        .BorderTop(2).BorderBottom(2).BorderColor(VertEmeraude)
                        .Padding(8).AlignCenter().Text(text =>
                        {
                            text.Span("DE LA COMMUNE ").FontSize(11).Bold().FontColor(VertFonce);
                            text.Span(commune?.TypCommune ?? "..........").FontSize(11).Bold().FontColor(VertFonce);
                            text.Span(" DE ").FontSize(11).Bold().FontColor(VertFonce);
                            text.Span(commune?.NomCommune ?? "....................").FontSize(11).Bold().FontColor(VertFonce);
                        });
                });

                column.Item().PaddingTop(8);

                // Exercice
                column.Item().AlignCenter().Text(bonCommande?.ExpressionBesoin?.Exercice?.Libelle ?? "2025")
                    .FontSize(12).Bold().FontColor(GrisArdoise);

                column.Item().PaddingTop(10);
                column.Item().LineHorizontal(1).LineColor(GrisBordure);
                column.Item().PaddingTop(10);
            });
        }

        /// <summary>
        /// Compose le contenu du document
        /// </summary>
        private void ComposeContent(IContainer container, BonCommande bonCommande, Commune commune, List<DetailBonCommande> details, double montantTotal)
        {
            container.Column(column =>
            {
                // ═══════════════════════════════════════════════════════════
                // INFORMATIONS GÉNÉRALES
                // ═══════════════════════════════════════════════════════════
                column.Item().Text("Informations Générales")
                    .FontSize(12).Bold().FontColor(BleuPrimaire);

                column.Item().PaddingTop(10);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // Ligne 1 : N° Bon de Commande + Date
                    table.Cell().Padding(5).Column(col =>
                    {
                        col.Item().Text("N° Bon de Commande").FontSize(8).FontColor(GrisTexte);
                        col.Item().PaddingTop(3).Background(FondGrisClair).Padding(8)
                            .Text(bonCommande?.Numero ?? "-").FontSize(11).Bold();
                    });

                    table.Cell().Padding(5).Column(col =>
                    {
                        col.Item().Text("Date de Création").FontSize(8).FontColor(GrisTexte);
                        col.Item().PaddingTop(3).Background(FondGrisClair).Padding(8)
                            .Text(bonCommande?.DateCreation.ToString("dd/MM/yyyy") ?? "-").FontSize(11).Bold();
                    });

                    // Ligne 2 : Expression de Besoin + Exercice
                    table.Cell().Padding(5).Column(col =>
                    {
                        col.Item().Text("Expression de Besoin").FontSize(8).FontColor(GrisTexte);
                        col.Item().PaddingTop(3).Background(FondBleuClair).Padding(8)
                            .Text(bonCommande?.ExpressionBesoin?.Numero ?? "-").FontSize(11).Bold().FontColor(BleuPrimaire);
                    });

                    table.Cell().Padding(5).Column(col =>
                    {
                        col.Item().Text("Exercice").FontSize(8).FontColor(GrisTexte);
                        col.Item().PaddingTop(3).Background(FondVertPale).Padding(8)
                            .Text(bonCommande?.ExpressionBesoin?.Exercice?.Libelle ?? "-").FontSize(11).Bold().FontColor(VertSucces);
                    });
                });

                column.Item().PaddingTop(15);

                // ═══════════════════════════════════════════════════════════
                // TABLEAU DES DÉTAILS
                // ═══════════════════════════════════════════════════════════
                column.Item().Text("Détails du Bon de Commande")
                    .FontSize(12).Bold().FontColor(BleuPrimaire);

                column.Item().PaddingTop(10);

                if (details != null && details.Any())
                {
                    column.Item().Table(table =>
                    {
                        // Définition des colonnes
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // #
                            columns.RelativeColumn(3);    // Désignation
                            columns.ConstantColumn(60);   // Quantité
                            columns.ConstantColumn(100);  // Prix Unitaire
                            columns.ConstantColumn(110);  // Total
                        });

                        // En-tête du tableau
                        table.Header(header =>
                        {
                            header.Cell().Background(BleuPrimaire).Padding(6)
                                .Text("#").FontSize(8).Bold().FontColor(Colors.White).AlignCenter();
                            header.Cell().Background(BleuPrimaire).Padding(6)
                                .Text("Désignation").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(BleuPrimaire).Padding(6)
                                .Text("Qté").FontSize(8).Bold().FontColor(Colors.White).AlignCenter();
                            header.Cell().Background(BleuPrimaire).Padding(6)
                                .Text("Prix Unitaire").FontSize(8).Bold().FontColor(Colors.White).AlignRight();
                            header.Cell().Background(BleuPrimaire).Padding(6)
                                .Text("Total").FontSize(8).Bold().FontColor(Colors.White).AlignRight();
                        });

                        // Lignes de données
                        int index = 1;
                        foreach (var detail in details)
                        {
                            var bgColor = index % 2 == 0 ? FondGrisClair : Blanc;

                            // #
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(GrisBordure).Padding(5)
                                .Text(index.ToString()).FontSize(8).AlignCenter();

                            // Désignation
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(GrisBordure).Padding(5)
                                .Text(detail.Designation ?? "-").FontSize(8);

                            // Quantité
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(GrisBordure).Padding(5)
                                .Element(c => c.Background(FondBleuClair).Padding(3).AlignCenter()
                                    .Text(detail.Quantite.ToString()).FontSize(8).Bold().FontColor(BleuPrimaire));

                            // Prix Unitaire
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(GrisBordure).Padding(5)
                                .Text($"{detail.PrixUnitaire:N0} GNF").FontSize(8).AlignRight().FontColor(GrisTexte);

                            // Total
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(GrisBordure).Padding(5)
                                .Element(c => c.Background(FondVertPale).Padding(3).AlignRight()
                                    .Text($"{detail.Total:N0} GNF").FontSize(8).Bold().FontColor(VertSucces));

                            index++;
                        }
                    });

                    column.Item().PaddingTop(10);

                    // ═══════════════════════════════════════════════════════════
                    // RÉCAPITULATIF - MONTANT TOTAL
                    // ═══════════════════════════════════════════════════════════
                    column.Item().AlignRight().Element(c =>
                    {
                        c.Width(250).Border(2).BorderColor(VertSucces).Background(FondVertPale).Padding(12).Column(recap =>
                        {
                            recap.Item().Row(row =>
                            {
                                row.RelativeItem().Text("MONTANT TOTAL :").FontSize(11).Bold().FontColor(GrisArdoise);
                                row.RelativeItem().AlignRight().Text($"{montantTotal:N0} GNF")
                                    .FontSize(14).Bold().FontColor(VertSucces);
                            });
                        });
                    });
                }
                else
                {
                    column.Item().Background(FondGrisClair).Padding(20).AlignCenter()
                        .Text("Aucun détail disponible").FontSize(10).Italic().FontColor(GrisTexte);
                }

                column.Item().PaddingTop(25);

                // ═══════════════════════════════════════════════════════════
                // SIGNATURE
                // ═══════════════════════════════════════════════════════════
                column.Item().AlignRight().Column(sig =>
                {
                    sig.Item().Text(text =>
                    {
                        text.Span(commune?.NomCommune ?? "....................").Bold().FontSize(10);
                        text.Span(" Le ......./......./............").FontSize(10);
                    });

                    sig.Item().PaddingTop(15).Text("L'Ordonateur :").FontSize(10).Bold();
                    sig.Item().PaddingTop(30).Text("M./Mme : ............................................").FontSize(10);
                });
            });
        }

        /// <summary>
        /// Compose le pied de page
        /// </summary>
        private void ComposeFooter(IContainer container, BonCommande bonCommande)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(GrisBordure);
                column.Item().PaddingTop(5);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().AlignLeft().Text($"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}")
                        .FontSize(8).Italic().FontColor(GrisTexte);

                    table.Cell().AlignCenter().Text($"Bon de Commande N° {bonCommande?.Numero ?? "-"}")
                        .FontSize(9).Bold().FontColor(GrisFonce);

                    table.Cell().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).FontColor(GrisTexte);
                        text.CurrentPageNumber().FontSize(8).FontColor(GrisTexte);
                        text.Span(" / ").FontSize(8).FontColor(GrisTexte);
                        text.TotalPages().FontSize(8).FontColor(GrisTexte);
                    });
                });
            });
        }
    }
}