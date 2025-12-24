using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using Collectivite.Models;
using System;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class OrdreRecettePdfExporter
    {
        // ═══════════════════════════════════════
        // PALETTE DE COULEURS
        // ═══════════════════════════════════════

        private static readonly string BleuPrimaire = "#1976D2";
        private static readonly string VertEmeraude = "#059669";
        private static readonly string VertSucces = "#4CAF50";

        private static readonly string BleuTresClair = "#E3F2FD";
        private static readonly string VertClair = "#D1FAE5";
        private static readonly string JauneClair = "#FFFDE7";
        private static readonly string VertPaleClair = "#F9FBE7";

        private static readonly string GrisArdoise = "#1E293B";
        private static readonly string GrisFonce = "#475569";
        private static readonly string GrisTexte = "#64748B";
        private static readonly string VertFonce = "#065F46";
        private static readonly string VertTexte = "#33691E";

        private static readonly string JauneBordure = "#FFF59D";
        private static readonly string GrisBordure = "#E0E0E0";
        // private static readonly string GrisFiligrane = "#F0F0F0";

        /// <summary>
        /// Exporte l'ordre de recette en PDF sur une seule page A4
        /// </summary>
        public static async Task<byte[]> ExporterAsync(OrdreRecette ordre, Commune commune, Mouvement? mouvement = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (commune == null)
            {
                var communeService = new CommuneService();
                commune = await communeService.GetCommuneByIdWithRelationsAsync(Properties.Settings.Default.CommuneId);
            }

            string typeCommune = commune?.TypCommune ?? "..........";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";
            string filigraneTexte = nomCommune.ToUpper();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Layers(layers =>
                    {
                        // ═══════════════════════════════════════════════════════════
                        // LAYER 1 : FILIGRANE DIAGONAL UNIQUE (45°)
                        // ═══════════════════════════════════════════════════════════
                     

                        // ═══════════════════════════════════════════════════════════
                        // LAYER 2 : CONTENU PRINCIPAL
                        // ═══════════════════════════════════════════════════════════
                        layers.PrimaryLayer().Column(col =>
                        {
                            // ═══════════════════════════════════════════════════════════
                            // EN-TÊTE OFFICIEL
                            // ═══════════════════════════════════════════════════════════
                            col.Item().Row(row =>
                            {
                                row.RelativeItem(2).Column(leftCol =>
                                {
                                    leftCol.Item().Text("Ministère de l'Administration du Territoire")
                                        .FontSize(9).Bold().FontColor(Color.FromHex(GrisArdoise));
                                    leftCol.Item().Text("et de la Décentralisation")
                                        .FontSize(9).Bold().FontColor(Color.FromHex(GrisArdoise));
                                    leftCol.Item().PaddingTop(2).Text("Direction Générale des Collectivités Locales")
                                        .FontSize(8).SemiBold().FontColor(Color.FromHex(GrisFonce));

                                    leftCol.Item().PaddingTop(8).Text(text =>
                                    {
                                        text.Span("REGION ADMINISTRATIVE DE ").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                        text.Span(region.ToUpper()).FontSize(8).SemiBold().FontColor(Color.FromHex(GrisArdoise));
                                    });
                                    leftCol.Item().Text(text =>
                                    {
                                        text.Span("PREFECTURE DE ").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                        text.Span(prefecture.ToUpper()).FontSize(8).SemiBold().FontColor(Color.FromHex(GrisArdoise));
                                    });
                                    leftCol.Item().Text(text =>
                                    {
                                        text.Span("COMMUNE ").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                        text.Span(typeCommune.ToUpper()).FontSize(8).SemiBold().FontColor(Color.FromHex(GrisArdoise));
                                        text.Span(" DE ").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                        text.Span(nomCommune.ToUpper()).FontSize(8).SemiBold().FontColor(Color.FromHex(GrisArdoise));
                                    });
                                });

                                row.RelativeItem(1).AlignCenter().AlignMiddle()
                                    .Text("⚜").FontSize(32).FontColor(Color.FromHex(VertEmeraude));

                                row.RelativeItem(2).AlignRight().Column(rightCol =>
                                {
                                    rightCol.Item().AlignRight().Text("REPUBLIQUE DE GUINEE")
                                        .FontSize(10).Bold().FontColor(Color.FromHex(GrisArdoise));
                                    rightCol.Item().AlignRight().Text("Travail - Justice - Solidarité")
                                        .FontSize(9).Italic().FontColor(Color.FromHex(GrisFonce));
                                });
                            });

                            col.Item().PaddingTop(20);

                            // ═══════════════════════════════════════════════════════════
                            // TITRE
                            // ═══════════════════════════════════════════════════════════
                            col.Item().AlignCenter().Text("ORDRE DE RECETTE")
                                .FontSize(18).Bold().FontColor(Color.FromHex(GrisArdoise));

                            col.Item().PaddingVertical(6).AlignCenter().Element(e =>
                            {
                                e.MinWidth(300).Background(Color.FromHex(VertClair))
                                    .BorderTop(2).BorderBottom(2).BorderColor(Color.FromHex(VertEmeraude))
                                    .Padding(6).AlignCenter()
                                    .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                                    .FontSize(10).SemiBold().FontColor(Color.FromHex(VertFonce));
                            });

                            col.Item().PaddingTop(4).AlignCenter()
                                .Text(ordre.Exercice?.Libelle ?? DateTime.Now.Year.ToString())
                                .FontSize(12).SemiBold().FontColor(Color.FromHex(GrisArdoise));

                            col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // INFORMATIONS GÉNÉRALES
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(20).Text("Informations générales")
                                .FontSize(11).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(6).Row(infoRow =>
                            {
                                infoRow.RelativeItem().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(75);
                                        columns.RelativeColumn();
                                    });

                                    AjouterLigneInfo(table, "N° Ordre :", ordre.NumeroOrdre ?? "-");
                                    AjouterLigneInfo(table, "Date :", ordre.DateOrdre.ToString("dd/MM/yyyy"));
                                    AjouterLigneInfo(table, "Exercice :", ordre.Exercice?.Libelle ?? "-");
                                    AjouterLigneInfo(table, "Tiers :", ordre.Tiers?.NomComplet ?? "Non spécifié");
                                });

                                infoRow.ConstantItem(20);

                                infoRow.RelativeItem().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(90);
                                        columns.RelativeColumn();
                                    });

                                    AjouterLigneInfo(table, "Imputation :", ordre.BudgetLine?.Nommenclature?.CodeNomenclature ?? "-");
                                    AjouterLigneInfo(table, "Ligne budgétaire :", TronquerTexte(ordre.BudgetLine?.Nommenclature?.Intitule, 35));
                                    AjouterLigneInfo(table, "Commune :", ordre.Commune?.Nom ?? nomCommune);
                                    AjouterLigneInfo(table, "Comptable :", ordre.Comptable ?? "Non défini");
                                });
                            });

                            col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // MONTANT
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(20).Text("Montant")
                                .FontSize(11).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(6).Background(Color.FromHex(BleuTresClair))
                                .Border(1.5f).BorderColor(Color.FromHex(BleuPrimaire))
                                .Padding(10).Row(row =>
                                {
                                    row.RelativeItem().AlignLeft().AlignMiddle()
                                        .Text("MONTANT DE L'ORDRE :").FontSize(10).SemiBold();
                                    row.RelativeItem().AlignRight().AlignMiddle()
                                        .Text($"{ordre.MontantOrdre:N0} GNF")
                                        .FontSize(16).Bold().FontColor(Color.FromHex(BleuPrimaire));
                                });

                            col.Item().PaddingTop(6).Background(Color.FromHex(VertPaleClair))
                                .Padding(8).Column(innerCol =>
                                {
                                    innerCol.Item().Text("Montant en lettres :")
                                        .FontSize(8).SemiBold().FontColor(Color.FromHex(GrisTexte));
                                    innerCol.Item().PaddingTop(2)
                                        .Text(ordre.MontantOrdreLettre ?? "-")
                                        .FontSize(9).Italic().FontColor(Color.FromHex(VertTexte));
                                });

                            col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // MOTIFS
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(10).Text("Motifs / Observations")
                                .FontSize(11).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(6).Background(Color.FromHex(JauneClair))
                                .Border(0.5f).BorderColor(Color.FromHex(JauneBordure))
                                .Padding(10).MinHeight(40)
                                .Text(string.IsNullOrEmpty(ordre.Motifs) ? "Aucun motif" : ordre.Motifs)
                                .FontSize(9).FontColor(Color.FromHex(GrisArdoise));

                            // ═══════════════════════════════════════════════════════════
                            // ENCAISSEMENT (si mouvement existe)
                            // ═══════════════════════════════════════════════════════════
                            if (mouvement != null)
                            {
                                col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                                col.Item().PaddingTop(20).Text("Informations d'encaissement")
                                    .FontSize(11).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                                string modePaiement = GetModePaiement(mouvement);

                                col.Item().PaddingTop(6).Row(encRow =>
                                {
                                    encRow.RelativeItem().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(110);
                                            columns.RelativeColumn();
                                        });

                                        AjouterLigneInfo(table, "Date encaissement :", mouvement.Date.ToString("dd/MM/yyyy"));

                                        table.Cell().PaddingVertical(2).Text("Montant encaissé :").FontSize(8).SemiBold();
                                        table.Cell().PaddingVertical(2).Text($"{mouvement.Montant:N0} GNF")
                                            .FontSize(8).SemiBold().FontColor(Color.FromHex(VertSucces));
                                    });

                                    encRow.ConstantItem(20);

                                    encRow.RelativeItem().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(110);
                                            columns.RelativeColumn();
                                        });

                                        table.Cell().PaddingVertical(2).Text("Mode encaissement :").FontSize(8).SemiBold();
                                        table.Cell().PaddingVertical(2).Text(modePaiement)
                                            .FontSize(8).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                                        if (!string.IsNullOrEmpty(mouvement.RefVirement))
                                            AjouterLigneInfo(table, "Réf. virement :", mouvement.RefVirement);
                                        else if (!string.IsNullOrEmpty(mouvement.RefChèque))
                                            AjouterLigneInfo(table, "Réf. chèque :", mouvement.RefChèque);
                                    });
                                });
                            }

                            // ═══════════════════════════════════════════════════════════
                            // ESPACE FLEXIBLE
                            // ═══════════════════════════════════════════════════════════
                            //col.Item().Extend();

                            // ═══════════════════════════════════════════════════════════
                            // SIGNATURE
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(30).AlignRight().PaddingRight(10).Column(sigCol =>
                            {
                                sigCol.Item().Text(text =>
                                {
                                    text.Span(nomCommune).FontSize(9);
                                    text.Span(" Le .......... / .......... / ..........").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                });

                                sigCol.Item().PaddingTop(20).Text("L'Ordonateur :")
                                    .FontSize(10).SemiBold();

                                sigCol.Item().PaddingTop(28).Text("M./Mme : ................................................................")
                                    .FontSize(10).SemiBold();
                            });

                            col.Item().PaddingTop(8);
                        });
                    });

                    // ═══════════════════════════════════════════════════════════
                    // PIED DE PAGE
                    // ═══════════════════════════════════════════════════════════
                    page.Footer().BorderTop(0.5f).BorderColor(Color.FromHex(GrisBordure)).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text($"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}")
                            .FontSize(7).Italic().FontColor(Color.FromHex(GrisTexte));

                        row.RelativeItem().AlignCenter()
                            .Text($"Ordre de Recette N° {ordre.NumeroOrdre}")
                            .FontSize(8).SemiBold().FontColor(Color.FromHex(GrisFonce));

                        row.RelativeItem().AlignRight()
                            .Text($"{ordre.Exercice?.Libelle ?? DateTime.Now.Year.ToString()}")
                            .FontSize(7).Italic().FontColor(Color.FromHex(GrisTexte));
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Ajoute une ligne d'information dans une table
        /// </summary>
        private static void AjouterLigneInfo(TableDescriptor table, string label, string valeur)
        {
            table.Cell().PaddingVertical(2).Text(label).FontSize(8).SemiBold();
            table.Cell().PaddingVertical(2).Text(valeur ?? "-").FontSize(8);
        }

        /// <summary>
        /// Détermine le mode de paiement
        /// </summary>
        private static string GetModePaiement(Mouvement mouvement)
        {
            if (!string.IsNullOrEmpty(mouvement.RefVirement)) return "Virement bancaire";
            if (!string.IsNullOrEmpty(mouvement.RefChèque)) return "Chèque";
            return "Espèces";
        }

        /// <summary>
        /// Tronque un texte
        /// </summary>
        private static string TronquerTexte(string? texte, int maxLength)
        {
            if (string.IsNullOrEmpty(texte)) return "-";
            if (texte.Length <= maxLength) return texte;
            return texte.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Version synchrone
        /// </summary>
        public static byte[] Exporter(OrdreRecette ordre, Commune commune, Mouvement? mouvement = null)
        {
            return Task.Run(() => ExporterAsync(ordre, commune, mouvement)).GetAwaiter().GetResult();
        }
    }
}