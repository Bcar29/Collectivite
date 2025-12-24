using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class GrandLivrePdfExporter
    {
        // ═══════════════════════════════════════════════════════════
        // COULEURS ADAPTÉES À LA PAGE XAML
        // ═══════════════════════════════════════════════════════════
        private static readonly string BLEU_TITRE = "#1976D2";       // Numéro de compte
        private static readonly string VERT_DEBIT = "#388E3C";       // Montants débit
        private static readonly string ROUGE_CREDIT = "#D32F2F";     // Montants crédit
        private static readonly string GRIS_TEXTE = "#666666";       // Textes secondaires
        private static readonly string GRIS_FOND = "#F5F5F5";        // Fond totaux
        private static readonly string GRIS_BORDURE = "#E0E0E0";     // Bordures
        private static readonly string BLANC = "#FFFFFF";            // Fond carte

        // Couleurs du drapeau guinéen
        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";

        /// <summary>
        /// Exporte le Grand Livre en fichier PDF (format cartes comme l'application)
        /// </summary>
        public static async Task<byte[]> ExporterAsync(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var comptesAvecMouvements = comptes.Where(c => c.Mouvements.Any()).ToList();

            // Récupérer les données de la commune
            var _communeService = new CommuneService();
            var commune = await _communeService.GetCommuneByIdWithRelationsAsync(Properties.Settings.Default.CommuneId);
            var exercice = ExerciceService.Instance.CurrentExercice;

            string typeCommune = commune?.TypCommune ?? "..........";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            // Préparer le texte de période si filtre
            string? periodeTexte = null;
            if (filtre != null && filtre.Mois.HasValue)
            {
                string[] moisNoms = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                                     "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
                periodeTexte = $"Mois de {moisNoms[filtre.Mois.Value]}";
                if (filtre.Annee.HasValue)
                    periodeTexte += $" {filtre.Annee.Value}";
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ═══════════════════════════════════════════════════════════
                    // EN-TÊTE SIMPLE (pages 2 et suivantes uniquement)
                    // ═══════════════════════════════════════════════════════════
                    page.Header().Column(headerCol =>
                    {
                        // En-tête simplifié pour les pages suivantes (page > 1)
                        headerCol.Item().ShowIf(ctx => ctx.PageNumber > 1).Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"GRAND LIVRE - {nomCommune.ToUpper()}")
                                .FontSize(10).Bold().FontColor(Color.FromHex(BLEU_TITRE));

                            row.RelativeItem().AlignCenter().Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                                .FontSize(10).Bold();

                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Page ").FontSize(9);
                                text.CurrentPageNumber().FontSize(9);
                                text.Span(" / ").FontSize(9);
                                text.TotalPages().FontSize(9);
                            });
                        });

                        headerCol.Item().ShowIf(ctx => ctx.PageNumber > 1)
                            .PaddingTop(5).LineHorizontal(1).LineColor(Color.FromHex(GRIS_BORDURE));

                        headerCol.Item().ShowIf(ctx => ctx.PageNumber > 1).PaddingBottom(10);
                    });

                    // ═══════════════════════════════════════════════════════════
                    // CONTENU - En-tête officiel (première page) + Grille de cartes
                    // ═══════════════════════════════════════════════════════════
                    page.Content().Column(contentCol =>
                    {
                        // ═══════════════════════════════════════════════════════════
                        // EN-TÊTE OFFICIEL (PREMIÈRE PAGE UNIQUEMENT)
                        // ═══════════════════════════════════════════════════════════
                        contentCol.Item().ShowOnce().Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                // Colonne gauche : Ministère
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

                                // Colonne droite : République
                                row.RelativeItem(2).AlignRight().Column(rightCol =>
                                {
                                    rightCol.Item().AlignRight().Text("REPUBLIQUE DE GUINEE")
                                        .FontSize(11).Bold();
                                    rightCol.Item().AlignRight().Text("Travail - Justice - Solidarité")
                                        .FontSize(10).Italic();
                                });
                            });

                            headerCol.Item().PaddingTop(15);

                            // Titre du document
                            headerCol.Item().AlignCenter().Text("GRAND LIVRE")
                                .FontSize(18).Bold().FontColor(Color.FromHex(BLEU_TITRE));

                            // Bandeau avec commune
                            headerCol.Item().PaddingVertical(5)
                                .BorderTop(2).BorderBottom(2).BorderColor(Color.FromHex(BLEU_TITRE))
                                .Padding(8).AlignCenter()
                                .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                                .FontSize(12);

                            // Exercice
                            headerCol.Item().PaddingTop(10).AlignCenter()
                                .Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                                .FontSize(14).Bold();

                            // Période (si filtre)
                            if (!string.IsNullOrEmpty(periodeTexte))
                            {
                                headerCol.Item().AlignCenter().Text(periodeTexte).FontSize(10).Italic();
                            }

                            headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Color.FromHex(GRIS_BORDURE));
                            headerCol.Item().PaddingBottom(10);
                        });

                        // ═══════════════════════════════════════════════════════════
                        // GRILLE DE CARTES (toutes les pages)
                        // ═══════════════════════════════════════════════════════════
                        contentCol.Item().Element(c => ComposeContent(c, comptesAvecMouvements));
                    });

                    // ═══════════════════════════════════════════════════════════
                    // PIED DE PAGE (toutes les pages)
                    // ═══════════════════════════════════════════════════════════
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text($"Édité le {DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(8).Italic()
                            .FontColor(Color.FromHex(GRIS_TEXTE));

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Color.FromHex(GRIS_TEXTE)));
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });

                        row.RelativeItem().AlignRight()
                            .Text($"Nombre de comptes : {comptesAvecMouvements.Count}").FontSize(8).Italic()
                            .FontColor(Color.FromHex(GRIS_TEXTE));
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Surcharge synchrone pour compatibilité
        /// </summary>
        public static byte[] Exporter(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            return ExporterAsync(comptes, filtre).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Compose le contenu principal - Grille de cartes
        /// </summary>
        private static void ComposeContent(IContainer container, List<GrandLivreCompteDTO> comptes)
        {
            container.Column(column =>
            {
                int comptesParLigne = 4;

                for (int i = 0; i < comptes.Count; i += comptesParLigne)
                {
                    var comptesLigne = comptes.Skip(i).Take(comptesParLigne).ToList();

                    // ✅ ShowEntire() empêche la ligne d'être coupée entre deux pages
                    column.Item().ShowEntire().Row(row =>
                    {
                        foreach (var compte in comptesLigne)
                        {
                            // ✅ ShowEntire() sur chaque carte pour éviter toute coupure
                            row.RelativeItem().Padding(4).ShowEntire().Element(c => DessinerCarte(c, compte));
                        }

                        int casesVides = comptesParLigne - comptesLigne.Count;
                        for (int j = 0; j < casesVides; j++)
                        {
                            row.RelativeItem().Padding(4);
                        }
                    });

                    column.Item().PaddingBottom(6);
                }
            });
        }

        /// <summary>
        /// Dessine une carte de compte (style identique à la page XAML)
        /// </summary>
        private static void DessinerCarte(IContainer container, GrandLivreCompteDTO compte)
        {
            container
                .Border(1)
                .BorderColor(Color.FromHex(GRIS_BORDURE))
                .Background(Color.FromHex(BLANC))
                .Column(column =>
                {
                    // ═══════════════════════════════════════
                    // EN-TÊTE : Numéro du compte (style XAML)
                    // ═══════════════════════════════════════
                    column.Item()
                        .Padding(8)
                        .AlignCenter()
                        .Text(compte.NumeroCompte)
                        .FontSize(16)
                        .Bold()
                        .FontColor(Color.FromHex(BLEU_TITRE));  // #1976D2

                    // ═══════════════════════════════════════
                    // INTITULÉ du compte
                    // ═══════════════════════════════════════
                    column.Item()
                        .BorderBottom(1)
                        .BorderColor(Color.FromHex(GRIS_BORDURE))
                        .PaddingHorizontal(4)
                        .PaddingBottom(6)
                        .AlignCenter()
                        .Text(compte.IntituleCompte)
                        .FontSize(9)
                        .FontColor(Color.FromHex(GRIS_TEXTE));  // #666666

                    // ═══════════════════════════════════════
                    // EN-TÊTES DÉBIT / CRÉDIT
                    // ═══════════════════════════════════════
                    column.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .BorderBottom(1)
                            .BorderColor(Color.FromHex(GRIS_BORDURE))
                            .Padding(4)
                            .AlignCenter()
                            .Text("Débit")
                            .FontSize(11)
                            .SemiBold()
                            .FontColor(Color.FromHex(GRIS_TEXTE));  // #666666

                        row.RelativeItem()
                            .BorderBottom(1)
                            .BorderColor(Color.FromHex(GRIS_BORDURE))
                            .Padding(4)
                            .AlignCenter()
                            .Text("Crédit")
                            .FontSize(11)
                            .SemiBold()
                            .FontColor(Color.FromHex(GRIS_TEXTE));  // #666666
                    });

                    // ═══════════════════════════════════════
                    // MOUVEMENTS
                    // ═══════════════════════════════════════
                    column.Item().Row(row =>
                    {
                        // Colonne Débit
                        row.RelativeItem().Column(colDebit =>
                        {
                            foreach (var mvt in compte.Mouvements)
                            {
                                colDebit.Item()
                                    .BorderBottom(1)
                                    .BorderColor(Color.FromHex(GRIS_BORDURE))
                                    .Padding(3)
                                    .AlignCenter()
                                    .Text(mvt.MontantDebit > 0 ? $"{mvt.MontantDebit:N0}" : "")
                                    .FontSize(10)
                                    .FontColor(Color.FromHex(VERT_DEBIT));  // #388E3C
                            }
                        });

                        // Colonne Crédit
                        row.RelativeItem().Column(colCredit =>
                        {
                            foreach (var mvt in compte.Mouvements)
                            {
                                colCredit.Item()
                                    .BorderBottom(1)
                                    .BorderColor(Color.FromHex(GRIS_BORDURE))
                                    .Padding(3)
                                    .AlignCenter()
                                    .Text(mvt.MontantCredit > 0 ? $"{mvt.MontantCredit:N0}" : "")
                                    .FontSize(10)
                                    .FontColor(Color.FromHex(ROUGE_CREDIT));  // #D32F2F
                            }
                        });
                    });

                    // ═══════════════════════════════════════
                    // TOTAUX (fond gris comme la page XAML)
                    // ═══════════════════════════════════════
                    column.Item().Background(Color.FromHex(GRIS_FOND)).Row(row =>  // #F5F5F5
                    {
                        row.RelativeItem()
                            .Padding(6)
                            .AlignCenter()
                            .Text($"{compte.TotalDebit:N0}")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Color.FromHex(VERT_DEBIT));  // #388E3C

                        row.RelativeItem()
                            .Padding(6)
                            .AlignCenter()
                            .Text($"{compte.TotalCredit:N0}")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Color.FromHex(ROUGE_CREDIT));  // #D32F2F
                    });

                    // ═══════════════════════════════════════
                    // SOLDE
                    // ═══════════════════════════════════════
                    column.Item()
                        .Background(Color.FromHex(GRIS_FOND))  // #F5F5F5
                        .Padding(4)
                        .AlignCenter()
                        .Text(compte.SoldeFormate)
                        .FontSize(9)
                        .FontColor(Color.FromHex(GRIS_TEXTE));  // #666666
                });
        }
    }
}