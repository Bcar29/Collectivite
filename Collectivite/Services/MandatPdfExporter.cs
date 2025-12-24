using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Collectivite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class MandatPdfExporter
    {
        // ═══════════════════════════════════════
        // PALETTE DE COULEURS
        // ═══════════════════════════════════════

        private static readonly string BleuPrimaire = "#1976D2";
        private static readonly string VertEmeraude = "#059669";
        private static readonly string VertSucces = "#388E3C";
        private static readonly string VertFonce = "#2E7D32";
        private static readonly string OrangeMoyen = "#F57C00";
        private static readonly string RougeMoyen = "#D32F2F";

        private static readonly string BleuTresClair = "#E3F2FD";
        private static readonly string VertClair = "#D1FAE5";
        private static readonly string VertPaleClair = "#E8F5E9";
        private static readonly string OrangeClair = "#FFF3E0";
        private static readonly string RougeClair = "#FFEBEE";
        private static readonly string GrisClair = "#F5F5F5";
        private static readonly string VertLettres = "#F9FBE7";

        private static readonly string GrisArdoise = "#1E293B";
        private static readonly string GrisFonce = "#475569";
        private static readonly string GrisTexte = "#64748B";
        private static readonly string VertTexte = "#33691E";

        private static readonly string GrisBordure = "#E0E0E0";
        private static readonly string GrisFiligrane = "#F0F0F0";
        // Couleurs du drapeau guinéen
        private static readonly string DrapeauRouge = "#CE1126";
        private static readonly string DrapeauJaune = "#FCD116";
        private static readonly string DrapeauVert = "#009460";

        /// <summary>
        /// Exporte le mandat en PDF
        /// </summary>
        public static async Task<byte[]> ExporterAsync(Mandat mandat, Commune commune, Mouvement? mouvement = null, List<Mouvement>? tousLesMouvements = null)
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
                            // TITRE + BADGES
                            // ═══════════════════════════════════════════════════════════
                            col.Item().AlignCenter().Row(titleRow =>
                            {
                                titleRow.AutoItem().Text("MANDAT")
                                    .FontSize(18).Bold().FontColor(Color.FromHex(GrisArdoise));

                                // Badge d'état
                                titleRow.AutoItem().PaddingLeft(10).Element(badge =>
                                {
                                    string etatTexte = mandat.Etat.ToString().Replace("_", " ");
                                    string badgeColor = GetEtatColor(mandat.Etat);

                                    badge.Background(Color.FromHex(badgeColor))
                                        .Padding(4, 0)
                                        .Text(etatTexte)
                                        .FontSize(7)
                                        .Bold()
                                        .FontColor(Colors.White);
                                });

                                // Badge de statut
                                titleRow.AutoItem().PaddingLeft(8).Element(badge =>
                                {
                                    string statutTexte = mandat.Status.ToString().Replace("_", " ");
                                    string badgeColor = GetStatutColor(mandat.Status);

                                    badge.Background(Color.FromHex(badgeColor))
                                        .Padding(4, 0)
                                        .Text(statutTexte)
                                        .FontSize(7)
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
                                    .FontSize(9).SemiBold().FontColor(Color.FromHex("#065F46"));
                            });

                            col.Item().PaddingTop(3).AlignCenter()
                                .Text(mandat.Engagement?.Exercice?.Libelle ?? DateTime.Now.Year.ToString())
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

                                    AjouterLigneInfo(table, "N° Mandat :", mandat.NumeroMandat ?? "-");
                                    AjouterLigneInfo(table, "Bordereau :", mandat.Bordereau ?? "Non renseigné");
                                    AjouterLigneInfo(table, "Mois :", mandat.Mois.ToString() ?? "-");
                                    AjouterLigneInfo(table, "Date émission :", mandat.DateEmission.ToString("dd/MM/yyyy"));
                                });

                                infoRow.ConstantItem(15);

                                infoRow.RelativeItem().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(70);
                                        columns.RelativeColumn();
                                    });

                                    AjouterLigneInfo(table, "Exercice :", mandat.Engagement?.Exercice?.Libelle ?? "-");
                                    AjouterLigneInfo(table, "Tiers :", mandat.Engagement?.Tiers?.NomComplet ?? "Non spécifié");
                                    AjouterLigneInfo(table, "Engagement :", TronquerTexte(mandat.Engagement?.Objet, 30));
                                    AjouterLigneInfo(table, "Objet :", TronquerTexte(mandat.Objet, 30));
                                });
                            });

                            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                            // ═══════════════════════════════════════════════════════════
                            // DÉCOMPOSITION DES MONTANTS
                            // ═══════════════════════════════════════════════════════════
                            col.Item().PaddingTop(10).Text("💰 Décomposition des montants")
                                .FontSize(10).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                            col.Item().PaddingTop(5).Row(montantRow =>
                            {
                                // Montant Brut
                                montantRow.RelativeItem().Background(Color.FromHex(BleuTresClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("Montant Brut").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{mandat.MontantBrut:N0} GNF")
                                            .FontSize(11).Bold().FontColor(Color.FromHex(BleuPrimaire));
                                    });

                                montantRow.ConstantItem(6);

                                // RTS
                                montantRow.RelativeItem().Background(Color.FromHex(RougeClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("RTS").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{mandat.Rts:N0} GNF")
                                            .FontSize(11).Bold().FontColor(Color.FromHex(RougeMoyen));
                                    });

                                montantRow.ConstantItem(6);

                                // Autres Précomptes
                                montantRow.RelativeItem().Background(Color.FromHex(OrangeClair))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text("Autres précomptes").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        c.Item().PaddingTop(3).Text($"{mandat.AutresPrecomptes:N0} GNF")
                                            .FontSize(11).Bold().FontColor(Color.FromHex(OrangeMoyen));
                                    });
                            });

                            // Montant Net
                            col.Item().PaddingTop(8).Background(Color.FromHex(VertPaleClair))
                                .Border(1.5f).BorderColor(Color.FromHex(VertSucces))
                                .Padding(10).Row(row =>
                                {
                                    row.RelativeItem().AlignLeft().AlignMiddle()
                                        .Text("MONTANT NET À PAYER :").FontSize(10).SemiBold();
                                    row.RelativeItem().AlignRight().AlignMiddle()
                                        .Text($"{mandat.MontantNet:N0} GNF")
                                        .FontSize(14).Bold().FontColor(Color.FromHex(VertFonce));
                                });

                            // Montant en lettres
                            col.Item().PaddingTop(6).Background(Color.FromHex(VertLettres))
                                .Padding(8).Column(innerCol =>
                                {
                                    innerCol.Item().Text("Montant en lettres :")
                                        .FontSize(7).SemiBold().FontColor(Color.FromHex(GrisTexte));
                                    innerCol.Item().PaddingTop(2)
                                        .Text(mandat.MontantLettre ?? "-")
                                        .FontSize(9).Italic().FontColor(Color.FromHex(VertTexte));
                                });

                            // ═══════════════════════════════════════════════════════════
                            // HISTORIQUE DES PAIEMENTS (TOUS les mouvements)
                            // ═══════════════════════════════════════════════════════════
                            if (tousLesMouvements != null && tousLesMouvements.Any())
                            {
                                col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Color.FromHex(GrisBordure));

                                col.Item().PaddingTop(10).Text("💳 Historique des paiements")
                                    .FontSize(10).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                                // Boucle sur TOUS les mouvements
                                foreach (var mvt in tousLesMouvements)
                                {
                                    col.Item().PaddingTop(6).Background(Color.FromHex(GrisClair))
                                        .Padding(8).Column(mvtCol =>
                                        {
                                            mvtCol.Item().Row(mvtRow =>
                                            {
                                                // Colonne gauche
                                                mvtRow.RelativeItem().Table(table =>
                                                {
                                                    table.ColumnsDefinition(columns =>
                                                    {
                                                        columns.ConstantColumn(80);
                                                        columns.RelativeColumn();
                                                    });

                                                    AjouterLigneInfo(table, "Date :", mvt.Date.ToString("dd/MM/yyyy"));

                                                    table.Cell().PaddingVertical(2).Text("Montant :").FontSize(8).SemiBold();
                                                    table.Cell().PaddingVertical(2).Text($"{mvt.Montant:N0} GNF")
                                                        .FontSize(8).SemiBold().FontColor(Color.FromHex(VertSucces));
                                                });

                                                mvtRow.ConstantItem(15);

                                                // Colonne droite
                                                mvtRow.RelativeItem().Table(table =>
                                                {
                                                    table.ColumnsDefinition(columns =>
                                                    {
                                                        columns.ConstantColumn(80);
                                                        columns.RelativeColumn();
                                                    });

                                                    string modePaiement = GetModePaiement(mvt);
                                                    table.Cell().PaddingVertical(2).Text("Mode :").FontSize(8).SemiBold();
                                                    table.Cell().PaddingVertical(2).Text(modePaiement)
                                                        .FontSize(8).SemiBold().FontColor(Color.FromHex(BleuPrimaire));

                                                    // Afficher la référence selon le mode
                                                    if (!string.IsNullOrEmpty(mvt.RefVirement))
                                                    {
                                                        AjouterLigneInfo(table, "Réf. virement :", mvt.RefVirement);
                                                        AjouterLigneInfo(table, "Num Compte :", mvt.NumBanqueBenef);
                                                    }
                                                    else if (!string.IsNullOrEmpty(mvt.RefChèque))
                                                    {
                                                        AjouterLigneInfo(table, "Réf. chèque :", mvt.RefChèque);
                                                    }
                                                    else
                                                    {
                                                        AjouterLigneInfo(table, "Référence :", "Espèces");
                                                    }

                                                    
                                                });
                                            });
                                        });
                                }
                            }

                            // ═══════════════════════════════════════════════════════════
                            // SIGNATURES (si mandat validé)
                            // ═══════════════════════════════════════════════════════════
                            if (mandat.Etat == Mandat.EtatMandat.Validé)
                            {
                                col.Item().PaddingTop(20).Row(sigRow =>
                                {
                                    // Signature S/G
                                    sigRow.RelativeItem().Column(sigCol =>
                                    {
                                        sigCol.Item().Text(text =>
                                        {
                                            text.Span(nomCommune).FontSize(8);
                                            text.Span(" Le ....../....../......").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        });

                                        sigCol.Item().PaddingTop(15).Text("Vu le S/G :")
                                            .FontSize(9).SemiBold();

                                        sigCol.Item().PaddingTop(20).Text("M./Mme : ............................................")
                                            .FontSize(8).SemiBold();
                                    });

                                    sigRow.ConstantItem(40);

                                    // Signature Ordonateur
                                    sigRow.RelativeItem().AlignRight().Column(sigCol =>
                                    {
                                        sigCol.Item().AlignRight().Text(text =>
                                        {
                                            text.Span(nomCommune).FontSize(8);
                                            text.Span(" Le ....../....../......").FontSize(7).FontColor(Color.FromHex(GrisTexte));
                                        });

                                        sigCol.Item().PaddingTop(15).AlignRight().Text("L'Ordonateur :")
                                            .FontSize(9).SemiBold();

                                        sigCol.Item().PaddingTop(20).AlignRight().Text("M./Mme : ............................................")
                                            .FontSize(8).SemiBold();
                                    });
                                });
                            }
                            else
                            {
                                col.Item().PaddingTop(15);
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
                            .Text($"Mandat N° {mandat.NumeroMandat}")
                            .FontSize(8).SemiBold().FontColor(Color.FromHex(GrisFonce));

                        row.RelativeItem().AlignRight()
                            .Text($"Statut : {mandat.Status.ToString().Replace("_", " ")}")
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
        private static string GetEtatColor(Mandat.EtatMandat etat)
        {
            return etat switch
            {
                Mandat.EtatMandat.Validé => "#4CAF50",       // Vert
                Mandat.EtatMandat.Non_Validé => "#FF9800",   // Orange
                _ => "#9E9E9E"                               // Gris
            };
        }

        /// <summary>
        /// Retourne la couleur du badge selon le statut
        /// </summary>
        private static string GetStatutColor(Mandat.StatutMandat statut)
        {
            return statut switch
            {
                Mandat.StatutMandat.Payé => "#4CAF50",       // Vert
                Mandat.StatutMandat.Partiel => "#2196F3",    // Bleu
                Mandat.StatutMandat.Non_Payé => "#F44336",   // Rouge
                _ => "#9E9E9E"                                // Gris
            };
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
        /// Version synchrone (évite le deadlock WPF)
        /// </summary>
        public static byte[] Exporter(Mandat mandat, Commune commune, Mouvement? mouvement = null, List<Mouvement>? tousLesMouvements = null)
        {
            return Task.Run(() => ExporterAsync(mandat, commune, mouvement, tousLesMouvements)).GetAwaiter().GetResult();
        }
    }
}