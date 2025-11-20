using Collectivite.Models;
using Collectivite.Services;
using iTextSharp.text;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net;

public static class SeedNomenclature
{
    public static void Seed(AppDbContext db)
    {
        if (db.Nommenclatures.Any()) return; // éviter double insertion

        #region recettes de fonctionnement
        // -----------------------------
        // CHAPITRE 71 : RECETTES FISCALES
        // -----------------------------
        var c71 = new Nommenclature
        {
            Chapitre = "71",
            Intitule = "RECETTES FISCALES",
            Nature = NatureType.Recette,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(c71);
        db.SaveChanges();

        var a710 = AddArticle(db, c71.Id, "710", "Impôts directs et taxes assimilées");

        var p7100 = AddParagraphe(db, a710.Id, "7100", "Contribution des patentes");

        AddSousParagraphe(db, p7100.Id, "71001", "Contribution des patentes, personnes morales");
        AddSousParagraphe(db, p7100.Id, "71002", "Contribution des patentes, personnes physiques");
        AddSousParagraphe(db, p7100.Id, "71003", "Quote part sur les marchés de TP et de génie civil");

        AddParagraphe(db, a710.Id, "7101", "Contribution des Licences");

        var p7102 = AddParagraphe(db, a710.Id, "7102", "Contribution Foncière Unique (CFU)");
        AddSousParagraphe(db, p7102.Id, "71020", "CFU, personne morale");
        AddSousParagraphe(db, p7102.Id, "71021", "CFU, personne physique");

        AddParagraphe(db, a710.Id, "7103", "Taxe Professionnelle Unique");
        AddParagraphe(db, a710.Id, "7104", "Taxe sur les armes à feu");

        // -----------------------------
        // CHAPITRE 72 : RECETTES NON FISCALES
        // -----------------------------
        var c72 = AddChapitre(db, "72", "RECETTES NON FISCALES");

        var a720 = AddArticle(db, c72.Id, "720", "Revenus des biens de l'Etat et autres produits");

        AddParagraphe(db, a720.Id, "7200", "Revenus des propriétés et domaines de l'Etat");
        AddParagraphe(db, a720.Id, "7201", "Amendes et pénalités");
        AddParagraphe(db, a720.Id, "7202", "Revenus des services publics");
        AddParagraphe(db, a720.Id, "7203", "Divers produits non fiscaux");

        // -----------------------------
        // CHAPITRE 73 : PRODUITS FINANCIERS
        // -----------------------------
        var c73 = AddChapitre(db, "73", "PRODUITS FINANCIERS");
        var a730 = AddArticle(db, c73.Id, "730", "Intérêts et revenus financiers");

        AddParagraphe(db, a730.Id, "7300", "Intérêts sur dépôts et comptes bancaires");
        AddParagraphe(db, a730.Id, "7301", "Revenus des titres et placements financiers");

        // -----------------------------
        // CHAPITRE 74 : TRANSFERTS COURANTS
        // -----------------------------
        var c74 = AddChapitre(db, "74", "TRANSFERTS COURANTS");
        var a740 = AddArticle(db, c74.Id, "740", "Subventions et transferts reçus");

        AddParagraphe(db, a740.Id, "7400", "Subventions budgétaires");
        AddParagraphe(db, a740.Id, "7401", "Transferts reçus de l’étranger");

        // -----------------------------
        // CHAPITRE 75 : EMPRUNTS ET DETTES
        // -----------------------------
        var c75 = AddChapitre(db, "75", "EMPRUNTS ET DETTES");
        var a750 = AddArticle(db, c75.Id, "750", "Emprunts contractés");

        AddParagraphe(db, a750.Id, "7500", "Emprunts nationaux");
        AddParagraphe(db, a750.Id, "7501", "Emprunts étrangers");

        // -----------------------------
        // CHAPITRE 76 : EXCÉDENTS D'EXERCICES PRÉCÉDENTS
        // -----------------------------
        var c76 = AddChapitre(db, "76", "EXCÉDENTS D'EXERCICES PRÉCÉDENTS");
        var a760 = AddArticle(db, c76.Id, "760", "Excédents des exercices antérieurs");

        AddParagraphe(db, a760.Id, "7600", "Excédents reportés non affectés");
        AddParagraphe(db, a760.Id, "7601", "Excédents reportés affectés");

        // -----------------------------
        // CHAPITRE 77 : OPÉRATIONS D'ORDRE
        // -----------------------------
        var c77 = AddChapitre(db, "77", "OPÉRATIONS D'ORDRE");
        var a770 = AddArticle(db, c77.Id, "770", "Opérations de régularisation");

        AddParagraphe(db, a770.Id, "7700", "Opérations internes d'ajustement");
        AddParagraphe(db, a770.Id, "7701", "Opérations d’ordre entre services");

        #endregion

        #region depense de fonctionnement
        //chapitre 60
        var c60 = AddChapitredf(db, "60", "ACHATS");

        // ARTICLE 601
        var a601 = AddArticledf(db, c60.Id, "601", "Achats stockés - Matières premières");

        AddParagraphedf(db, a601.Id, "6011", "Matières premières A");
        AddParagraphedf(db, a601.Id, "6012", "Matières premières B");

        // ARTICLE 602
        var a602 = AddArticledf(db, c60.Id, "602", "Achats stockés - Autres approvisionnements");

        AddParagraphedf(db, a602.Id, "6021", "Combustibles");
        AddParagraphedf(db, a602.Id, "6022", "Eau");
        AddParagraphedf(db, a602.Id, "6023", "Électricité");
        AddParagraphedf(db, a602.Id, "6024", "Fournitures d'entretien");

        // ARTICLE 604
        var a604 = AddArticledf(db, c60.Id, "604", "Achats non stockés de matières et fournitures");

        AddParagraphedf(db, a604.Id, "6041", "Papeterie");
        AddParagraphedf(db, a604.Id, "6042", "Fournitures de bureau");
        AddParagraphedf(db, a604.Id, "6043", "Petit matériel");

        // ARTICLE 606
        var a606 = AddArticledf(db, c60.Id, "606", "Achats non stockés - autres");

        AddParagraphedf(db, a606.Id, "6061", "Produits d'entretien");
        AddParagraphedf(db, a606.Id, "6062", "Fournitures diverses");

        var c61 = AddChapitredf(db, "61", "SERVICES EXTERNES");

        // ARTICLE 611
        var a611 = AddArticledf(db, c61.Id, "611", "Sous-traitance générale");

        AddParagraphedf(db, a611.Id, "6111", "Travaux sous-traités");
        AddParagraphedf(db, a611.Id, "6112", "Prestations de services");

        // ARTICLE 612
        var a612 = AddArticledf(db, c61.Id, "612", "Locations");

        AddParagraphedf(db, a612.Id, "6121", "Location matériel");
        AddParagraphedf(db, a612.Id, "6122", "Location immobilière");

        // ARTICLE 613
        var a613 = AddArticledf(db, c61.Id, "613", "Entretien et réparations");

        AddParagraphedf(db, a613.Id, "6131", "Réparations bâtiments");
        AddParagraphedf(db, a613.Id, "6132", "Réparations matériel");

        // ARTICLE 615
        var a615 = AddArticledf(db, c61.Id, "615", "Maintenance");

        AddParagraphedf(db, a615.Id, "6151", "Maintenance informatique");
        AddParagraphedf(db, a615.Id, "6152", "Maintenance équipements administratifs");

        var c62 = AddChapitredf(db, "62", "AUTRES SERVICES EXTÉRIEURS");

        // ARTICLE 621
        var a621 = AddArticledf(db, c62.Id, "621", "Personnel extérieur à l'administration");

        AddParagraphedf(db, a621.Id, "6211", "Main d'œuvre temporaire");
        AddParagraphedf(db, a621.Id, "6212", "Vacations");

        // ARTICLE 622
        var a622 = AddArticledf(db, c62.Id, "622", "Rémunérations d'intermédiaires et honoraires");

        AddParagraphedf(db, a622.Id, "6221", "Honoraires juridiques");
        AddParagraphedf(db, a622.Id, "6222", "Honoraires comptables");

        // ARTICLE 623
        var a623 = AddArticledf(db, c62.Id, "623", "Publicité, publications et relations publiques");

        AddParagraphedf(db, a623.Id, "6231", "Annonces");
        AddParagraphedf(db, a623.Id, "6232", "Publicité institutions");

        // ARTICLE 625
        var a625 = AddArticledf(db, c62.Id, "625", "Déplacements, missions et réceptions");

        AddParagraphedf(db, a625.Id, "6251", "Frais de mission");
        AddParagraphedf(db, a625.Id, "6252", "Frais de déplacement");
        AddParagraphedf(db, a625.Id, "6253", "Réceptions");


        //-----------------------------
        //CHAPITRE 63 : IMPÔTS, TAXES ET VERSEMENTS ASSIMILÉS
        //-----------------------------
        var c63 = AddChapitredf(db, "63", "IMPÔTS, TAXES ET VERSEMENTS ASSIMILÉS");

        // ARTICLE 631
        var a631 = AddArticledf(db, c63.Id, "631", "Impôts à la charge de l’administration");

        AddParagraphedf(db, a631.Id, "6311", "Impôts sur rémunérations");
        AddParagraphedf(db, a631.Id, "6312", "Autres impôts");

        // ARTICLE 633
        var a633 = AddArticledf(db, c63.Id, "633", "Taxes foncières et autres taxes");

        AddParagraphedf(db, a633.Id, "6331", "Taxes foncières");
        AddParagraphedf(db, a633.Id, "6332", "Droits divers");

        // ARTICLE 635
        var a635 = AddArticledf(db, c63.Id, "635", "Autres impôts, taxes et versements");

        AddParagraphedf(db, a635.Id, "6351", "Taxes administratives");
        AddParagraphedf(db, a635.Id, "6352", "Contributions diverses");

        // -----------------------------
        // CHAPITRE 64 : INTERVENTIONS A CARACTERE ECONOMIQUE ET SOCIAL
        // -----------------------------
        var c64 = AddChapitredf(db, "64", "INTERVENTIONS A CARACTERE ECONOMIQUE ET SOCIAL");

        // ARTICLE 640 : Dotations
        var a640 = AddArticle(db, c64.Id, "640", "Dotations");

        AddParagraphedf(db, a640.Id, "6400", "Dotations pour entretien et réparations des établissements préscolaires et scolaires");
        AddParagraphedf(db, a640.Id, "6401", "Dotations pour entretien et réparations des établissements sanitaires");
        AddParagraphedf(db, a640.Id, "6402", "Dotations pour entretien et réparations des centres de culture, d'arts et de sports");
        AddParagraphedf(db, a640.Id, "6403", "Dotations pour entretien et réparations des voiries");
        AddParagraphedf(db, a640.Id, "6404", "Dotations pour entretiens et réparations des marchés");
        AddParagraphedf(db, a640.Id, "6405", "Dotations pour entretiens et réparations des gares routières");
        AddParagraphedf(db, a640.Id, "6406", "Dotations pour entretiens et réparations des abattoirs");
        AddParagraphedf(db, a640.Id, "6407", "Dotations pour entretiens et réparations des cimetières, parcs, jardins et points d'eau");
        AddParagraphedf(db, a640.Id, "6408", "Dotations de fonctionnement aux services de protection civile et de lutte contre l'incendie");

        // ARTICLE 641 : Subventions et allocations
        var a641 = AddArticledf(db, c64.Id, "641", "Subventions et allocations accordées");

        AddParagraphedf(db, a641.Id, "6410", "Subventions à la caisse de péréquation inter-collectivités locales");
        AddParagraphedf(db, a641.Id, "6411", "Subventions aux associations de jeunesse et de sport");
        AddParagraphedf(db, a641.Id, "6412", "Subventions aux associations culturelles et artistiques");
        AddParagraphedf(db, a641.Id, "6413", "Subventions pour manifestations diverses");
        AddParagraphedf(db, a641.Id, "6414", "Subventions aux organismes socio-économiques");
        AddParagraphedf(db, a641.Id, "6415", "Subventions aux sinistrés et indigents");
        AddParagraphedf(db, a641.Id, "6418", "Autres subventions et allocations");

        // ARTICLE 642 : Actions socio-économiques
        var a642 = AddArticledf(db, c64.Id, "642", "Actions socio-économiques");

        AddParagraphedf(db, a642.Id, "6421", "Actions économiques d'intérêt de la CL");
        AddParagraphedf(db, a642.Id, "6422", "Actions de jumelage et de coopération");
        AddParagraphedf(db, a642.Id, "6428", "Autres actions socio-économiques");


        // -----------------------------
        // CHAPITRE 65 : FRAIS FINANCIERS ET CHARGES ASSIMILEES
        // -----------------------------
        var c65 = AddChapitredf(db, "65", "FRAIS FINANCIERS ET CHARGES ASSIMILEES");

        // ARTICLE 650 : Frais financiers
        var a650 = AddArticledf(db, c65.Id, "650", "Frais financiers");

        AddParagraphedf(db, a650.Id, "6501", "Frais et commissions sur emprunts auprès des établissements de crédit");
        AddParagraphedf(db, a650.Id, "6502", "Frais et commissions sur autres emprunts");

        // ARTICLE 651 : Intérêts sur emprunts
        AddArticledf(db, c65.Id, "651", "Intérêts sur emprunts");

        // ARTICLE 652 : Intérêts sur compte courant
        AddArticledf(db, c65.Id, "652", "Intérêts sur compte courant");

        // ARTICLE 653 : Intérêts sur dettes diverses
        AddArticledf(db, c65.Id, "653", "Intérêts sur dettes diverses");

        // ARTICLE 654 : Pertes sur cessions de titres
        AddArticledf(db, c65.Id, "654", "Pertes sur cessions de titres");

        // ARTICLE 655 : Pertes de charges financières
        AddArticledf(db, c65.Id, "655", "Pertes de charges financières");

        // ARTICLE 656 : Charges de régies
        AddArticledf(db, c65.Id, "656", "Charges de régies");

        // ARTICLE 658 : Autres frais financiers
        AddArticledf(db, c65.Id, "658", "Autres frais financiers et charges assimilées");


        // -----------------------------
        // CHAPITRE 66 : CHARGES EXCEPTIONNELLES, ANTERIEURES ET DIVERSES
        // -----------------------------
        var c66 = AddChapitredf(db, "66", "CHARGES EXCEPTIONNELLES, ANTERIEURES ET DIVERSES");

        // ARTICLE 660 : Charges exceptionnelles
        var a660 = AddArticledf(db, c66.Id, "660", "Charges exceptionnelles");

        AddParagraphedf(db, a660.Id, "6601", "Annulation et réduction des ordres de recettes");
        AddParagraphedf(db, a660.Id, "6602", "Amendes et pénalités payées par la CL");

        // Sous-paragraphes de 6602
        AddSousParagraphedf(db, a660.Id, "66021", "Amendes payées par la CL");
        AddSousParagraphedf(db, a660.Id, "66022", "Pénalités payées par la CL");

        AddParagraphedf(db, a660.Id, "6604", "Admission en non valeur");
        AddParagraphedf(db, a660.Id, "6607", "Valeur comptable nette des immobilisations cédées");
        AddParagraphedf(db, a660.Id, "6608", "Autres charges exceptionnelles");

        // ARTICLE 661 : Charges antérieures
        var a661 = AddArticledf(db, c66.Id, "661", "Charges antérieures");

        AddParagraphedf(db, a661.Id, "6610", "Déficit de fonctionnement reporté");
        AddParagraphedf(db, a661.Id, "6611", "Arriérés de cotisations");
        AddParagraphedf(db, a661.Id, "6612", "Charges des exercices antérieurs");
        AddParagraphedf(db, a661.Id, "6618", "Autres charges extérieures");

        // ARTICLE 662 : Prélèvement pour dépenses d'investissements
        AddArticledf(db, c66.Id, "662", "Prélèvement pour dépenses d'investissements");

        // ARTICLE 663 : Charges diverses
        var a663 = AddArticledf(db, c66.Id, "663", "Charges diverses");

        AddParagraphedf(db, a663.Id, "6630", "Pénalités de retard sur marché payées par la CL");
        AddParagraphedf(db, a663.Id, "6631", "Frais de perception et de recouvrement des impôts et taxes");
        AddParagraphedf(db, a663.Id, "6632", "Indemnités d'expropriation versées");
        AddParagraphedf(db, a663.Id, "6633", "Frais d'inhumation des corps abandonnés");
        AddParagraphedf(db, a663.Id, "6635", "Quotes-parts versées aux quartiers");
        AddParagraphedf(db, a663.Id, "6636", "Quotes-parts versées aux Districts");
        AddParagraphedf(db, a663.Id, "6638", "Autres charges diverses");

        #endregion

        #region recettes d'investissement
        //CHAPITRE 10 — DOTATIONS
        var c10 = AddChapitreri(db, "10", "DOTATIONS");

        var a100 = AddArticleri(db, c10.Id, "100", "Dotations initiales");
        var a101 = AddArticleri(db, c10.Id, "101", "Dotations complémentaires d'équipements");


        //CHAPITRE 11 — PRELEVEMENTS
        var c11 = AddChapitreri(db, "11", "PRELEVEMENTS");

        var a110 = AddArticleri(db, c11.Id, "110", "Prélèvements sur recettes de fonctionnement");


        //CHAPITRE 12 — FONDS DE RÉSERVES
        var c12 = AddChapitreri(db, "12", "FONDS DE RESERVES");

        var a120 = AddArticleri(db, c12.Id, "120", "Fonds de réserves d'investissement");
        var a121 = AddArticleri(db, c12.Id, "121", "Fonds de réserves d'amortissement des équipements et du mobilier");


        //CHAPITRE 13 — RÉSULTAT
        var c13 = AddChapitreri(db, "13", "RESULTAT");

        var a139 = AddArticleri(db, c13.Id, "139", "Résultat net");

        AddParagrapheri(db, a139.Id, "1390", "Excédent d'investissements reporté");

        //CHAPITRE 14 — SUBVENTIONS D’ÉQUIPEMENTS
        var c14 = AddChapitreri(db, "14", "SUBVENTIONS D'EQUIPEMENTS");

        var a140 = AddArticleri(db, c14.Id, "140", "Subventions d'équipement reçues de l'Etat");
        var a148 = AddArticleri(db, c14.Id, "148", "Autres subventions d'équipements");


        //CHAPITRE 15 — DONS, LEGS, CONTRIBUTIONS VOLONTAIRES, TRANSFERTS
        var c15 = AddChapitreri(db, "15","DONS, LEGS, CONTRIBUTIONS VOLONTAIRES ET TRANSFERTS DE PROPRIETE");

        //Article 150 – Produits des dons reçus
        var a150 = AddArticleri(db, c15.Id, "150", "Produits des dons reçus");

        AddParagrapheri(db, a150.Id, "1500", "Produits des dons en nature avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1501", "Produits des dons en nature sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1502", "Produits des dons en espèces avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1503", "Produits des dons en espèces sans affectation particulière reçus de l'Etat");

        AddParagrapheri(db, a150.Id, "1504", "Produits des dons en nature avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1505", "Produits des dons en nature sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1506", "Produits des dons en espèces avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1507", "Produits des dons en espèces sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1508", "Autres produits des dons");

        //Article 151 – Produits des legs reçus
        var a151 = AddArticleri(db, c15.Id, "151", "Produits des legs reçus");

        AddParagrapheri(db, a151.Id, "1510", "Produits des legs en nature avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1511", "Produits des legs en nature sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1512", "Produits des legs en espèces avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1513", "Produits des legs en espèces sans affectation particulière reçus de l'Etat");

        AddParagrapheri(db, a151.Id, "1514", "Produits des legs en nature avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1515", "Produits des legs en nature sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1516", "Produits des legs en espèces avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1517", "Produits des legs en espèces sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1518", "Autres produits des legs reçus");

        //Article 152 – Contributions volontaires
        var a152 = AddArticleri(db, c15.Id, "152", "Contributions volontaires");

        AddParagrapheri(db, a152.Id, "1520", "Contributions volontaires en nature");
        AddParagrapheri(db, a152.Id, "1521", "Contributions volontaires en espèces");
        AddParagrapheri(db, a152.Id, "1528", "Autres contributions");

        //Article 153 – Transferts de propriété
        var a153 = AddArticleri(db, c15.Id, "153", "Transferts de propriété");

        AddParagrapheri(db, a153.Id, "1530", "Transferts d'immobilisations incorporelles");
        AddParagrapheri(db, a153.Id, "1531", "Transfert d'immobilisations corporelles");


        // -----------------------------
        // CHAPITRE 16 : FONDS DE CONCOURS, D'AIDES ET DE PEREQUATION
        // -----------------------------
        var c16 = AddChapitreri(db, "16", "FONDS DE CONCOURS, D'AIDES ET DE PEREQUATION");

        // --- Article 160
        var a160 = AddArticleri(db, c16.Id, "160", "Fonds de concours");

        AddParagrapheri(db, a160.Id, "1600", "Fonds de concours reçus de l'Etat");
        AddParagrapheri(db, a160.Id, "1601", "Fonds de concours reçus d'autres CL");
        AddParagrapheri(db, a160.Id, "1602", "Fonds de concours reçus d'organismes nationaux");
        AddParagrapheri(db, a160.Id, "1603", "Fonds de concours reçus d'organismes étrangers");
        AddParagrapheri(db, a160.Id, "1608", "Autres fonds de concours reçus");

        // --- Article 161
        var a161 = AddArticleri(db, c16.Id, "161", "Fonds d'aide");

        AddParagrapheri(db, a161.Id, "1610", "Fonds d'aide reçus de l'Etat");
        AddParagrapheri(db, a161.Id, "1611", "Fonds d'aide reçus d'autres CL");
        AddParagrapheri(db, a161.Id, "1612", "Fonds d'aide reçus d'organismes nationaux");
        AddParagrapheri(db, a161.Id, "1613", "Fonds d'aide reçus d'organismes étrangers");
        AddParagrapheri(db, a161.Id, "1618", "Fonds d'aide reçus d'autres organismes");

        // --- Article 162
        var a162 = AddArticleri(db, c16.Id, "162", "Fonds de péréquation reçus");

        AddParagrapheri(db, a162.Id, "1620", "Fonds de concours reçus du FNDT/ANAFIC");
        AddParagrapheri(db, a162.Id, "1628", "Autres fonds de concours, d'aide et de péréquation");


        // -----------------------------
        // CHAPITRE 17 : EMPRUNTS ET DETTES ASSIMILEES
        // -----------------------------
        var c17 = AddChapitreri(db, "17", "EMPRUNTS ET DETTES ASSIMILEES");

        // --- Article 170
        var a170 = AddArticleri(db, c17.Id, "170", "Emprunts reçus");

        var p1700 = AddParagrapheri(db, a170.Id, "1700", "Emprunts auprès des établissements de crédits");
        AddSousParagrapheri(db, p1700.Id, "17001", "En monnaie nationale");
        AddSousParagrapheri(db, p1700.Id, "17002", "En devises");

        AddParagrapheri(db, a170.Id, "1708", "Autres emprunts");

        // --- Article 171
        var a171 = AddArticleri(db, c17.Id, "171", "Dettes assimilées");

        AddParagrapheri(db, a171.Id, "1710", "Dépôts et cautionnements reçus");
        AddParagrapheri(db, a171.Id, "1711", "Intérêts courus des emprunts");
        AddParagrapheri(db, a171.Id, "1718", "Autres dettes assimilées");


        // -----------------------------
        // CHAPITRE 18 : VALEURS DE PORTEFEUILLE ET DE L'ALIÉNATION DU PATRIMOINE
        // -----------------------------
        var c18 = AddChapitreri(db, "18", "VALEURS DE PORTEFEUILLE ET DE L'ALIENATION DU PATRIMOINE");

        // --- Article 180
        var a180 = AddArticleri(db, c18.Id, "180", "Vente de valeurs en portefeuille");

        AddParagrapheri(db, a180.Id, "1801", "Vente de terrains");
        AddParagrapheri(db, a180.Id, "1802", "Vente de réserves foncières");
        AddParagrapheri(db, a180.Id, "1803", "Vente de bâtiments");
        AddParagrapheri(db, a180.Id, "1804", "Vente d'équipements, matériels, mobiliers, outillages et actifs");
        AddParagrapheri(db, a180.Id, "1805", "Vente aux enchères publiques d'éléments du patrimoine de la CL");

        // --- Article 181
        var a181 = AddArticleri(db, c18.Id, "181", "Revenus du secteur minier");

        AddParagrapheri(db, a181.Id, "1810", "Taxe superficiaire");
        AddParagrapheri(db, a181.Id, "1811", "Quote-part sur les taxes minières affectées au développement local");
        AddParagrapheri(db, a181.Id, "1812", "Contributions volontaires des sociétés minières au développement communautaire");
        AddParagrapheri(db, a181.Id, "1818", "Autres recettes minières");

        // --- Article 182
        var a182 = AddArticleri(db, c18.Id, "182", "Revenus d'exploitation d'autres valeurs en portefeuille");

        AddParagrapheri(db, a182.Id, "1820", "Secteur transport");
        AddParagrapheri(db, a182.Id, "1821", "Secteur actif biologique");
        AddParagrapheri(db, a182.Id, "1822", "Secteur agricole");
        AddParagrapheri(db, a182.Id, "1828", "Autres revenus des secteurs d'exploitation");


        // -----------------------------
        // CHAPITRE 19 : RECETTES DIVERSES
        // -----------------------------
        var c19 = AddChapitreri(db, "19", "RECETTES DIVERSES");

        // --- Article 190
        var a190 = AddArticleri(db, c19.Id, "190", "Recettes en atténuation des dépenses");

        // --- Article 191
        var a191 = AddArticleri(db, c19.Id, "191", "Revenus antérieurs divers");

        #endregion
        
        
        // -------------------------------------
        // DÉPENSES D'INVESTISSEMENT
        // -------------------------------------

        // nature = 1 / section = 1
        // (à gérer dans ton code avant l’injection)

        // -----------------------------
        // CHAPITRE 10 : DOTATIONS
        // -----------------------------
        //var c10 = AddChapitre(db, "10", "DOTATIONS");

        //var a100 = AddArticle(db, c10.Id, "100", "Dotations d'équipement accordées");


        //// -----------------------------
        //// CHAPITRE 14 : SUBVENTIONS D'ÉQUIPEMENTS ACCORDÉES
        //// -----------------------------
        //var c14 = AddChapitre(db, "14", "SUBVENTIONS D'EQUIPEMENTS ACCORDEES");

        //var a146 = AddArticle(db, c14.Id, "146", "Subventions d'équipements accordées");
        //var a148 = AddArticle(db, c14.Id, "148", "Autres subventions d'équipements");


        //// -----------------------------
        //// CHAPITRE 16 : FONDS DE CONCOURS / AIDES
        //// -----------------------------
        //var c16 = AddChapitre(db, "16", "FONDS DE CONCOURS, D'AIDES ET DE PEREQUATION");

        //var a165 = AddArticle(db, c16.Id, "165", "Fonds de concours et d'aides attribués par la CL");
        //var a168 = AddArticle(db, c16.Id, "168", "Autres fonds de concours et d'aides attribuées par la CL");


        //// -----------------------------
        //// CHAPITRE 17 : EMPRUNTS ET DETTES ASSIMILÉES
        //// -----------------------------
        //var c17 = AddChapitre(db, "17", "EMPRUNTS ET DETTES ASSIMILES");

        //var a176 = AddArticle(db, c17.Id, "176", "Remboursement du capital des emprunts");

        //AddParagraphe(db, a176.Id, "1760", "Remboursement du capital des emprunts auprès des établissements de crédit");
        //AddParagraphe(db, a176.Id, "1761", "Remboursement des dettes assimilées");

        //AddSousParagraphe(db, a176.Id, "17610", "Dépôts et cautionnement versés");

        //AddParagraphe(db, a176.Id, "1768", "Remboursement d'autres emprunts et dettes assimilées");


        //// -----------------------------
        //// CHAPITRE 18 : VALEURS EN PORTEFEUILLE / ALIÉNATION DU PATRIMOINE
        //// -----------------------------
        //var c18 = AddChapitre(db, "18", "VALEURS EN PORTEFEUILLE DE L'ALIENATION DU PATRIMOINE");

        //var a186 = AddArticle(db, c18.Id, "186", "Acquisition de valeurs en portefeuille et de patrimoine");

        //AddParagraphe(db, a186.Id, "1861", "Titres");
        //AddParagraphe(db, a186.Id, "1862", "Actions");
        //AddParagraphe(db, a186.Id, "1863", "Participations");
        //AddParagraphe(db, a186.Id, "1868", "Autres valeurs de portefeuille acquises");


        // -----------------------------
        // CHAPITRE 21 : IMMOBILISATIONS INCORPORELLES
        // -----------------------------
        var c21 = AddChapitredi(db, "21", "ACQUISITIONS D'IMMOBILISATIONS INCORPORELLES");

        var a210 = AddArticledi(db, c21.Id, "210", "Frais d'études et de recherches");
        AddParagraphedi(db, a210.Id, "2100", "Frais d'études");
        AddParagraphedi(db, a210.Id, "2101", "Frais de recherches");

        var a213 = AddArticledi(db, c21.Id, "213", "Logiciels et sites internet");
        AddParagraphedi(db, a213.Id, "2131", "Logiciels");
        AddParagraphedi(db, a213.Id, "2132", "Sites internet");

        var a214 = AddArticledi(db, c21.Id, "214", "Documentation technique");
        var a215 = AddArticledi(db, c21.Id, "215", "Assistance technique");
        var a216 = AddArticledi(db, c21.Id, "216", "Supervision des travaux");
        var a218 = AddArticledi(db, c21.Id, "218", "Autres immobilisations incorporelles");

        var a219 = AddArticledi(db, c21.Id, "219", "Immobilisations incorporelles en cours");
        AddParagraphedi(db, a219.Id, "2190", "Frais d'étude en cours");
        AddParagraphedi(db, a219.Id, "2191", "Frais de recherches en cours");
        AddParagraphedi(db, a219.Id, "2193", "Logiciels, sites internet et web en cours");

        AddSousParagraphedi(db, a219.Id, "21931", "Logiciels en cours");
        AddSousParagraphedi(db, a219.Id, "21932", "Site internet en cours");
        AddSousParagraphedi(db, a219.Id, "21933", "Web en cours");

        AddParagraphedi(db, a219.Id, "2194", "Frais de documentation technique en cours");
        AddParagraphedi(db, a219.Id, "2198", "Autres immobilisations incorporelles en cours");





    }

    #region methodes recettes de fonctionnement

    // ---------------------------------------------------
    //  HELPERS POUR LES RECETTES DE FONCTIONNEMENT POUR CRÉER RAPIDEMENT CHAPITRES/ARTICLES/...
    // ---------------------------------------------------
    private static Nommenclature AddChapitre(AppDbContext db, string code, string intitule)
    {
        var c = new Nommenclature
        {
            Chapitre = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(c);
        db.SaveChanges();
        return c;
    }

    private static Nommenclature AddArticle(AppDbContext db, int parentId, string code, string intitule)
    {
        var a = new Nommenclature
        {
            ParentId = parentId,
            Article = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(a);
        db.SaveChanges();
        return a;
    }

    private static Nommenclature AddParagraphe(AppDbContext db, int parentId, string code, string intitule)
    {
        var p = new Nommenclature
        {
            ParentId = parentId,
            Paragraphe = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(p);
        db.SaveChanges();
        return p;
    }

    private static Nommenclature AddSousParagraphe(AppDbContext db, int parentId, string code, string intitule)
    {
        var s = new Nommenclature
        {
            ParentId = parentId,
            SousParagraphe = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(s);
        db.SaveChanges();
        return s;
    }
    #endregion

    #region methodes recettes d'investissement
    // ---------------------------------------------------
    //  HELPERS POUR LES RECETTES D'INVESTISSEMENT POUR CRÉER RAPIDEMENT CHAPITRES/ARTICLES/...
    // ---------------------------------------------------
    private static Nommenclature AddChapitreri(AppDbContext db, string code, string intitule)
    {
        var c = new Nommenclature
        {
            Chapitre = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(c);
        db.SaveChanges();
        return c;
    }

    private static Nommenclature AddArticleri(AppDbContext db, int parentId, string code, string intitule)
    {
        var a = new Nommenclature
        {
            ParentId = parentId,
            Article = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(a);
        db.SaveChanges();
        return a;
    }

    private static Nommenclature AddParagrapheri(AppDbContext db, int parentId, string code, string intitule)
    {
        var p = new Nommenclature
        {
            ParentId = parentId,
            Paragraphe = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(p);
        db.SaveChanges();
        return p;
    }

    private static Nommenclature AddSousParagrapheri(AppDbContext db, int parentId, string code, string intitule)
    {
        var s = new Nommenclature
        {
            ParentId = parentId,
            SousParagraphe = code,
            Intitule = intitule,
            Nature = NatureType.Recette,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(s);
        db.SaveChanges();
        return s;
    }
    #endregion

    #region methodes depenses de fonctionnement
    // ---------------------------------------------------
    //  HELPERS POUR LES DEPENSE DE FONCTIONNEMENT POUR CRÉER RAPIDEMENT CHAPITRES/ARTICLES/...
    // ---------------------------------------------------
    private static Nommenclature AddChapitredf(AppDbContext db, string code, string intitule)
    {
        var c = new Nommenclature
        {
            Chapitre = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(c);
        db.SaveChanges();
        return c;
    }

    private static Nommenclature AddArticledf(AppDbContext db, int parentId, string code, string intitule)
    {
        var a = new Nommenclature
        {
            ParentId = parentId,
            Article = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(a);
        db.SaveChanges();
        return a;
    }

    private static Nommenclature AddParagraphedf(AppDbContext db, int parentId, string code, string intitule)
    {
        var p = new Nommenclature
        {
            ParentId = parentId,
            Paragraphe = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(p);
        db.SaveChanges();
        return p;
    }

    private static Nommenclature AddSousParagraphedf(AppDbContext db, int parentId, string code, string intitule)
    {
        var s = new Nommenclature
        {
            ParentId = parentId,
            SousParagraphe = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(s);
        db.SaveChanges();
        return s;
    }
    #endregion

    #region methodes depenses d'investissement
    // ---------------------------------------------------
    //  HELPERS POUR LES DEPENSE D'INVESTISSEMENT POUR CRÉER RAPIDEMENT CHAPITRES/ARTICLES/...
    // ---------------------------------------------------
    private static Nommenclature AddChapitredi(AppDbContext db, string code, string intitule)
    {
        var c = new Nommenclature
        {
            Chapitre = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(c);
        db.SaveChanges();
        return c;
    }

    private static Nommenclature AddArticledi(AppDbContext db, int parentId, string code, string intitule)
    {
        var a = new Nommenclature
        {
            ParentId = parentId,
            Article = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(a);
        db.SaveChanges();
        return a;
    }

    private static Nommenclature AddParagraphedi(AppDbContext db, int parentId, string code, string intitule)
    {
        var p = new Nommenclature
        {
            ParentId = parentId,
            Paragraphe = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(p);
        db.SaveChanges();
        return p;
    }

    private static Nommenclature AddSousParagraphedi(AppDbContext db, int parentId, string code, string intitule)
    {
        var s = new Nommenclature
        {
            ParentId = parentId,
            SousParagraphe = code,
            Intitule = intitule,
            Nature = NatureType.Depense,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(s);
        db.SaveChanges();
        return s;
    }
    
    #endregion


}
