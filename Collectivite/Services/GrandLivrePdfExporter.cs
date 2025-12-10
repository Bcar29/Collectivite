
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Services
{
    public class GrandLivrePdfExporter
    {
        /// <summary>
        /// Exporte le Grand Livre en fichier PDF (format cartes comme l'application)
        /// </summary>
        public static byte[] Exporter(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            // Configuration de la licence (Community pour usage gratuit)
            QuestPDF.Settings.License = LicenseType.Community;

            var comptesAvecMouvements = comptes.Where(c => c.Mouvements.Any()).ToList();
            string titre = ConstruireTitre(filtre);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // En-tête
                    page.Header().Element(c => ComposeHeader(c, titre));

                    // Contenu - Grille de cartes
                    page.Content().Element(c => ComposeContent(c, comptesAvecMouvements));

                    // Pied de page
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Compose l'en-tête du document
        /// </summary>
        private static void ComposeHeader(IContainer container, string titre)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(titre)
                        .FontSize(22)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    row.ConstantItem(150).AlignRight().Text($"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });

                column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                column.Item().PaddingBottom(10);
            });
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

        /// <summary>
        /// Compose le pied de page
        /// </summary>
        private static void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem()
                        .Text("Plateforme de Gestion Budgétaire et Comptable des Collectivités Locales")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                    row.RelativeItem()
                        .AlignRight()
                        .Text(x =>
                        {
                            x.Span("Page ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" / ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                });
            });
        }

        /// <summary>
        /// Construit le titre selon les filtres
        /// </summary>
        private static string ConstruireTitre(GrandLivreFiltreDTO? filtre)
        {
            var parties = new List<string> { "Grand Livre" };

            if (filtre != null)
            {
                if (filtre.Mois.HasValue)
                {
                    string[] mois = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                                     "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
                    parties.Add($"du mois de {mois[filtre.Mois.Value]}");
                }

                if (filtre.Annee.HasValue)
                {
                    parties.Add(filtre.Annee.Value.ToString());
                }
            }

            return string.Join(" ", parties);
        }
    }
}