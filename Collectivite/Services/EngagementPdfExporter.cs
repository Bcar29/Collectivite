using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Collectivite.Models;
using System;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class EngagementPdfExporter
    {
        // ═══════════════════════════════════════
        // PALETTE DE COULEURS
        // ═══════════════════════════════════════

        private static readonly string BleuPrimaire = "#1976D2";
        private static readonly string VertEmeraude = "#059669";
        private static readonly string VertSucces = "#388E3C";
        private static readonly string OrangeMoyen = "#F57C00";

        private static readonly string BleuTresClair = "#E3F2FD";
        private static readonly string VertClair = "#D1FAE5";
        private static readonly string VertPaleClair = "#E8F5E9";
        private static readonly string OrangeClair = "#FFF3E0";
        private static readonly string GrisClair = "#F5F5F5";
        private static readonly string VertLettres = "#F9FBE7";

        private static readonly string GrisArdoise = "#1E293B";
        private static readonly string GrisFonce = "#475569";
        private static readonly string GrisTexte = "#64748B";
        private static readonly string VertFonce = "#065F46";
        private static readonly string VertTexte = "#33691E";

        private static readonly string GrisBordure = "#E0E0E0";
        private static readonly string GrisFiligrane = "#F0F0F0";
        // Couleurs du drapeau guinéen
        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";

        /// <summary>
        /// Exporte l'engagement en PDF sur une seule page A4
        /// </summary>
        public static async Task<byte[]> ExporterAsync(Engagement engagement, Commune commune)
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
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Content().Layers(layers =>
                    {
                        // ═══════════════════════════════════════════════════════════
                        // LAYER 1 : FILIGRANE
                        // ═══════════════════════════════════════════════════════════
                        layers.Layer().AlignCenter().AlignMiddle()
                            .Text(filigraneTexte)
                            .FontSize(50)
                            .Bold()
                            .FontColor(Color.FromHex(GrisFiligrane));

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
                                    rightCol.Item().AlignRight().Text("REPUBLIQUE DE GUINEE")
                                        .FontSize(10).Bold().FontColor(Color.FromHex(GrisArdoise));
                                    rightCol.Item().AlignRight().Text("Travail - Justice - Solidarité")
                                        .FontSize(9).Italic().FontColor(Color.FromHex(GrisFonce));
                                });
                            });

                            col.Item().PaddingTop(15);

                            // ═══════════════════════════════════════════════════════════
                            // TITRE + ÉTAT
                            // ═══════════════════════════════════════════════════════════
                            col.Item().AlignCenter().Row(titleRow =>
                            {
                                titleRow.AutoItem().Text("ENGAGEMENT")
                                    .FontSize(18).Bold().FontColor(Color.FromHex(GrisArdoise));

                                // Badge d'état
                                titleRow.AutoItem().PaddingLeft(15).Element(badge =>
                                {
                                    string etatTexte = engagement.Etat.ToString().Replace("_", " ");
                                    string badgeColor = GetEtatColor(engagement.Etat);

                                    badge.Background(Color.FromHex(badgeColor))
                                        .Padding(4,0)
                                        .Text(etatTexte)
                                        .FontSize(8)
                                        .Bold()
                                        .FontColor(Colors.White);
                                });
                            });

                            col.Item().PaddingVertical(5).AlignCenter().Element(e =>
                            {
                                e.MinWidth(300).Background(Color.FromHex(VertClair))
                                    .BorderTop(2).BorderBottom(2).BorderColor(Color.FromHex(VertEmeraude))
                                    .Padding(5).AlignCenter()
                                    .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                                    .FontSize(9).SemiBold().FontColor(Color.FromHex(VertFonce));
                            });

                            col.Item().PaddingTop(3).AlignCenter()
                                .Text(engagement.Exercice?.Libelle ?? DateTime.Now.Year.ToString())
                                .FontSize(11).SemiBold().FontColor(Color.FromHex(GrisArdoise));

                            col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // INFORMATIONS GÉNÉRALES
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(10).Text("📋 Informations générales")
                                .FontSize(10).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(5).Row(infoRow =>
                            {
                                infoRow.RelativeItem().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(70);
                                        columns.RelativeColumn();
                                    });

                                    AjouterLigneInfo(table, "Exercice :", engagement.Exercice?.Libelle ?? "-");
                                    AjouterLigneInfo(table, "Date :", engagement.DateEngagement.ToString("dd/MM/yyyy"));
                                    AjouterLigneInfo(table, "Commune :", engagement.Commune?.Nom ?? nomCommune);
                                    AjouterLigneInfo(table, "Tiers :", engagement.Tiers?.NomComplet ?? "Non spécifié");
                                });

                                infoRow.ConstantItem(15);

                                infoRow.RelativeItem().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(85);
                                        columns.RelativeColumn();
                                    });

                                    AjouterLigneInfo(table, "Ligne budgétaire :", TronquerTexte(engagement.BudgetLine?.Nommenclature?.Intitule, 35));
                                    AjouterLigneInfo(table, "Bon Commande :", engagement.BonCommande?.Numero ?? "Non spécifié");
                                    AjouterLigneInfo(table, "Facture :", engagement.Facture?.NumeroFacture ?? "Non spécifiée");
                                    AjouterLigneInfo(table, "Objet :", TronquerTexte(engagement.Objet, 35));
                                });
                            });

                            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // MONTANTS (3 colonnes)
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(10).Text("💰 Montants")
                                .FontSize(10).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(5).Row(montantRow =>
                            {
                                // Crédits budgétaires
                                montantRow.RelativeItem().Background(Color.FromHex(BleuTresClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("Crédits budgétaires").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{engagement.CreditsBudgetaires:N0} GNF")
                                            .FontSize(12).Bold().FontColor(Color.FromHex(BleuPrimaire));
                                    });

                                montantRow.ConstantItem(8);

                                // Engagements antérieurs
                                montantRow.RelativeItem().Background(Color.FromHex(OrangeClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("Engagements antérieurs").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{engagement.EngagementsAnterieurs:N0} GNF")
                                            .FontSize(12).Bold().FontColor(Color.FromHex(OrangeMoyen));
                                    });

                                montantRow.ConstantItem(8);

                                // Montant de l'engagement
                                montantRow.RelativeItem().Background(Color.FromHex(VertPaleClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("Montant de l'engagement").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{engagement.MontantEngagement:N0} GNF")
                                            .FontSize(12).Bold().FontColor(Color.FromHex(VertSucces));
                                    });
                            });

                            // Cumul des engagements
                            col.Item().PaddingTop(8).Background(Color.FromHex(GrisClair))
                                .Padding(10).Row(row =>
                                {
                                    row.RelativeItem().AlignLeft().AlignMiddle()
                                        .Text("Cumul des engagements :").FontSize(9).SemiBold();
                                    row.RelativeItem().AlignRight().AlignMiddle()
                                        .Text($"{engagement.CumulEngagement:N0} GNF")
                                        .FontSize(14).Bold().FontColor(Color.FromHex(BleuPrimaire));
                                });

                            // Montant en lettres
                            col.Item().PaddingTop(6).Background(Color.FromHex(VertLettres))
                                .Padding(8).Column(innerCol =>
                                {
                                    innerCol.Item().Text("Montant en lettres :")
                                        .FontSize(7).SemiBold().FontColor(Color.FromHex(GrisTexte));
                                    innerCol.Item().PaddingTop(2)
                                        .Text(engagement.MontantLettre ?? "-")
                                        .FontSize(9).Italic().FontColor(Color.FromHex(VertTexte));
                                });

                            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // FICHIER JOINT
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(10).Text("📎 Fichier joint")
                                .FontSize(10).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(5).Background(Color.FromHex(GrisClair))
                                .Padding(8)
                                .Text(string.IsNullOrEmpty(engagement.FichierName) ? "Aucun fichier joint" : engagement.FichierName)
                                .FontSize(8).FontColor(Color.FromHex(GrisArdoise));

                            // ═══════════════════════════════════════════════════════════
                            // SIGNATURE (si engagement validé)
                            // ═══════════════════════════════════════════════════════════
                            if (engagement.Etat == Engagement.EtatEngagement.Validé)
                            {
                                col.Item().PaddingTop(25).AlignRight().PaddingRight(10).Column(sigCol =>
                                {
                                    sigCol.Item().Text(text =>
                                    {
                                        text.Span(nomCommune).FontSize(9);
                                        text.Span(" Le .......... / .......... / ..........").FontSize(8).FontColor(Color.FromHex(GrisTexte));
                                    });

                                    sigCol.Item().PaddingTop(18).Text("L'Ordonateur :")
                                        .FontSize(9).SemiBold();

                                    sigCol.Item().PaddingTop(25).Text("M./Mme : ................................................................")
                                        .FontSize(9).SemiBold();
                                });
                            }
                            else
                            {
                                col.Item().PaddingTop(20);
                            }

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
                            .Text($"Engagement - {engagement.Exercice?.Libelle}")
                            .FontSize(8).SemiBold().FontColor(Color.FromHex(GrisFonce));

                        row.RelativeItem().AlignRight()
                            .Text($"État : {engagement.Etat.ToString().Replace("_", " ")}")
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
        /// Retourne la couleur du badge selon l'état
        /// </summary>
        private static string GetEtatColor(Engagement.EtatEngagement etat)
        {
            return etat switch
            {
                Engagement.EtatEngagement.Validé => "#4CAF50",       // Vert
                Engagement.EtatEngagement.Non_Validé => "#FF9800",   // Orange
                _ => "#9E9E9E"                                        // Gris
            };
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
        /// Version synchrone (évite le deadlock WPF)
        /// </summary>
        public static byte[] Exporter(Engagement engagement, Commune commune)
        {
            return Task.Run(() => ExporterAsync(engagement, commune)).GetAwaiter().GetResult();
        }
    }
}