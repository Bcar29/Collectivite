using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Collectivite.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour générer des rapports PDF pour les communes
    /// </summary>
    public class CommunePdfService
    {
        public CommunePdfService()
        {
            // Configuration de la licence QuestPDF (Community)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Génère un rapport PDF complet pour une commune avec ses détails
        /// </summary>
        public string GenerateRapportCommune(Commune commune, DetailCommune? detailCommune)
        {
            try
            {
                // Créer le nom du fichier
                string fileName = $"Rapport_Commune_{commune.Nom}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Rapports_Communes",
                    fileName
                );

                // Créer le dossier si nécessaire
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                // Générer le PDF
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                        // En-tête
                        page.Header().Element(container => ComposeHeader(container, commune));

                        // Contenu
                        page.Content().Element(container => ComposeContent(container, commune, detailCommune));

                        // Pied de page
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(outputPath);

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la génération du PDF : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Compose l'en-tête du document
        /// </summary>
        private void ComposeHeader(IContainer container, Commune commune)
        {
            container.Column(column =>
            {
                // Titre principal
                column.Item().AlignCenter().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("RÉPUBLIQUE DE GUINÉE")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().AlignCenter().Text("Travail - Justice - Solidarité")
                            .FontSize(11).Italic().FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(2).LineColor(Colors.Blue.Darken3);

                // Titre du rapport
                column.Item().PaddingVertical(15).AlignCenter().Text($"RAPPORT DE LA COMMUNE DE {commune.Nom.ToUpper()}")
                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken4);

                column.Item().PaddingBottom(5).AlignCenter().Text($"Type: {commune.TypCommune}")
                    .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                column.Item().PaddingBottom(15).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        /// <summary>
        /// Compose le contenu principal du document
        /// </summary>
        private void ComposeContent(IContainer container, Commune commune, DetailCommune? detailCommune)
        {
            container.Column(column =>
            {
                // Section 1: Informations Générales
                column.Item().Element(c => ComposeSectionInformationsGenerales(c, commune));

                // Section 2: Localisation Géographique
                column.Item().Element(c => ComposeSectionLocalisation(c, commune));

                // Si des détails existent, afficher les sections supplémentaires
                if (detailCommune != null)
                {
                    // Section 3: Administration
                    column.Item().Element(c => ComposeSectionAdministration(c, detailCommune));

                    // Section 4: Démographie
                    column.Item().Element(c => ComposeSectionDemographie(c, detailCommune));

                    // Section 5: Éducation
                    column.Item().Element(c => ComposeSectionEducation(c, detailCommune));

                    // Section 6: Santé
                    column.Item().Element(c => ComposeSectionSante(c, detailCommune));

                    // Section 7: Infrastructures et Ressources
                    column.Item().Element(c => ComposeSectionInfrastructures(c, detailCommune));

                    // Section 8: Économie et Sécurité
                    column.Item().Element(c => ComposeSectionEconomie(c, detailCommune));
                }
                else
                {
                    column.Item().PaddingVertical(20).AlignCenter()
                        .Background(Colors.Grey.Lighten3)
                        .Padding(15)
                        .Text("Aucun détail disponible pour cette commune")
                        .FontSize(12).Italic().FontColor(Colors.Grey.Darken2);
                }
            });
        }

        /// <summary>
        /// Section Informations Générales
        /// </summary>
        private void ComposeSectionInformationsGenerales(IContainer container, Commune commune)
        {
            container.PaddingBottom(15).Column(column =>
            {
                // Titre de section
                column.Item().Background(Colors.Blue.Lighten4).Padding(10)
                    .Text(" INFORMATIONS GÉNÉRALES")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken4);

                // Contenu
                column.Item().Padding(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Nom de la commune :").SemiBold();
                        row.RelativeItem().Text(commune.Nom);
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Type de commune :").SemiBold();
                        row.RelativeItem().Text(commune.TypCommune);
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Région administrative :").SemiBold();
                        row.RelativeItem().Text(commune.RegionCommune);
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Préfecture :").SemiBold();
                        row.RelativeItem().Text(string.IsNullOrWhiteSpace(commune.PrefectureCommune)
                            ? "Non renseignée"
                            : commune.PrefectureCommune);
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Date de création :").SemiBold();
                        row.RelativeItem().Text(commune.DateCreation.ToString("dd MMMM yyyy"));
                    });
                });
            });
        }

        /// <summary>
        /// Section Localisation Géographique
        /// </summary>
        private void ComposeSectionLocalisation(IContainer container, Commune commune)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Green.Lighten4).Padding(10)
                    .Text(" LOCALISATION GÉOGRAPHIQUE")
                    .FontSize(14).Bold().FontColor(Colors.Green.Darken4);

                column.Item().Padding(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    // En-tête
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Green.Lighten3)
                            .Padding(8).Text("Destination").SemiBold();
                        header.Cell().Background(Colors.Green.Lighten3)
                            .Padding(8).AlignRight().Text("Distance (km)").SemiBold();
                    });

                    // Lignes
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Chef-Lieu de Province");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{commune.DistanceChefLieuProvince:N2}");

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Chef-Lieu de Région");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{commune.DistanceChefLieuRegion:N2}");

                    table.Cell().Padding(8).Text("Capitale (Conakry)");
                    table.Cell().Padding(8).AlignRight().Text($"{commune.DistanceCapitale:N2}");
                });
            });
        }

        /// <summary>
        /// Section Administration
        /// </summary>
        private void ComposeSectionAdministration(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Orange.Lighten4).Padding(10)
                    .Text(" ADMINISTRATION")
                    .FontSize(14).Bold().FontColor(Colors.Orange.Darken4);

                column.Item().Padding(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(2).Text("Nombre de conseillers").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.NombreConseillers.ToString());
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Délégations spéciales").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.NombreDelegationSpeciale.ToString());
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Effectif total du personnel").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.EffectifTotalPersonnel.ToString());
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Personnel permanent").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.EffectifPermanent.ToString());
                    });

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Personnel temporaire").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.EffectifTemporaire.ToString());
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Divisions administratives").SemiBold();
                        row.RelativeItem().AlignRight().Text("");
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Quartiers").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreQuartiers.ToString());
                    });

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Districts").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreDistricts.ToString());
                    });

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Secteurs").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreSecteurs.ToString());
                    });
                });
            });
        }

        /// <summary>
        /// Section Démographie
        /// </summary>
        private void ComposeSectionDemographie(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Purple.Lighten4).Padding(10)
                    .Text(" DÉMOGRAPHIE")
                    .FontSize(14).Bold().FontColor(Colors.Purple.Darken4);

                column.Item().Padding(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    // En-tête
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Purple.Lighten3)
                            .Padding(8).Text("Indicateur").SemiBold();
                        header.Cell().Background(Colors.Purple.Lighten3)
                            .Padding(8).AlignRight().Text("Valeur").SemiBold();
                        header.Cell().Background(Colors.Purple.Lighten3)
                            .Padding(8).AlignRight().Text("%").SemiBold();
                    });

                    // Population totale
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Population totale").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{detail.PopulationTotale:N0}").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text("100%");

                    // Femmes
                    decimal percentFemmes = detail.PopulationTotale > 0
                        ? (decimal)detail.PopulationFemmes / detail.PopulationTotale * 100
                        : 0;
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("  • Femmes");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{detail.PopulationFemmes:N0}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{percentFemmes:N1}%");

                    // Hommes
                    decimal percentHommes = detail.PopulationTotale > 0
                        ? (decimal)detail.PopulationHommes / detail.PopulationTotale * 100
                        : 0;
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("  • Hommes");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{detail.PopulationHommes:N0}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{percentHommes:N1}%");

                    // Superficie
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Superficie");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"{detail.Superficie:N2} km²");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text("-");

                    // Densité
                    table.Cell().Padding(8).Text("Densité de population");
                    table.Cell().Padding(8).AlignRight().Text($"{detail.Densite:N2} hab/km²");
                    table.Cell().Padding(8).AlignRight().Text("-");
                });
            });
        }

        /// <summary>
        /// Section Éducation
        /// </summary>
        private void ComposeSectionEducation(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Teal.Lighten4).Padding(10)
                    .Text(" ÉDUCATION")
                    .FontSize(14).Bold().FontColor(Colors.Teal.Darken4);

                column.Item().Padding(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    // En-tête
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Teal.Lighten3)
                            .Padding(8).Text("Niveau").SemiBold();
                        header.Cell().Background(Colors.Teal.Lighten3)
                            .Padding(8).AlignCenter().Text("Écoles").SemiBold();
                        header.Cell().Background(Colors.Teal.Lighten3)
                            .Padding(8).AlignCenter().Text("Classes").SemiBold();
                        header.Cell().Background(Colors.Teal.Lighten3)
                            .Padding(8).AlignCenter().Text("Élèves").SemiBold();
                    });

                    // Préscolaire
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Préscolaire");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreEcolesPrescolaire.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreClassesPrescolaire.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreElevesPrescolaire.ToString());

                    // Primaire
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Primaire");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreEcolesPrimaire.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreClassesPrimaire.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreElevesPrimaire.ToString());

                    // Collège
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Collège");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreEcolesCollege.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreClassesCollege.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreElevesCollege.ToString());

                    // Lycée
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text("Lycée");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreEcolesLycee.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreClassesLycee.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(detail.NombreElevesLycee.ToString());

                    // Total
                    int totalEcoles = detail.NombreEcolesPrescolaire + detail.NombreEcolesPrimaire +
                                     detail.NombreEcolesCollege + detail.NombreEcolesLycee;
                    int totalClasses = detail.NombreClassesPrescolaire + detail.NombreClassesPrimaire +
                                      detail.NombreClassesCollege + detail.NombreClassesLycee;
                    int totalEleves = detail.NombreElevesPrescolaire + detail.NombreElevesPrimaire +
                                     detail.NombreElevesCollege + detail.NombreElevesLycee;

                    table.Cell().Background(Colors.Teal.Lighten5).Padding(8).Text("TOTAL").Bold();
                    table.Cell().Background(Colors.Teal.Lighten5).Padding(8).AlignCenter().Text(totalEcoles.ToString()).Bold();
                    table.Cell().Background(Colors.Teal.Lighten5).Padding(8).AlignCenter().Text(totalClasses.ToString()).Bold();
                    table.Cell().Background(Colors.Teal.Lighten5).Padding(8).AlignCenter().Text(totalEleves.ToString()).Bold();
                });
            });
        }

        /// <summary>
        /// Section Santé
        /// </summary>
        private void ComposeSectionSante(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Red.Lighten4).Padding(10)
                    .Text(" INFRASTRUCTURES SANITAIRES")
                    .FontSize(14).Bold().FontColor(Colors.Red.Darken4);

                column.Item().Padding(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(2).Text("Centres de santé").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.NombreCentresSante.ToString());
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Postes de santé").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.NombrePostesSante.ToString());
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Structures de santé améliorée").SemiBold();
                        row.RelativeItem().AlignRight().Text(detail.NombreSanteAmelioree.ToString());
                    });

                    int totalSante = detail.NombreCentresSante + detail.NombrePostesSante + detail.NombreSanteAmelioree;
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("Total structures sanitaires").Bold();
                        row.RelativeItem().AlignRight().Text(totalSante.ToString()).Bold();
                    });
                });
            });
        }

        /// <summary>
        /// Section Infrastructures et Ressources
        /// </summary>
        private void ComposeSectionInfrastructures(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Cyan.Lighten4).Padding(10)
                    .Text(" INFRASTRUCTURES ET RESSOURCES")
                    .FontSize(14).Bold().FontColor(Colors.Cyan.Darken4);

                column.Item().Padding(10).Column(col =>
                {
                    // Eau
                    col.Item().Text("Accès à l'eau").SemiBold().FontColor(Colors.Cyan.Darken3);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Points d'eau").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombrePointsEau.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Forages").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreForages.ToString());
                    });

                    // Organisations
                    col.Item().PaddingTop(10).Text("Organisations de la société civile").SemiBold().FontColor(Colors.Cyan.Darken3);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Associations").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreAssociation.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • ONG nationales").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreOngNationales.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • ONG étrangères").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreOngEtrangeres.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Groupements").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreGroupements.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Coopératives").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreCooperatives.ToString());
                    });
                });
            });
        }

        /// <summary>
        /// Section Économie et Sécurité
        /// </summary>
        private void ComposeSectionEconomie(IContainer container, DetailCommune detail)
        {
            container.PaddingBottom(15).Column(column =>
            {
                column.Item().Background(Colors.Amber.Lighten4).Padding(10)
                    .Text(" ÉCONOMIE ET SÉCURITÉ")
                    .FontSize(14).Bold().FontColor(Colors.Amber.Darken4);

                column.Item().Padding(10).Column(col =>
                {
                    // Marchés
                    col.Item().Text("Activités commerciales").SemiBold().FontColor(Colors.Amber.Darken3);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Marchés journaliers").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreMarchesJournaliers.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Marchés hebdomadaires").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreMarchesHebdomadaires.ToString());
                    });
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Total marchés").FontSize(10).Bold();
                        row.RelativeItem().AlignRight().Text(detail.NombreMarches.ToString()).Bold();
                    });

                    // Sécurité
                    col.Item().PaddingTop(10).Text("Sécurité").SemiBold().FontColor(Colors.Amber.Darken3);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(2).Text("  • Détenteurs d'armes à feu déclarés").FontSize(10);
                        row.RelativeItem().AlignRight().Text(detail.NombreDetenteursArmesFeu.ToString());
                    });
                });
            });
        }

        /// <summary>
        /// Compose le pied de page
        /// </summary>
        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().AlignLeft()
                        .Text($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber().FontSize(9);
                            text.Span(" / ").FontSize(9);
                            text.TotalPages().FontSize(9);
                        });

                    row.RelativeItem().AlignRight()
                        .Text("Système de Gestion des Collectivités")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        /// <summary>
        /// Ouvre le PDF après génération
        /// </summary>
        public void OpenPdf(string pdfPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Impossible d'ouvrir le PDF : {ex.Message}", ex);
            }
        }
    }
}