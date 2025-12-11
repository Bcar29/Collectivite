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

        
        /// <summary>
        /// Exporte le Grand Livre en fichier PDF (format cartes comme l'application)
        /// </summary>
        public static async Task<byte[]> ExporterAsync( List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            // Configuration de la licence (Community pour usage gratuit)
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

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ═══════════════════════════════════════════════════════════
                    // EN-TÊTE OFFICIEL (identique au Livre Journal)
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

                        headerCol.Item().PaddingTop(15);

                        headerCol.Item().AlignCenter().Text("GRAND LIVRE")
                            .FontSize(18).Bold();

                        headerCol.Item().PaddingVertical(5)
                            .BorderTop(2).BorderBottom(2).BorderColor(Colors.Green.Darken1)
                            .Padding(8).AlignCenter()
                            .Text($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}")
                            .FontSize(12);

                        headerCol.Item().PaddingTop(10).AlignCenter()
                            .Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                            .FontSize(14).Bold();

                        // Afficher le filtre de période si présent
                        if (filtre != null && filtre.Mois.HasValue)
                        {
                            string[] moisNoms = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                                                 "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
                            string periode = $"Mois de {moisNoms[filtre.Mois.Value]}";
                            if (filtre.Annee.HasValue)
                                periode += $" {filtre.Annee.Value}";

                            headerCol.Item().AlignCenter().Text(periode).FontSize(10).Italic();
                        }

                        headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // ═══════════════════════════════════════════════════════════
                    // CONTENU - Grille de cartes
                    // ═══════════════════════════════════════════════════════════
                    page.Content().PaddingVertical(10).Element(c => ComposeContent(c, comptesAvecMouvements));

                    // ═══════════════════════════════════════════════════════════
                    // PIED DE PAGE (identique au Livre Journal)
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
                            .Text($"Nombre de comptes : {comptesAvecMouvements.Count}").FontSize(8).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Surcharge synchrone pour compatibilité (appelle la version async)
        /// </summary>
        public static byte[] Exporter(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            // Version simplifiée sans les informations de la commune
            // Pour utiliser l'en-tête complet, utilisez ExporterAsync
            return ExporterSansCommune(comptes, filtre);
        }

        /// <summary>
        /// Export sans informations de commune (version de compatibilité)
        /// </summary>
        private static byte[] ExporterSansCommune(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var comptesAvecMouvements = comptes.Where(c => c.Mouvements.Any()).ToList();
            var exercice = ExerciceService.Instance.CurrentExercice;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ═══════════════════════════════════════════════════════════
                    // EN-TÊTE OFFICIEL (version sans commune spécifique)
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

                        headerCol.Item().PaddingTop(15);

                        headerCol.Item().AlignCenter().Text("GRAND LIVRE")
                            .FontSize(18).Bold();

                        headerCol.Item().PaddingVertical(5)
                            .BorderTop(2).BorderBottom(2).BorderColor(Colors.Green.Darken1)
                            .Padding(8).AlignCenter()
                            .Text("COMPTABILITÉ GÉNÉRALE")
                            .FontSize(12);

                        headerCol.Item().PaddingTop(10).AlignCenter()
                            .Text($"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}")
                            .FontSize(14).Bold();

                        // Afficher le filtre de période si présent
                        if (filtre != null && filtre.Mois.HasValue)
                        {
                            string[] moisNoms = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                                                 "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
                            string periode = $"Mois de {moisNoms[filtre.Mois.Value]}";
                            if (filtre.Annee.HasValue)
                                periode += $" {filtre.Annee.Value}";

                            headerCol.Item().AlignCenter().Text(periode).FontSize(10).Italic();
                        }

                        headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // Contenu - Grille de cartes
                    page.Content().PaddingVertical(10).Element(c => ComposeContent(c, comptesAvecMouvements));

                    // Pied de page
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
                            .Text($"Nombre de comptes : {comptesAvecMouvements.Count}").FontSize(8).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Compose le contenu principal - Grille de cartes
        /// </summary>
        private static void ComposeContent(IContainer container, List<GrandLivreCompteDTO> comptes)
        {
            container.Column(column =>
            {
                // Afficher les comptes en grille de 4 colonnes
                int comptesParLigne = 4;

                for (int i = 0; i < comptes.Count; i += comptesParLigne)
                {
                    var comptesLigne = comptes.Skip(i).Take(comptesParLigne).ToList();

                    column.Item().Row(row =>
                    {
                        foreach (var compte in comptesLigne)
                        {
                            row.RelativeItem().Padding(3).Element(c => DessinerCarte(c, compte));
                        }

                        // Remplir les cases vides si nécessaire
                        int casesVides = comptesParLigne - comptesLigne.Count;
                        for (int j = 0; j < casesVides; j++)
                        {
                            row.RelativeItem().Padding(3);
                        }
                    });

                    column.Item().PaddingBottom(5);
                }
            });
        }

        /// <summary>
        /// Dessine une carte de compte
        /// </summary>
        private static void DessinerCarte(IContainer container, GrandLivreCompteDTO compte)
        {
            container.Border(1).BorderColor(Colors.Grey.Darken1).Column(column =>
            {
                // ═══════════════════════════════════════
                // EN-TÊTE : Numéro du compte
                // ═══════════════════════════════════════
                column.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(5)
                    .AlignCenter()
                    .Text(compte.NumeroCompte)
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                // ═══════════════════════════════════════
                // INTITULÉ du compte
                // ═══════════════════════════════════════
                column.Item()
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Padding(3)
                    .AlignCenter()
                    .Text(compte.IntituleCompte)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);

                // ═══════════════════════════════════════
                // EN-TÊTES DÉBIT / CRÉDIT
                // ═══════════════════════════════════════
                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Background(Colors.Green.Lighten4)
                        .Padding(3)
                        .AlignCenter()
                        .Text("Débit")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken2);

                    row.RelativeItem()
                        .Background(Colors.Red.Lighten4)
                        .Padding(3)
                        .AlignCenter()
                        .Text("Crédit")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken2);
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
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(2)
                                .AlignCenter()
                                .Text(mvt.MontantDebit > 0 ? $"{mvt.MontantDebit:N0}" : "")
                                .FontSize(9)
                                .FontColor(Colors.Green.Darken3);
                        }
                    });

                    // Colonne Crédit
                    row.RelativeItem().Column(colCredit =>
                    {
                        foreach (var mvt in compte.Mouvements)
                        {
                            colCredit.Item()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(2)
                                .AlignCenter()
                                .Text(mvt.MontantCredit > 0 ? $"{mvt.MontantCredit:N0}" : "")
                                .FontSize(9)
                                .FontColor(Colors.Red.Darken3);
                        }
                    });
                });

                // ═══════════════════════════════════════
                // TOTAUX
                // ═══════════════════════════════════════
                column.Item().Background(Colors.Grey.Lighten3).Row(row =>
                {
                    row.RelativeItem()
                        .Padding(3)
                        .AlignCenter()
                        .Text($"{compte.TotalDebit:N0}")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Green.Darken3);

                    row.RelativeItem()
                        .Padding(3)
                        .AlignCenter()
                        .Text($"{compte.TotalCredit:N0}")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Red.Darken3);
                });

                // ═══════════════════════════════════════
                // SOLDE
                // ═══════════════════════════════════════
                column.Item()
                    .Background(Colors.Grey.Lighten4)
                    .Padding(3)
                    .AlignCenter()
                    .Text(compte.SoldeFormate)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }
    }
}