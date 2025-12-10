using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Collectivite.Services
{
    public class GrandLivreExcelExporter
    {
        // Configuration de la grille
        private const int COLONNES_PAR_LIGNE = 4;  // 4 comptes par ligne comme dans l'app
        private const int LARGEUR_CARTE = 4;       // Chaque carte occupe 4 colonnes Excel
        private const int ESPACEMENT = 1;          // 1 colonne d'espacement entre les cartes

        /// <summary>
        /// Exporte le Grand Livre en fichier Excel (format cartes comme l'application)
        /// </summary>
        public static byte[] Exporter(List<GrandLivreCompteDTO> comptes, GrandLivreFiltreDTO? filtre = null)
        {
            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Grand Livre");

            // Titre principal
            string titre = ConstruireTitre(filtre);
            ws.Cell("A1").Value = titre;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 20;
            ws.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;
            ws.Range(1, 1, 1, 20).Merge();

            // Date d'export
            ws.Cell("A2").Value = $"Exporté le : {DateTime.Now:dd/MM/yyyy à HH:mm}";
            ws.Cell("A2").Style.Font.Italic = true;
            ws.Cell("A2").Style.Font.FontSize = 10;
            ws.Range(2, 1, 2, 20).Merge();

            // Filtrer les comptes avec mouvements
            var comptesAvecMouvements = comptes.Where(c => c.Mouvements.Any()).ToList();

            // Dessiner les cartes
            int ligneDebut = 4;
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

                // Passer à la ligne suivante (hauteur carte = en-tête + mouvements + totaux + espacement)
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
        /// Dessine une carte de compte dans Excel
        /// </summary>
        private static void DessinerCarte(IXLWorksheet ws, GrandLivreCompteDTO compte, int ligneDebut, int colonneDebut, int maxMouvements)
        {
            int col1 = colonneDebut;
            int col2 = colonneDebut + 1;
            int col3 = colonneDebut + 2;
            int col4 = colonneDebut + 3;

            // ═══════════════════════════════════════
            // EN-TÊTE DU COMPTE (Numéro)
            // ═══════════════════════════════════════
            var cellNumero = ws.Cell(ligneDebut, col1);
            ws.Range(ligneDebut, col1, ligneDebut, col4).Merge();
            cellNumero.Value = compte.NumeroCompte;
            cellNumero.Style.Font.Bold = true;
            cellNumero.Style.Font.FontSize = 14;
            cellNumero.Style.Font.FontColor = XLColor.DarkBlue;
            cellNumero.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellNumero.Style.Fill.BackgroundColor = XLColor.LightGray;
            cellNumero.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ═══════════════════════════════════════
            // INTITULÉ DU COMPTE
            // ═══════════════════════════════════════
            var cellIntitule = ws.Cell(ligneDebut + 1, col1);
            ws.Range(ligneDebut + 1, col1, ligneDebut + 1, col4).Merge();
            cellIntitule.Value = compte.IntituleCompte;
            cellIntitule.Style.Font.FontSize = 10;
            cellIntitule.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellIntitule.Style.Alignment.WrapText = true;
            cellIntitule.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ═══════════════════════════════════════
            // EN-TÊTES DÉBIT / CRÉDIT
            // ═══════════════════════════════════════
            int ligneHeaders = ligneDebut + 2;

            // Colonne Débit (occupe 2 colonnes)
            ws.Range(ligneHeaders, col1, ligneHeaders, col2).Merge();
            var cellDebitHeader = ws.Cell(ligneHeaders, col1);
            cellDebitHeader.Value = "Débit";
            cellDebitHeader.Style.Font.Bold = true;
            cellDebitHeader.Style.Font.FontSize = 11;
            cellDebitHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellDebitHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9"); // Vert clair
            cellDebitHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Colonne Crédit (occupe 2 colonnes)
            ws.Range(ligneHeaders, col3, ligneHeaders, col4).Merge();
            var cellCreditHeader = ws.Cell(ligneHeaders, col3);
            cellCreditHeader.Value = "Crédit";
            cellCreditHeader.Style.Font.Bold = true;
            cellCreditHeader.Style.Font.FontSize = 11;
            cellCreditHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellCreditHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE"); // Rouge clair
            cellCreditHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

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
                cellDebit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Cellules Crédit
                ws.Range(ligneMouvement + i, col3, ligneMouvement + i, col4).Merge();
                var cellCredit = ws.Cell(ligneMouvement + i, col3);
                cellCredit.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                cellCredit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Remplir les valeurs si mouvement existe
                if (i < compte.Mouvements.Count)
                {
                    var mvt = compte.Mouvements[i];

                    if (mvt.MontantDebit > 0)
                    {
                        cellDebit.Value = mvt.MontantDebit;
                        cellDebit.Style.NumberFormat.Format = "#,##0";
                        cellDebit.Style.Font.FontColor = XLColor.DarkGreen;
                    }

                    if (mvt.MontantCredit > 0)
                    {
                        cellCredit.Value = mvt.MontantCredit;
                        cellCredit.Style.NumberFormat.Format = "#,##0";
                        cellCredit.Style.Font.FontColor = XLColor.DarkRed;
                    }
                }
            }

            // ═══════════════════════════════════════
            // TOTAUX
            // ═══════════════════════════════════════
            int ligneTotaux = ligneMouvement + maxMouvements;

            // Total Débit
            ws.Range(ligneTotaux, col1, ligneTotaux, col2).Merge();
            var cellTotalDebit = ws.Cell(ligneTotaux, col1);
            cellTotalDebit.Value = compte.TotalDebit;
            cellTotalDebit.Style.NumberFormat.Format = "#,##0";
            cellTotalDebit.Style.Font.Bold = true;
            cellTotalDebit.Style.Font.FontColor = XLColor.DarkGreen;
            cellTotalDebit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellTotalDebit.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            cellTotalDebit.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Total Crédit
            ws.Range(ligneTotaux, col3, ligneTotaux, col4).Merge();
            var cellTotalCredit = ws.Cell(ligneTotaux, col3);
            cellTotalCredit.Value = compte.TotalCredit;
            cellTotalCredit.Style.NumberFormat.Format = "#,##0";
            cellTotalCredit.Style.Font.Bold = true;
            cellTotalCredit.Style.Font.FontColor = XLColor.DarkRed;
            cellTotalCredit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellTotalCredit.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            cellTotalCredit.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ═══════════════════════════════════════
            // SOLDE
            // ═══════════════════════════════════════
            int ligneSolde = ligneTotaux + 1;
            ws.Range(ligneSolde, col1, ligneSolde, col4).Merge();
            var cellSolde = ws.Cell(ligneSolde, col1);
            cellSolde.Value = compte.SoldeFormate;
            cellSolde.Style.Font.FontSize = 10;
            cellSolde.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellSolde.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            cellSolde.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ═══════════════════════════════════════
            // BORDURE GLOBALE DE LA CARTE
            // ═══════════════════════════════════════
            var carteRange = ws.Range(ligneDebut, col1, ligneSolde, col4);
            carteRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            carteRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;
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