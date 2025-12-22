using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BalanceExcelExporter
    {
        // ═══════════════════════════════════════
        // PALETTE DE COULEURS (identique à BalancePage.xaml)
        // ═══════════════════════════════════════

        // Couleurs principales des en-têtes
        private static readonly XLColor VertEmeraude = XLColor.FromHtml("#059669");
        private static readonly XLColor RougeCoral = XLColor.FromHtml("#DC2626");
        private static readonly XLColor BleuIndigo = XLColor.FromHtml("#4F46E5");

        // Couleurs claires des sous-en-têtes
        private static readonly XLColor VertClair = XLColor.FromHtml("#D1FAE5");
        private static readonly XLColor VertClairAccent = XLColor.FromHtml("#A7F3D0");
        private static readonly XLColor RougeClair = XLColor.FromHtml("#FEE2E2");
        private static readonly XLColor RougeClairAccent = XLColor.FromHtml("#FECACA");
        private static readonly XLColor BleuClair = XLColor.FromHtml("#E0E7FF");

        // Couleurs de fond des cellules de données
        private static readonly XLColor VertTresClair = XLColor.FromHtml("#F0FDF4");
        private static readonly XLColor RougeTresClair = XLColor.FromHtml("#FEF2F2");

        // Couleurs de texte
        private static readonly XLColor TexteVertFonce = XLColor.FromHtml("#065F46");
        private static readonly XLColor TexteRougeFonce = XLColor.FromHtml("#991B1B");
        private static readonly XLColor TexteBleuFonce = XLColor.FromHtml("#3730A3");
        private static readonly XLColor TexteVert = XLColor.FromHtml("#059669");
        private static readonly XLColor TexteRouge = XLColor.FromHtml("#DC2626");

        // Couleurs de la ligne de totaux
        private static readonly XLColor GrisArdoise = XLColor.FromHtml("#1E293B");
        private static readonly XLColor VertLumineux = XLColor.FromHtml("#34D399");
        private static readonly XLColor RougeLumineux = XLColor.FromHtml("#F87171");

        // Couleurs neutres
        private static readonly XLColor GrisClair = XLColor.FromHtml("#F1F5F9");
        private static readonly XLColor GrisTexte = XLColor.FromHtml("#64748B");
        private static readonly XLColor GrisFonce = XLColor.FromHtml("#475569");
        private static readonly XLColor GrisBordure = XLColor.FromHtml("#E2E8F0");

        /// <summary>
        /// Exporte la Balance en fichier Excel avec en-tête officiel (version async)
        /// </summary>
        public static async Task<byte[]> ExporterAsync(List<BalanceLigneDTO> lignes, BalanceTotauxDTO totaux, BalanceFiltreDTO filtre)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Balance");

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

            string[] moisNoms = { "", "JANVIER", "FÉVRIER", "MARS", "AVRIL", "MAI", "JUIN",
                                  "JUILLET", "AOÛT", "SEPTEMBRE", "OCTOBRE", "NOVEMBRE", "DÉCEMBRE" };
            string moisTexte = moisNoms[filtre.Mois];

            int row = 1;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE OFFICIEL GUINÉEN
            // ═══════════════════════════════════════════════════════════

            // Ligne 1 : Ministère (gauche) et République (droite)
            ws.Cell(row, 1).Value = "Ministère de l'Administration du Territoire";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 10;
            ws.Range(row, 1, row, 6).Merge();

            ws.Cell(row, 9).Value = "REPUBLIQUE DE GUINEE";
            ws.Cell(row, 9).Style.Font.Bold = true;
            ws.Cell(row, 9).Style.Font.FontSize = 11;
            ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(row, 9, row, 12).Merge();
            row++;

            // Ligne 2 : et de la Décentralisation (gauche) et Devise (droite)
            ws.Cell(row, 1).Value = "et de la Décentralisation";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 10;
            ws.Range(row, 1, row, 6).Merge();

            ws.Cell(row, 9).Value = "Travail - Justice - Solidarité";
            ws.Cell(row, 9).Style.Font.Italic = true;
            ws.Cell(row, 9).Style.Font.FontSize = 10;
            ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(row, 9, row, 12).Merge();
            row++;

            // Ligne 3 : Direction Générale
            ws.Cell(row, 1).Value = "Direction Générale des Collectivités Locales";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 9;
            ws.Range(row, 1, row, 6).Merge();
            row += 2;

            // Ligne 5 : Région Administrative
            ws.Cell(row, 1).Value = $"REGION ADMINISTRATIVE DE {region.ToUpper()}";
            ws.Cell(row, 1).Style.Font.FontSize = 9;
            ws.Range(row, 1, row, 6).Merge();
            row++;

            // Ligne 6 : Préfecture
            ws.Cell(row, 1).Value = $"PREFECTURE DE {prefecture.ToUpper()}";
            ws.Cell(row, 1).Style.Font.FontSize = 9;
            ws.Range(row, 1, row, 6).Merge();
            row++;

            // Ligne 7 : Commune
            ws.Cell(row, 1).Value = $"COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            ws.Cell(row, 1).Style.Font.FontSize = 9;
            ws.Range(row, 1, row, 6).Merge();
            row += 2;

            // ═══════════════════════════════════════════════════════════
            // TITRE PRINCIPAL
            // ═══════════════════════════════════════════════════════════
            ws.Cell(row, 1).Value = "BALANCE MENSUELLE DES COMPTES";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 18;
            ws.Cell(row, 1).Style.Font.FontColor = GrisArdoise;
            ws.Range(row, 1, row, 12).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            // Sous-titre avec commune (bandeau vert)
            ws.Cell(row, 1).Value = $"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Range(row, 1, row, 12).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = VertClair;
            ws.Range(row, 1, row, 12).Style.Border.TopBorder = XLBorderStyleValues.Double;
            ws.Range(row, 1, row, 12).Style.Border.TopBorderColor = VertEmeraude;
            ws.Range(row, 1, row, 12).Style.Border.BottomBorder = XLBorderStyleValues.Double;
            ws.Range(row, 1, row, 12).Style.Border.BottomBorderColor = VertEmeraude;
            row++;

            // Exercice
            ws.Cell(row, 1).Value = $"Exercice {exercice?.GetAnnee() ?? DateTime.Now.Year}";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Range(row, 1, row, 12).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            // Mois
            ws.Cell(row, 1).Value = $"Mois de {moisTexte}";
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Cell(row, 1).Style.Font.FontColor = GrisFonce;
            ws.Range(row, 1, row, 12).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row += 2;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE NIVEAU 1 (groupes de colonnes)
            // ═══════════════════════════════════════════════════════════

            // Colonnes vides pour N° et Intitulé
            ws.Range(row, 1, row, 2).Merge();
            ws.Cell(row, 1).Style.Fill.BackgroundColor = GrisClair;
            ws.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, 1).Style.Border.OutsideBorderColor = GrisBordure;

            // Débit - Vert émeraude
            ws.Range(row, 3, row, 6).Merge();
            ws.Cell(row, 3).Value = "DÉBIT";
            ws.Cell(row, 3).Style.Fill.BackgroundColor = VertEmeraude;
            ws.Cell(row, 3).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Font.FontSize = 12;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, 3).Style.Border.OutsideBorderColor = XLColor.FromHtml("#047857");

            // Crédit - Rouge coral
            ws.Range(row, 7, row, 10).Merge();
            ws.Cell(row, 7).Value = "CRÉDIT";
            ws.Cell(row, 7).Style.Fill.BackgroundColor = RougeCoral;
            ws.Cell(row, 7).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 7).Style.Font.Bold = true;
            ws.Cell(row, 7).Style.Font.FontSize = 12;
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, 7).Style.Border.OutsideBorderColor = XLColor.FromHtml("#B91C1C");

            // Solde - Bleu indigo
            ws.Range(row, 11, row, 12).Merge();
            ws.Cell(row, 11).Value = "SOLDE";
            ws.Cell(row, 11).Style.Fill.BackgroundColor = BleuIndigo;
            ws.Cell(row, 11).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 11).Style.Font.Bold = true;
            ws.Cell(row, 11).Style.Font.FontSize = 12;
            ws.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, 11).Style.Border.OutsideBorderColor = XLColor.FromHtml("#4338CA");

            row++;

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE NIVEAU 2 (sous-colonnes)
            // ═══════════════════════════════════════════════════════════
            var headers = new[] {
                "N° Comptes", "Intitulés",
                "Balance\nEntrée", "Mouv\nAntérieur", "Mouv\nMois", "Total",
                "Balance\nEntrée", "Mouv\nAntérieur", "Mouv\nMois", "Total",
                "Débiteur", "Créditeur"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Colonnes N° et Intitulés
                if (i < 2)
                {
                    cell.Style.Fill.BackgroundColor = GrisClair;
                    cell.Style.Font.FontColor = GrisFonce;
                    cell.Style.Border.OutsideBorderColor = GrisBordure;
                }
                // Colonnes Débit (vert clair)
                else if (i >= 2 && i <= 4)
                {
                    cell.Style.Fill.BackgroundColor = VertClair;
                    cell.Style.Font.FontColor = TexteVertFonce;
                    cell.Style.Border.OutsideBorderColor = VertClairAccent;
                }
                // Colonne Total Débit (vert accent)
                else if (i == 5)
                {
                    cell.Style.Fill.BackgroundColor = VertClairAccent;
                    cell.Style.Font.FontColor = TexteVertFonce;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#6EE7B7");
                }
                // Colonnes Crédit (rouge clair)
                else if (i >= 6 && i <= 8)
                {
                    cell.Style.Fill.BackgroundColor = RougeClair;
                    cell.Style.Font.FontColor = TexteRougeFonce;
                    cell.Style.Border.OutsideBorderColor = RougeClairAccent;
                }
                // Colonne Total Crédit (rouge accent)
                else if (i == 9)
                {
                    cell.Style.Fill.BackgroundColor = RougeClairAccent;
                    cell.Style.Font.FontColor = TexteRougeFonce;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#FCA5A5");
                }
                // Colonnes Solde (bleu clair)
                else
                {
                    cell.Style.Fill.BackgroundColor = BleuClair;
                    cell.Style.Font.FontColor = TexteBleuFonce;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#C7D2FE");
                }
            }
            ws.Row(row).Height = 35;
            row++;

            // ═══════════════════════════════════════════════════════════
            // DONNÉES
            // ═══════════════════════════════════════════════════════════
            bool alternate = false;
            foreach (var ligne in lignes)
            {
                var bgColor = alternate ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                // N° Compte
                ws.Cell(row, 1).Value = ligne.NumeroCompte;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontColor = GrisArdoise;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = bgColor;

                // Intitulé
                ws.Cell(row, 2).Value = ligne.IntituleCompte;
                ws.Cell(row, 2).Style.Font.FontColor = GrisFonce;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = bgColor;

                // Débit - Balance Entrée, Mouv Antérieur, Mouv Mois
                for (int col = 3; col <= 5; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = bgColor;
                    ws.Cell(row, col).Style.Font.FontColor = GrisTexte;
                }
                if (ligne.DebitBalanceEntree > 0)
                    ws.Cell(row, 3).Value = ligne.DebitBalanceEntree;
                if (ligne.DebitMouvAnterieur > 0)
                    ws.Cell(row, 4).Value = ligne.DebitMouvAnterieur;
                if (ligne.DebitMouvMois > 0)
                    ws.Cell(row, 5).Value = ligne.DebitMouvMois;

                // Total Débit
                ws.Cell(row, 6).Style.Fill.BackgroundColor = VertTresClair;
                if (ligne.DebitTotal > 0)
                {
                    ws.Cell(row, 6).Value = ligne.DebitTotal;
                    ws.Cell(row, 6).Style.Font.Bold = true;
                    ws.Cell(row, 6).Style.Font.FontColor = TexteVert;
                }

                // Crédit - Balance Entrée, Mouv Antérieur, Mouv Mois
                for (int col = 7; col <= 9; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = bgColor;
                    ws.Cell(row, col).Style.Font.FontColor = GrisTexte;
                }
                if (ligne.CreditBalanceEntree > 0)
                    ws.Cell(row, 7).Value = ligne.CreditBalanceEntree;
                if (ligne.CreditMouvAnterieur > 0)
                    ws.Cell(row, 8).Value = ligne.CreditMouvAnterieur;
                if (ligne.CreditMouvMois > 0)
                    ws.Cell(row, 9).Value = ligne.CreditMouvMois;

                // Total Crédit
                ws.Cell(row, 10).Style.Fill.BackgroundColor = RougeTresClair;
                if (ligne.CreditTotal > 0)
                {
                    ws.Cell(row, 10).Value = ligne.CreditTotal;
                    ws.Cell(row, 10).Style.Font.Bold = true;
                    ws.Cell(row, 10).Style.Font.FontColor = TexteRouge;
                }

                // Solde Débiteur
                ws.Cell(row, 11).Style.Fill.BackgroundColor = VertTresClair;
                if (ligne.SoldeDebiteur > 0)
                {
                    ws.Cell(row, 11).Value = ligne.SoldeDebiteur;
                    ws.Cell(row, 11).Style.Font.FontColor = TexteVert;
                    ws.Cell(row, 11).Style.Font.Bold = true;
                }

                // Solde Créditeur
                ws.Cell(row, 12).Style.Fill.BackgroundColor = RougeTresClair;
                if (ligne.SoldeCrebiteur > 0)
                {
                    ws.Cell(row, 12).Value = ligne.SoldeCrebiteur;
                    ws.Cell(row, 12).Style.Font.FontColor = TexteRouge;
                    ws.Cell(row, 12).Style.Font.Bold = true;
                }

                // Format des nombres et bordures
                for (int col = 1; col <= 12; col++)
                {
                    if (col >= 3)
                        ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, col).Style.Alignment.Horizontal = col <= 2 ?
                        (col == 1 ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Left) :
                        XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    ws.Cell(row, col).Style.Border.OutsideBorderColor = GrisBordure;
                }

                alternate = !alternate;
                row++;
            }

            // ═══════════════════════════════════════════════════════════
            // LIGNE DE TOTAUX
            // ═══════════════════════════════════════════════════════════
            ws.Range(row, 1, row, 2).Merge();
            ws.Cell(row, 1).Value = "TOTAUX";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = GrisArdoise;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Totaux Débit
            ws.Cell(row, 3).Value = totaux.TotalDebitBalanceEntree;
            ws.Cell(row, 4).Value = totaux.TotalDebitMouvAnterieur;
            ws.Cell(row, 5).Value = totaux.TotalDebitMouvMois;
            ws.Cell(row, 6).Value = totaux.TotalDebit;
            ws.Cell(row, 6).Style.Font.FontColor = VertLumineux;

            // Totaux Crédit
            ws.Cell(row, 7).Value = totaux.TotalCreditBalanceEntree;
            ws.Cell(row, 8).Value = totaux.TotalCreditMouvAnterieur;
            ws.Cell(row, 9).Value = totaux.TotalCreditMouvMois;
            ws.Cell(row, 10).Value = totaux.TotalCredit;
            ws.Cell(row, 10).Style.Font.FontColor = RougeLumineux;

            // Totaux Solde
            ws.Cell(row, 11).Value = totaux.TotalSoldeDebiteur;
            ws.Cell(row, 11).Style.Font.FontColor = VertLumineux;
            ws.Cell(row, 12).Value = totaux.TotalSoldeCrebiteur;
            ws.Cell(row, 12).Style.Font.FontColor = RougeLumineux;

            // Format des totaux
            for (int col = 1; col <= 12; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Font.Bold = true;
                if (col >= 3)
                {
                    cell.Style.NumberFormat.Format = "#,##0";
                    if (col != 6 && col != 10 && col != 11 && col != 12)
                        cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Fill.BackgroundColor = GrisArdoise;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#334155");
            }
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // ═══════════════════════════════════════════════════════════
            // AJUSTEMENT DES COLONNES
            // ═══════════════════════════════════════════════════════════
            ws.Column(1).Width = 14;
            ws.Column(2).Width = 28;
            for (int col = 3; col <= 12; col++)
                ws.Column(col).Width = 15;

            // ═══════════════════════════════════════════════════════════
            // PIED DE PAGE
            // ═══════════════════════════════════════════════════════════
            row += 2;
            ws.Cell(row, 1).Value = $"Édité le : {DateTime.Now:dd/MM/yyyy à HH:mm}";
            ws.Cell(row, 1).Style.Font.Italic = true;
            ws.Cell(row, 1).Style.Font.FontSize = 9;
            ws.Cell(row, 1).Style.Font.FontColor = GrisTexte;

            ws.Cell(row, 10).Value = $"Nombre de comptes : {lignes.Count}";
            ws.Cell(row, 10).Style.Font.Italic = true;
            ws.Cell(row, 10).Style.Font.FontSize = 9;
            ws.Cell(row, 10).Style.Font.FontColor = GrisTexte;
            ws.Range(row, 10, row, 12).Merge();
            ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Version synchrone pour compatibilité (appelle la version async)
        /// </summary>
        public static byte[] Exporter(List<BalanceLigneDTO> lignes, BalanceTotauxDTO totaux, BalanceFiltreDTO filtre)
        {
            return ExporterAsync(lignes, totaux, filtre).GetAwaiter().GetResult();
        }
    }
}