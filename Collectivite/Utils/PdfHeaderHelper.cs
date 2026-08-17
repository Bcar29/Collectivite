using iTextSharp.text;
using iTextSharp.text.pdf;
using Collectivite.Models;
using System;

namespace Collectivite.Utils
{
    /// <summary>
    /// Classe utilitaire pour générer l'en-tête officiel guinéen dans les PDF
    /// </summary>
    public static class PdfHeaderHelper
    {
        // ═══════════════════════════════════════
        // COULEURS OFFICIELLES
        // ═══════════════════════════════════════
        public static readonly BaseColor VertEmeraude = new BaseColor(5, 150, 105);      // #059669
        public static readonly BaseColor GrisArdoise = new BaseColor(30, 41, 59);        // #1E293B
        public static readonly BaseColor GrisFonce = new BaseColor(71, 85, 105);         // #475569
        public static readonly BaseColor GrisTexte = new BaseColor(100, 116, 139);       // #64748B
        public static readonly BaseColor VertFonce = new BaseColor(6, 95, 70);           // #065F46
        public static readonly BaseColor VertClair = new BaseColor(209, 250, 229);       // #D1FAE5
        public static readonly BaseColor GrisBordure = new BaseColor(224, 224, 224);     // #E0E0E0

        // ═══════════════════════════════════════
        // POLICES
        // ═══════════════════════════════════════
        private static Font GetFont(float size, int style = Font.NORMAL, BaseColor color = null)
        {
            return FontFactory.GetFont(FontFactory.HELVETICA, size, style, color ?? GrisArdoise);
        }

        /// <summary>
        /// Ajoute l'en-tête officiel guinéen complet au document PDF
        /// </summary>
        /// <param name="document">Le document PDF</param>
        /// <param name="commune">La commune</param>
        /// <param name="titre">Le titre du document (ex: "BUDGET PRIMITIF", "COMPTE ADMINISTRATIF")</param>
        /// <param name="sousTitre">Le sous-titre optionnel (ex: "Recettes de Fonctionnement")</param>
        /// <param name="exercice">Le libellé de l'exercice</param>
        public static void AjouterEnTeteOfficiel(Document document, Commune commune, string titre, string sousTitre = null, string exercice = null)
        {
            string typeCommune = commune?.TypCommune ?? "..........";
            string nomCommune = commune?.NomCommune ?? "............................";
            string region = commune?.RegionCommune ?? "............................";
            string prefecture = commune?.PrefectureCommune ?? "............................";

            // ═══════════════════════════════════════════════════════════
            // TABLEAU EN-TÊTE (3 colonnes)
            // ═══════════════════════════════════════════════════════════
            PdfPTable headerTable = new PdfPTable(3) { WidthPercentage = 100 };
            headerTable.SetWidths(new float[] { 40f, 20f, 40f });

            // ─────────────────────────────────────────────────────────────
            // COLONNE GAUCHE : Ministère + Infos géographiques
            // ─────────────────────────────────────────────────────────────
            PdfPCell leftCell = new PdfPCell { Border = Rectangle.NO_BORDER, PaddingBottom = 10 };

            Paragraph leftContent = new Paragraph();
            leftContent.Add(new Chunk("Ministère de l'Administration du Territoire\n", GetFont(10, Font.BOLD)));
            leftContent.Add(new Chunk("et de la Décentralisation\n", GetFont(10, Font.BOLD)));
            leftContent.Add(new Chunk("Direction Générale des Collectivités Locales\n\n", GetFont(9, Font.NORMAL, GrisFonce)));

            leftContent.Add(new Chunk("REGION ADMINISTRATIVE DE ", GetFont(9, Font.NORMAL, GrisTexte)));
            leftContent.Add(new Chunk(region.ToUpper() + "\n", GetFont(9, Font.BOLD)));

            leftContent.Add(new Chunk("PREFECTURE DE ", GetFont(9, Font.NORMAL, GrisTexte)));
            leftContent.Add(new Chunk(prefecture.ToUpper() + "\n", GetFont(9, Font.BOLD)));

            leftContent.Add(new Chunk("COMMUNE ", GetFont(9, Font.NORMAL, GrisTexte)));
            leftContent.Add(new Chunk(typeCommune.ToUpper(), GetFont(9, Font.BOLD)));
            leftContent.Add(new Chunk(" DE ", GetFont(9, Font.NORMAL, GrisTexte)));
            leftContent.Add(new Chunk(nomCommune.ToUpper(), GetFont(9, Font.BOLD)));

            leftCell.AddElement(leftContent);
            headerTable.AddCell(leftCell);

            
            // ─────────────────────────────────────────────────────────────
            // COLONNE DROITE : République + Devise
            // ─────────────────────────────────────────────────────────────
            PdfPCell rightCell = new PdfPCell { Border = Rectangle.NO_BORDER, PaddingBottom = 10 };

            Paragraph rightContent = new Paragraph();
            rightContent.Alignment = Element.ALIGN_RIGHT;
            rightContent.Add(new Chunk("REPUBLIQUE DE GUINEE\n", GetFont(11, Font.BOLD)));
            rightContent.Add(new Chunk("Travail - Justice - Solidarité", GetFont(10, Font.ITALIC, GrisFonce)));

            rightCell.AddElement(rightContent);
            headerTable.AddCell(rightCell);

            document.Add(headerTable);

            // ═══════════════════════════════════════════════════════════
            // TITRE DU DOCUMENT
            // ═══════════════════════════════════════════════════════════
            document.Add(new Paragraph(" ") { SpacingAfter = 10 });

            Paragraph titlePara = new Paragraph(titre.ToUpper(), GetFont(18, Font.BOLD));
            titlePara.Alignment = Element.ALIGN_CENTER;
            titlePara.SpacingAfter = 8;
            document.Add(titlePara);

            // ═══════════════════════════════════════════════════════════
            // BANDEAU VERT AVEC COMMUNE
            // ═══════════════════════════════════════════════════════════
            PdfPTable bandeauTable = new PdfPTable(1) { WidthPercentage = 60, HorizontalAlignment = Element.ALIGN_CENTER };

            PdfPCell bandeauCell = new PdfPCell();
            bandeauCell.BackgroundColor = VertClair;
            bandeauCell.BorderColor = VertEmeraude;
            bandeauCell.BorderWidthTop = 2;
            bandeauCell.BorderWidthBottom = 2;
            bandeauCell.BorderWidthLeft = 0;
            bandeauCell.BorderWidthRight = 0;
            bandeauCell.Padding = 8;
            bandeauCell.HorizontalAlignment = Element.ALIGN_CENTER;

            Paragraph bandeauText = new Paragraph($"DE LA COMMUNE {typeCommune.ToUpper()} DE {nomCommune.ToUpper()}", GetFont(11, Font.BOLD, VertFonce));
            bandeauText.Alignment = Element.ALIGN_CENTER;
            bandeauCell.AddElement(bandeauText);

            bandeauTable.AddCell(bandeauCell);
            document.Add(bandeauTable);

            // ═══════════════════════════════════════════════════════════
            // SOUS-TITRE (optionnel)
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrEmpty(sousTitre))
            {
                Paragraph subTitlePara = new Paragraph(sousTitre, GetFont(12, Font.BOLD, GrisFonce));
                subTitlePara.Alignment = Element.ALIGN_CENTER;
                subTitlePara.SpacingBefore = 10;
                document.Add(subTitlePara);
            }

            // ═══════════════════════════════════════════════════════════
            // EXERCICE
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrEmpty(exercice))
            {
                Paragraph exercicePara = new Paragraph($"{exercice}", GetFont(12, Font.BOLD));
                exercicePara.Alignment = Element.ALIGN_CENTER;
                exercicePara.SpacingBefore = 8;
                document.Add(exercicePara);
            }

            // ═══════════════════════════════════════════════════════════
            // DATE DE GÉNÉRATION
            // ═══════════════════════════════════════════════════════════
            Paragraph datePara = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", GetFont(9, Font.ITALIC, GrisTexte));
            datePara.Alignment = Element.ALIGN_RIGHT;
            datePara.SpacingBefore = 10;
            datePara.SpacingAfter = 15;
            document.Add(datePara);

            // Ligne de séparation
            PdfPTable separatorTable = new PdfPTable(1) { WidthPercentage = 100 };
            PdfPCell separatorCell = new PdfPCell { Border = Rectangle.NO_BORDER, BorderWidthBottom = 0.5f, BorderColorBottom = GrisBordure, FixedHeight = 5 };
            separatorTable.AddCell(separatorCell);
            document.Add(separatorTable);

            document.Add(new Paragraph(" ") { SpacingAfter = 10 });
        }

        /// <summary>
        /// Ajoute un pied de page standard au document
        /// </summary>
        /// <param name="document">Le document PDF</param>
        /// <param name="texteGauche">Texte à gauche (ex: titre du document)</param>
        /// <param name="texteCentre">Texte au centre (ex: numéro de page)</param>
        /// <param name="texteDroite">Texte à droite (ex: exercice)</param>
        public static void AjouterPiedDePage(Document document, string texteGauche = null, string texteCentre = null, string texteDroite = null)
        {
            document.Add(new Paragraph(" ") { SpacingBefore = 15 });

            // Ligne de séparation
            PdfPTable separatorTable = new PdfPTable(1) { WidthPercentage = 100 };
            PdfPCell separatorCell = new PdfPCell { Border = Rectangle.NO_BORDER, BorderWidthTop = 0.5f, BorderColorTop = GrisBordure, FixedHeight = 5 };
            separatorTable.AddCell(separatorCell);
            document.Add(separatorTable);

            // Tableau pied de page
            PdfPTable footerTable = new PdfPTable(3) { WidthPercentage = 100 };
            footerTable.SetWidths(new float[] { 33f, 34f, 33f });

            // Gauche
            PdfPCell leftCell = new PdfPCell(new Phrase(texteGauche ?? "", GetFont(8, Font.ITALIC, GrisTexte)));
            leftCell.Border = Rectangle.NO_BORDER;
            leftCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerTable.AddCell(leftCell);

            // Centre
            PdfPCell centerCell = new PdfPCell(new Phrase(texteCentre ?? "", GetFont(9, Font.BOLD, GrisFonce)));
            centerCell.Border = Rectangle.NO_BORDER;
            centerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerTable.AddCell(centerCell);

            // Droite
            PdfPCell rightCell = new PdfPCell(new Phrase(texteDroite ?? "", GetFont(8, Font.ITALIC, GrisTexte)));
            rightCell.Border = Rectangle.NO_BORDER;
            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            footerTable.AddCell(rightCell);

            document.Add(footerTable);
        }

        /// <summary>
        /// Ajoute une section de signatures (S/G et Ordonateur)
        /// </summary>
        /// <param name="document">Le document PDF</param>
        /// <param name="nomCommune">Nom de la commune</param>
        /// <param name="inclureSecretaireGeneral">Inclure la signature du S/G</param>
        public static void AjouterSignatures(Document document, string nomCommune, bool inclureSecretaireGeneral = true)
        {
            document.Add(new Paragraph(" ") { SpacingBefore = 30 });

            PdfPTable sigTable = new PdfPTable(inclureSecretaireGeneral ? 3 : 1) { WidthPercentage = 100 };

            if (inclureSecretaireGeneral)
            {
                sigTable.SetWidths(new float[] { 45f, 10f, 45f });

                // Signature S/G
                PdfPCell sgCell = new PdfPCell { Border = Rectangle.NO_BORDER };
                Paragraph sgContent = new Paragraph();
                sgContent.Add(new Chunk($"{nomCommune} Le ....../....../......\n\n", GetFont(9, Font.NORMAL, GrisTexte)));
                sgContent.Add(new Chunk("Vu le S/G :\n\n\n", GetFont(10, Font.BOLD)));
                sgContent.Add(new Chunk("M./Mme : ............................................", GetFont(9, Font.BOLD)));
                sgCell.AddElement(sgContent);
                sigTable.AddCell(sgCell);

                // Espace central
                PdfPCell spaceCell = new PdfPCell { Border = Rectangle.NO_BORDER };
                sigTable.AddCell(spaceCell);
            }

            // Signature Ordonateur
            PdfPCell ordCell = new PdfPCell { Border = Rectangle.NO_BORDER };
            Paragraph ordContent = new Paragraph();
            ordContent.Alignment = Element.ALIGN_RIGHT;
            ordContent.Add(new Chunk($"{nomCommune} Le ....../....../......\n\n", GetFont(9, Font.NORMAL, GrisTexte)));
            ordContent.Add(new Chunk("L'Ordonateur :\n\n\n", GetFont(10, Font.BOLD)));
            ordContent.Add(new Chunk("M./Mme : ............................................", GetFont(9, Font.BOLD)));
            ordCell.AddElement(ordContent);
            sigTable.AddCell(ordCell);

            document.Add(sigTable);
        }

        /// <summary>
        /// Ajoute une bannière de section colorée (ex: nom d'un onglet/d'une catégorie), pour bien
        /// séparer visuellement les sections d'un export PDF regroupant plusieurs tableaux.
        /// </summary>
        public static void AjouterBanniereSection(Document document, string sectionName, BaseColor? backgroundColor = null)
        {
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.WHITE);

            PdfPTable bannerTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 15 };
            PdfPCell bannerCell = new PdfPCell(new Phrase(sectionName.ToUpper(), sectionFont))
            {
                BackgroundColor = backgroundColor ?? new BaseColor(25, 118, 210),
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 10,
                BorderWidth = 0
            };
            bannerTable.AddCell(bannerCell);
            document.Add(bannerTable);
        }

        /// <summary>
        /// Crée une cellule de tableau avec couleur de fond
        /// </summary>
        public static PdfPCell CreateCell(string text, Font font, BaseColor bgColor, int alignment = Element.ALIGN_LEFT, float padding = 5)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.HorizontalAlignment = alignment;
            cell.Padding = padding;
            cell.BorderColor = GrisBordure;
            return cell;
        }
    }
}