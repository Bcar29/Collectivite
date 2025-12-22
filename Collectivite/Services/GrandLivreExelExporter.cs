using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class GrandLivreExcelExporter
    {
        // Configuration de la grille
        private const int COLONNES_PAR_LIGNE = 4;  // 4 comptes par ligne comme dans l'app
        private const int LARGEUR_CARTE = 4;       // Chaque carte occupe 4 colonnes Excel
        private const int ESPACEMENT = 1;          // 1 colonne d'espacement entre les cartes

        // ═══════════════════════════════════════════════════════════
        // COULEURS ADAPTÉES À LA PAGE XAML
        // ═══════════════════════════════════════════════════════════
        private static readonly string BLEU_TITRE = "#1976D2";       // Numéro de compte
        private static readonly string VERT_DEBIT = "#388E3C";       // Montants débit
        private static readonly string ROUGE_CREDIT = "#D32F2F";     // Montants crédit
        private static readonly string GRIS_TEXTE = "#666666";       // Textes secondaires
        private static readonly string GRIS_FOND = "#F5F5F5";        // Fond totaux
        private static readonly string GRIS_BORDURE = "#E0E0E0";     // Bordures

        /// <summary>
        /// Exporte le Grand Livre en fichier Excel avec en-tête officiel (version async)
        /// </summary>
        public static async Task<byte[]> ExporterAsync(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Grand Livre");

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

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE OFFICIEL GUINÉEN
            // ═══════════════════════════════════════════════════════════
            int ligneActuelle = 1;

            // Ligne 1 : Ministère (gauche) et République (droite)
            ws.Cell(ligneActuelle, 1).Value = "Ministère de l'Administration du Territoire";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 10;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ws.Cell(ligneActuelle, 14).Value = "REPUBLIQUE DE GUINEE";
            ws.Cell(ligneActuelle, 14).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 14).Style.Font.FontSize = 11;
            ws.Cell(ligneActuelle, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(ligneActuelle, 14, ligneActuelle, 20).Merge();

            ligneActuelle++;

            // Ligne 2 : et de la Décentralisation (gauche) et Devise (droite)
            ws.Cell(ligneActuelle, 1).Value = "et de la Décentralisation";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 10;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ws.Cell(ligneActuelle, 14).Value = "Travail - Justice - Solidarité";
            ws.Cell(ligneActuelle, 14).Style.Font.Italic = true;
            ws.Cell(ligneActuelle, 14).Style.Font.FontSize = 10;
            ws.Cell(ligneActuelle, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(ligneActuelle, 14, ligneActuelle, 20).Merge();

            ligneActuelle++;

            // Ligne 3 : Direction Générale
            ws.Cell(ligneActuelle, 1).Value = "Direction Générale des Collectivités Locales";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 9;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ligneActuelle += 2;

            // Ligne 5 : Région Administrative
            ws.Cell(ligneActuelle, 1).Value = $"REGION ADMINISTRATIVE DE {region.ToUpper()}";
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 9;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ligneActuelle++;

            // Ligne 6 : Préfecture
            ws.Cell(ligneActuelle, 1).Value = $"PREFECTURE DE {prefecture.ToUpper()}";
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 9;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ligneActuelle++;

            // Ligne 7 : Commune
            ws.Cell(ligneActuelle, 1).Value = $"COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 9;
            ws.Range(ligneActuelle, 1, ligneActuelle, 8).Merge();

            ligneActuelle += 2;

            // ═══════════════════════════════════════════════════════════
            // TITRE PRINCIPAL
            // ═══════════════════════════════════════════════════════════
            ws.Cell(ligneActuelle, 1).Value = "GRAND LIVRE";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 20;
            ws.Cell(ligneActuelle, 1).Style.Font.FontColor = XLColor.DarkGreen;
            ws.Cell(ligneActuelle, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Merge();

            ligneActuelle++;

            // Sous-titre avec commune
            ws.Cell(ligneActuelle, 1).Value = $"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 12;
            ws.Cell(ligneActuelle, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(ligneActuelle, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Merge();
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Style.Border.TopBorder = XLBorderStyleValues.Double;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Style.Border.TopBorderColor = XLColor.DarkGreen;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Style.Border.BottomBorder = XLBorderStyleValues.Double;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Style.Border.BottomBorderColor = XLColor.DarkGreen;

            ligneActuelle++;

            // Exercice
            ws.Cell(ligneActuelle, 1).Value = $"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}";
            ws.Cell(ligneActuelle, 1).Style.Font.Bold = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 14;
            ws.Cell(ligneActuelle, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Merge();

            ligneActuelle++;

            // Période (si filtre)
            if (filtre != null && filtre.Mois.HasValue)
            {
                string[] moisNoms = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                                     "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
                string periode = $"Mois de {moisNoms[filtre.Mois.Value]}";
                if (filtre.Annee.HasValue)
                    periode += $" {filtre.Annee.Value}";

                ws.Cell(ligneActuelle, 1).Value = periode;
                ws.Cell(ligneActuelle, 1).Style.Font.Italic = true;
                ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 10;
                ws.Cell(ligneActuelle, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(ligneActuelle, 1, ligneActuelle, 20).Merge();

                ligneActuelle++;
            }

            // Date d'export
            ws.Cell(ligneActuelle, 1).Value = $"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}";
            ws.Cell(ligneActuelle, 1).Style.Font.Italic = true;
            ws.Cell(ligneActuelle, 1).Style.Font.FontSize = 9;
            ws.Cell(ligneActuelle, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(ligneActuelle, 1, ligneActuelle, 20).Merge();

            ligneActuelle += 2;

            // ═══════════════════════════════════════════════════════════
            // CONTENU - CARTES DE COMPTES
            // ═══════════════════════════════════════════════════════════
            var comptesAvecMouvements = comptes.Where(c => c.Mouvements.Any()).ToList();

            int ligneDebut = ligneActuelle;
            int compteIndex = 0;

            while (compteIndex < comptesAvecMouvements.Count)
            {
                // Calculer la hauteur maximale de cette ligne de cartes
                int maxMouvements = 0;
                for (int i = 0; i < COLONNES_PAR_LIGNE && compteIndex + i < comptesAvecMouvements.Count; i++)
                {
                    var c = comptesAvecMouvements[compteIndex + i];
                    maxMouvements = Math.Max(maxMouvements, c.Mouvements.Count);
                }

                // Dessiner chaque carte de cette ligne
                for (int i = 0; i < COLONNES_PAR_LIGNE && compteIndex < comptesAvecMouvements.Count; i++)
                {
                    var compte = comptesAvecMouvements[compteIndex];
                    int colonneDebut = 1 + i * (LARGEUR_CARTE + ESPACEMENT);

                    DessinerCarte(ws, compte, ligneDebut, colonneDebut, maxMouvements);
                    compteIndex++;
                }

                // Passer à la ligne suivante
                ligneDebut += 4 + maxMouvements + 3;
            }

            // Ajuster les largeurs de colonnes
            for (int col = 1; col <= COLONNES_PAR_LIGNE * (LARGEUR_CARTE + ESPACEMENT); col++)
            {
                ws.Column(col).Width = 15;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Exporte le Grand Livre en fichier Excel (version synchrone - compatibilité)
        /// </summary>
        public static byte[] Exporter(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            // Appeler la version async de manière synchrone
            return ExporterAsync(comptes, filtre).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Dessine une carte de compte dans Excel (style identique à la page XAML)
        /// </summary>
        private static void DessinerCarte(IXLWorksheet ws, GrandLivreCompteDTO compte, int ligneDebut, int colonneDebut, int maxMouvements)
        {
            int col1 = colonneDebut;
            int col2 = colonneDebut + 1;
            int col3 = colonneDebut + 2;
            int col4 = colonneDebut + 3;

            // ═══════════════════════════════════════
            // EN-TÊTE DU COMPTE (Numéro) - Bleu #1976D2
            // ═══════════════════════════════════════
            var cellNumero = ws.Cell(ligneDebut, col1);
            ws.Range(ligneDebut, col1, ligneDebut, col4).Merge();
            cellNumero.Value = compte.NumeroCompte;
            cellNumero.Style.Font.Bold = true;
            cellNumero.Style.Font.FontSize = 16;
            cellNumero.Style.Font.FontColor = XLColor.FromHtml(BLEU_TITRE);  // #1976D2
            cellNumero.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellNumero.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellNumero.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // ═══════════════════════════════════════
            // INTITULÉ DU COMPTE - Gris #666666
            // ═══════════════════════════════════════
            var cellIntitule = ws.Cell(ligneDebut + 1, col1);
            ws.Range(ligneDebut + 1, col1, ligneDebut + 1, col4).Merge();
            cellIntitule.Value = compte.IntituleCompte;
            cellIntitule.Style.Font.FontSize = 10;
            cellIntitule.Style.Font.FontColor = XLColor.FromHtml(GRIS_TEXTE);  // #666666
            cellIntitule.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellIntitule.Style.Alignment.WrapText = true;
            cellIntitule.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellIntitule.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // ═══════════════════════════════════════
            // EN-TÊTES DÉBIT / CRÉDIT - Gris #666666
            // ═══════════════════════════════════════
            int ligneHeaders = ligneDebut + 2;

            // Colonne Débit
            ws.Range(ligneHeaders, col1, ligneHeaders, col2).Merge();
            var cellDebitHeader = ws.Cell(ligneHeaders, col1);
            cellDebitHeader.Value = "Débit";
            cellDebitHeader.Style.Font.Bold = true;
            cellDebitHeader.Style.Font.FontSize = 11;
            cellDebitHeader.Style.Font.FontColor = XLColor.FromHtml(GRIS_TEXTE);  // #666666
            cellDebitHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellDebitHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellDebitHeader.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // Colonne Crédit
            ws.Range(ligneHeaders, col3, ligneHeaders, col4).Merge();
            var cellCreditHeader = ws.Cell(ligneHeaders, col3);
            cellCreditHeader.Value = "Crédit";
            cellCreditHeader.Style.Font.Bold = true;
            cellCreditHeader.Style.Font.FontSize = 11;
            cellCreditHeader.Style.Font.FontColor = XLColor.FromHtml(GRIS_TEXTE);  // #666666
            cellCreditHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellCreditHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellCreditHeader.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // ═══════════════════════════════════════
            // MOUVEMENTS
            // ═══════════════════════════════════════
            int ligneMouvement = ligneDebut + 3;

            for (int i = 0; i < maxMouvements; i++)
            {
                // Cellules Débit
                ws.Range(ligneMouvement + i, col1, ligneMouvement + i, col2).Merge();
                var cellDebit = ws.Cell(ligneMouvement + i, col1);
                cellDebit.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                cellDebit.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);
                cellDebit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Cellules Crédit
                ws.Range(ligneMouvement + i, col3, ligneMouvement + i, col4).Merge();
                var cellCredit = ws.Cell(ligneMouvement + i, col3);
                cellCredit.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                cellCredit.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);
                cellCredit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Remplir les valeurs si mouvement existe
                if (i < compte.Mouvements.Count)
                {
                    var mvt = compte.Mouvements[i];

                    if (mvt.MontantDebit > 0)
                    {
                        cellDebit.Value = mvt.MontantDebit;
                        cellDebit.Style.NumberFormat.Format = "#,##0";
                        cellDebit.Style.Font.FontColor = XLColor.FromHtml(VERT_DEBIT);  // #388E3C
                    }

                    if (mvt.MontantCredit > 0)
                    {
                        cellCredit.Value = mvt.MontantCredit;
                        cellCredit.Style.NumberFormat.Format = "#,##0";
                        cellCredit.Style.Font.FontColor = XLColor.FromHtml(ROUGE_CREDIT);  // #D32F2F
                    }
                }
            }

            // ═══════════════════════════════════════
            // TOTAUX - Fond gris #F5F5F5
            // ═══════════════════════════════════════
            int ligneTotaux = ligneMouvement + maxMouvements;

            // Total Débit
            ws.Range(ligneTotaux, col1, ligneTotaux, col2).Merge();
            var cellTotalDebit = ws.Cell(ligneTotaux, col1);
            cellTotalDebit.Value = compte.TotalDebit;
            cellTotalDebit.Style.NumberFormat.Format = "#,##0";
            cellTotalDebit.Style.Font.Bold = true;
            cellTotalDebit.Style.Font.FontColor = XLColor.FromHtml(VERT_DEBIT);  // #388E3C
            cellTotalDebit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellTotalDebit.Style.Fill.BackgroundColor = XLColor.FromHtml(GRIS_FOND);  // #F5F5F5
            cellTotalDebit.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellTotalDebit.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // Total Crédit
            ws.Range(ligneTotaux, col3, ligneTotaux, col4).Merge();
            var cellTotalCredit = ws.Cell(ligneTotaux, col3);
            cellTotalCredit.Value = compte.TotalCredit;
            cellTotalCredit.Style.NumberFormat.Format = "#,##0";
            cellTotalCredit.Style.Font.Bold = true;
            cellTotalCredit.Style.Font.FontColor = XLColor.FromHtml(ROUGE_CREDIT);  // #D32F2F
            cellTotalCredit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellTotalCredit.Style.Fill.BackgroundColor = XLColor.FromHtml(GRIS_FOND);  // #F5F5F5
            cellTotalCredit.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellTotalCredit.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // ═══════════════════════════════════════
            // SOLDE - Gris #666666 sur fond #F5F5F5
            // ═══════════════════════════════════════
            int ligneSolde = ligneTotaux + 1;
            ws.Range(ligneSolde, col1, ligneSolde, col4).Merge();
            var cellSolde = ws.Cell(ligneSolde, col1);
            cellSolde.Value = compte.SoldeFormate;
            cellSolde.Style.Font.FontSize = 10;
            cellSolde.Style.Font.FontColor = XLColor.FromHtml(GRIS_TEXTE);  // #666666
            cellSolde.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellSolde.Style.Fill.BackgroundColor = XLColor.FromHtml(GRIS_FOND);  // #F5F5F5
            cellSolde.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cellSolde.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);

            // ═══════════════════════════════════════
            // BORDURE GLOBALE DE LA CARTE
            // ═══════════════════════════════════════
            var carteRange = ws.Range(ligneDebut, col1, ligneSolde, col4);
            carteRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            carteRange.Style.Border.OutsideBorderColor = XLColor.FromHtml(GRIS_BORDURE);  // #E0E0E0
        }
    }
}