using Collectivite.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BalanceAnnuellePdfExporter
    {
        // ═══════════════════════════════════════
        // PALETTE DE COULEURS
        // ═══════════════════════════════════════

        private static readonly Color VertEmeraude = Color.FromHex("#059669");
        private static readonly Color RougeCoral = Color.FromHex("#DC2626");
        private static readonly Color BleuIndigo = Color.FromHex("#4F46E5");

        private static readonly Color VertClair = Color.FromHex("#D1FAE5");
        private static readonly Color VertClairAccent = Color.FromHex("#A7F3D0");
        private static readonly Color RougeClair = Color.FromHex("#FEE2E2");
        private static readonly Color RougeClairAccent = Color.FromHex("#FECACA");
        private static readonly Color BleuClair = Color.FromHex("#E0E7FF");

        private static readonly Color VertTresClair = Color.FromHex("#F0FDF4");
        private static readonly Color RougeTresClair = Color.FromHex("#FEF2F2");
        private static readonly Color GrisTresClair = Color.FromHex("#F8FAFC");

        private static readonly Color TexteVertFonce = Color.FromHex("#065F46");
        private static readonly Color TexteRougeFonce = Color.FromHex("#991B1B");
        private static readonly Color TexteBleuFonce = Color.FromHex("#3730A3");

        private static readonly Color GrisArdoise = Color.FromHex("#1E293B");
        private static readonly Color VertLumineux = Color.FromHex("#34D399");
        private static readonly Color RougeLumineux = Color.FromHex("#F87171");
        private static readonly Color GrisTotaux = Color.FromHex("#94A3B8");

        private static readonly Color GrisClair = Color.FromHex("#F1F5F9");
        private static readonly Color GrisTexte = Color.FromHex("#64748B");
        private static readonly Color GrisFonce = Color.FromHex("#475569");
        private static readonly Color GrisBordure = Color.FromHex("#E2E8F0");
        private static readonly Color GrisBordureFonce = Color.FromHex("#334155");

        private static readonly Color BorderVertFonce = Color.FromHex("#047857");
        private static readonly Color BorderRougeFonce = Color.FromHex("#B91C1C");
        private static readonly Color BorderBleuFonce = Color.FromHex("#4338CA");
        private static readonly Color BorderVertClair = Color.FromHex("#A7F3D0");
        private static readonly Color BorderVertAccent = Color.FromHex("#6EE7B7");
        private static readonly Color BorderRougeClair = Color.FromHex("#FECACA");
        private static readonly Color BorderRougeAccent = Color.FromHex("#FCA5A5");
        private static readonly Color BorderBleuClair = Color.FromHex("#C7D2FE");

        /// <summary>
        /// Exporte la Balance Annuelle en fichier PDF avec en-tête officiel (version async)
        /// </summary>
        public static async Task<byte[]> ExporterAsync(List<BalanceAnnuelleLigneDTO> lignes, BalanceAnnuelleTotauxDTO totaux, BalanceAnnuelleFiltreDTO filtre)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // ═══════════════════════════════════════════════════════════
            // RÉCUPÉRER LES DONNÉES DE LA COMMUNE
            // ═══════════════════════════════════════════════════════════
            var _communeService = new CommuneService();
            var commune = await _communeService.GetCommuneByIdWithRelationsAsync(Properties.Settings.Default.CommuneId);
            var exercice = ExerciceService.Instance.CurrentExercice;

            string typeCommune = commune?.TypCommune ?? "..........";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    // ═══════════════════════════════════════════════════════════
                    // EN-TÊTE OFFICIEL GUINÉEN
                    // ═══════════════════════════════════════════════════════════
                    page.Header().Column(headerCol =>
                    {
                        // Ligne 1 : Ministère (gauche) et République (droite)
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

                            row.RelativeItem(1).AlignCenter().AlignMiddle().Text("⚜")
                                .FontSize(30);

                            row.RelativeItem(2).AlignRight().Column(rightCol =>
                            {
                                rightCol.Item().AlignRight().Text("REPUBLIQUE DE GUINEE")
                                    .FontSize(11).Bold();
                                rightCol.Item().AlignRight().Text("Travail - Justice - Solidarité")
                                    .FontSize(10).Italic();
                            });
                        });

                        headerCol.Item().PaddingTop(12);

                        // Titre principal
                        headerCol.Item().AlignCenter().Text("BALANCE ANNUELLE DES COMPTES")
                            .FontSize(18).Bold().FontColor(GrisArdoise);

                        // Bandeau avec commune
                        headerCol.Item().PaddingVertical(5)
                            .BorderTop(2).BorderBottom(2).BorderColor(VertEmeraude)
                            .Padding(6).AlignCenter()
                            .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                            .FontSize(12).Bold();

                        // Exercice / Année
                        headerCol.Item().PaddingTop(6).AlignCenter()
                            .Text($" {ExerciceService.Instance.CurrentExercice.Libelle}")
                            .FontSize(14).Bold();

                        headerCol.Item().PaddingTop(8).LineHorizontal(1).LineColor(GrisBordure);
                        headerCol.Item().PaddingBottom(8);
                    });

                    // ═══════════════════════════════════════════════════════════
                    // CONTENU - Tableau
                    // ═══════════════════════════════════════════════════════════
                    page.Content().Element(c => ComposeTable(c, lignes, totaux));

                    // ═══════════════════════════════════════════════════════════
                    // PIED DE PAGE
                    // ═══════════════════════════════════════════════════════════
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text($"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}")
                            .FontSize(8).Italic().FontColor(GrisTexte);

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(GrisTexte));
                            text.Span("Page ");
                            text.CurrentPageNumber().FontColor(GrisFonce);
                            text.Span(" / ");
                            text.TotalPages().FontColor(GrisFonce);
                        });

                        row.RelativeItem().AlignRight()
                            .Text($"Nombre de comptes : {lignes.Count}")
                            .FontSize(8).Italic().FontColor(GrisTexte);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Version synchrone pour compatibilité (évite le deadlock WPF)
        /// </summary>
        public static byte[] Exporter(List<BalanceAnnuelleLigneDTO> lignes, BalanceAnnuelleTotauxDTO totaux, BalanceAnnuelleFiltreDTO filtre)
        {
            // Utiliser Task.Run pour éviter le deadlock WPF
            return Task.Run(() => ExporterAsync(lignes, totaux, filtre)).GetAwaiter().GetResult();
        }

        private static void ComposeTable(IContainer container, List<BalanceAnnuelleLigneDTO> lignes, BalanceAnnuelleTotauxDTO totaux)
        {
            container.Table(table =>
            {
                // Définition des colonnes (10 colonnes)
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);   // N° Compte
                    columns.RelativeColumn(2);    // Intitulé
                    columns.ConstantColumn(65);   // Balance Entrée D
                    columns.ConstantColumn(65);   // Mouv Annuel D
                    columns.ConstantColumn(70);   // Total D
                    columns.ConstantColumn(65);   // Balance Entrée C
                    columns.ConstantColumn(65);   // Mouv Annuel C
                    columns.ConstantColumn(70);   // Total C
                    columns.ConstantColumn(70);   // Débiteur
                    columns.ConstantColumn(70);   // Créditeur
                });

                // EN-TÊTE NIVEAU 1
                table.Header(header =>
                {
                    // Cellules pour N° et Intitulé
                    header.Cell().RowSpan(2).Element(HeaderCellNeutre).Text("N°\nComptes").FontSize(8);
                    header.Cell().RowSpan(2).Element(HeaderCellNeutre).Text("Intitulés").FontSize(8);

                    // Débit (3 colonnes) - Vert émeraude
                    header.Cell().ColumnSpan(3).Element(HeaderCellDebit).Text("DÉBIT").FontSize(11);

                    // Crédit (3 colonnes) - Rouge coral
                    header.Cell().ColumnSpan(3).Element(HeaderCellCredit).Text("CRÉDIT").FontSize(11);

                    // Solde (2 colonnes) - Bleu indigo
                    header.Cell().ColumnSpan(2).Element(HeaderCellSolde).Text("SOLDE").FontSize(11);

                    // EN-TÊTE NIVEAU 2 - Débit (tons verts clairs)
                    header.Cell().Element(SubHeaderCellDebit).Text("Balance\nEntrée").FontSize(7);
                    header.Cell().Element(SubHeaderCellDebit).Text("Mouv\nAnnuel").FontSize(7);
                    header.Cell().Element(SubHeaderCellDebitTotal).Text("Total").FontSize(8);

                    // EN-TÊTE NIVEAU 2 - Crédit (tons rouges clairs)
                    header.Cell().Element(SubHeaderCellCredit).Text("Balance\nEntrée").FontSize(7);
                    header.Cell().Element(SubHeaderCellCredit).Text("Mouv\nAnnuel").FontSize(7);
                    header.Cell().Element(SubHeaderCellCreditTotal).Text("Total").FontSize(8);

                    // EN-TÊTE NIVEAU 2 - Solde (tons bleus clairs)
                    header.Cell().Element(SubHeaderCellSolde).Text("Débiteur").FontSize(8);
                    header.Cell().Element(SubHeaderCellSolde).Text("Créditeur").FontSize(8);
                });

                // DONNÉES
                bool alternate = false;
                foreach (var ligne in lignes)
                {
                    var bgColor = alternate ? GrisTresClair : Colors.White;

                    // N° Compte
                    table.Cell().Element(c => DataCellCustom(c, bgColor))
                        .Text(ligne.NumeroCompte).FontSize(8).Bold().FontColor(GrisArdoise);

                    // Intitulé
                    table.Cell().Element(c => DataCellLeftCustom(c, bgColor))
                        .Text(ligne.IntituleCompte).FontSize(7).FontColor(GrisFonce);

                    // Débit - Balance Entrée, Mouv Annuel
                    table.Cell().Element(c => DataCellCustom(c, bgColor))
                        .Text(ligne.DebitBalanceEntreeFormate).FontSize(7).FontColor(GrisTexte);
                    table.Cell().Element(c => DataCellCustom(c, bgColor))
                        .Text(ligne.DebitMouvAnnuelFormate).FontSize(7).FontColor(GrisTexte);

                    // Total Débit
                    table.Cell().Element(DataCellDebitTotal)
                        .Text(ligne.DebitTotalFormate).FontSize(8);

                    // Crédit - Balance Entrée, Mouv Annuel
                    table.Cell().Element(c => DataCellCustom(c, bgColor))
                        .Text(ligne.CreditBalanceEntreeFormate).FontSize(7).FontColor(GrisTexte);
                    table.Cell().Element(c => DataCellCustom(c, bgColor))
                        .Text(ligne.CreditMouvAnnuelFormate).FontSize(7).FontColor(GrisTexte);

                    // Total Crédit
                    table.Cell().Element(DataCellCreditTotal)
                        .Text(ligne.CreditTotalFormate).FontSize(8);

                    // Solde Débiteur
                    table.Cell().Element(DataCellSoldeDebiteur)
                        .Text(ligne.SoldeDebiteurFormate).FontSize(8);

                    // Solde Créditeur
                    table.Cell().Element(DataCellSoldeCrebiteur)
                        .Text(ligne.SoldeCrebiteurFormate).FontSize(8);

                    alternate = !alternate;
                }

                // LIGNE DE TOTAUX
                table.Cell().ColumnSpan(2).Element(TotalCell)
                    .Text("TOTAUX").FontSize(9).Bold();

                // Totaux Débit
                table.Cell().Element(TotalCell)
                    .Text(totaux.TotalDebitBalanceEntreeFormate).FontSize(7);
                table.Cell().Element(TotalCell)
                    .Text(totaux.TotalDebitMouvAnnuelFormate).FontSize(7);
                table.Cell().Element(TotalCellGreen)
                    .Text(totaux.TotalDebitFormate).FontSize(9);

                // Totaux Crédit
                table.Cell().Element(TotalCell)
                    .Text(totaux.TotalCreditBalanceEntreeFormate).FontSize(7);
                table.Cell().Element(TotalCell)
                    .Text(totaux.TotalCreditMouvAnnuelFormate).FontSize(7);
                table.Cell().Element(TotalCellRed)
                    .Text(totaux.TotalCreditFormate).FontSize(9);

                // Totaux Solde
                table.Cell().Element(TotalCellGreen)
                    .Text(totaux.TotalSoldeDebiteurFormate).FontSize(9);
                table.Cell().Element(TotalCellRed)
                    .Text(totaux.TotalSoldeCrebiteurFormate).FontSize(9);
            });
        }

        #region Styles de cellules - EN-TÊTES NIVEAU 1

        private static IContainer HeaderCellNeutre(IContainer container)
        {
            return container
                .Background(GrisClair)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(GrisFonce).Bold());
        }

        private static IContainer HeaderCellDebit(IContainer container)
        {
            return container
                .Background(VertEmeraude)
                .Border(1)
                .BorderColor(BorderVertFonce)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
        }

        private static IContainer HeaderCellCredit(IContainer container)
        {
            return container
                .Background(RougeCoral)
                .Border(1)
                .BorderColor(BorderRougeFonce)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
        }

        private static IContainer HeaderCellSolde(IContainer container)
        {
            return container
                .Background(BleuIndigo)
                .Border(1)
                .BorderColor(BorderBleuFonce)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
        }

        #endregion

        #region Styles de cellules - SOUS-EN-TÊTES NIVEAU 2

        private static IContainer SubHeaderCellDebit(IContainer container)
        {
            return container
                .Background(VertClair)
                .Border(1)
                .BorderColor(BorderVertClair)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(TexteVertFonce).SemiBold());
        }

        private static IContainer SubHeaderCellDebitTotal(IContainer container)
        {
            return container
                .Background(VertClairAccent)
                .Border(1)
                .BorderColor(BorderVertAccent)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(TexteVertFonce).Bold());
        }

        private static IContainer SubHeaderCellCredit(IContainer container)
        {
            return container
                .Background(RougeClair)
                .Border(1)
                .BorderColor(BorderRougeClair)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(TexteRougeFonce).SemiBold());
        }

        private static IContainer SubHeaderCellCreditTotal(IContainer container)
        {
            return container
                .Background(RougeClairAccent)
                .Border(1)
                .BorderColor(BorderRougeAccent)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(TexteRougeFonce).Bold());
        }

        private static IContainer SubHeaderCellSolde(IContainer container)
        {
            return container
                .Background(BleuClair)
                .Border(1)
                .BorderColor(BorderBleuClair)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(TexteBleuFonce).SemiBold());
        }

        #endregion

        #region Styles de cellules - DONNÉES

        private static IContainer DataCellCustom(IContainer container, Color bgColor)
        {
            return container
                .Background(bgColor)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer DataCellLeftCustom(IContainer container, Color bgColor)
        {
            return container
                .Background(bgColor)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignLeft()
                .AlignMiddle();
        }

        private static IContainer DataCellDebitTotal(IContainer container)
        {
            return container
                .Background(VertTresClair)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.Bold().FontColor(VertEmeraude));
        }

        private static IContainer DataCellCreditTotal(IContainer container)
        {
            return container
                .Background(RougeTresClair)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.Bold().FontColor(RougeCoral));
        }

        private static IContainer DataCellSoldeDebiteur(IContainer container)
        {
            return container
                .Background(VertTresClair)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.SemiBold().FontColor(VertEmeraude));
        }

        private static IContainer DataCellSoldeCrebiteur(IContainer container)
        {
            return container
                .Background(RougeTresClair)
                .Border(1)
                .BorderColor(GrisBordure)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.SemiBold().FontColor(RougeCoral));
        }

        #endregion

        #region Styles de cellules - TOTAUX

        private static IContainer TotalCell(IContainer container)
        {
            return container
                .Background(GrisArdoise)
                .Border(1)
                .BorderColor(GrisBordureFonce)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(GrisTotaux).Bold());
        }

        private static IContainer TotalCellGreen(IContainer container)
        {
            return container
                .Background(GrisArdoise)
                .Border(1)
                .BorderColor(GrisBordureFonce)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(VertLumineux).Bold());
        }

        private static IContainer TotalCellRed(IContainer container)
        {
            return container
                .Background(GrisArdoise)
                .Border(1)
                .BorderColor(GrisBordureFonce)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(RougeLumineux).Bold());
        }

        #endregion
    }
}