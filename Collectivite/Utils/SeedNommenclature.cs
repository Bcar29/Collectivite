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
        AddSousParagraphe(db, p7100.Id, "71003", "Quote part sur les marchés de TP et de génie civil (patente proportionnelle)");

        AddParagraphe(db, a710.Id, "7101", "Contribution des Licences");

        var p7102 = AddParagraphe(db, a710.Id, "7102", "Contribution Foncière Unique (CFU)");
        AddSousParagraphe(db, p7102.Id, "71020", "Contribution Foncière Unique (CFU), personne morale");
        AddSousParagraphe(db, p7102.Id, "71021", "Contribution Foncière Unique (CFU), personne physique");

        AddParagraphe(db, a710.Id, "7103", "Taxe Professionnelle Unique");
        AddParagraphe(db, a710.Id, "7104", "Taxe sur les armes à feu");
        AddParagraphe(db, a710.Id, "7105", "Taxe sur les biens de mains mortes");
        AddParagraphe(db, a710.Id, "7106", "Taxe Unique sur les Véhicules (TUV)");
        AddParagraphe(db, a710.Id, "7107", "Retenue sur les achats de produits locaux");
        AddParagraphe(db, a710.Id, "7108", "Retenue sur les revenus des prestataires non-résidents");
        AddParagraphe(db, a710.Id, "7109", "Autres recettes fiscales");

        // -----------------------------
        // CHAPITRE 72 : RECETTES NON FISCALES
        // -----------------------------
        var c72 = AddChapitre(db, "72", "RECETTES NON FISCALES");

        var a720 = AddArticle(db, c72.Id, "720", "Taxes remuneratoires");

        var p7200 = AddParagraphe(db, a720.Id, "7200", "Taxes d'état civil, notariales ou de greffe");
        AddSousParagraphe(db, p7200.Id, "72000", "Acte de naissance");
        AddSousParagraphe(db, p7200.Id, "72001", "Actes de mariage");
        AddSousParagraphe(db, p7200.Id, "72002", "Actes de décès");
        AddSousParagraphe(db, p7200.Id, "72003", "Délivrance des copies d'état civil conformes à l'original");
        AddSousParagraphe(db, p7200.Id, "72004", "Livret de famille");
        AddSousParagraphe(db, p7200.Id, "72005", "Actes de transcription");
        AddSousParagraphe(db, p7200.Id, "72006", "Certificat de résidence");
        AddSousParagraphe(db, p7200.Id, "72007", "Certificat de vie collective");
        AddSousParagraphe(db, p7200.Id, "72008", "Certificat de célibat");
        AddSousParagraphe(db, p7200.Id, "72009", "Actes notariés ou de greffe (donations, cessions, mutations)");

        AddParagraphe(db, a720.Id, "7201", "Taxes d'abattage");
        AddParagraphe(db, a720.Id, "7202", "Taxe de publicité");
        AddParagraphe(db, a720.Id, "7203", "Taxe d'hygiène et de salubrité publique");
        AddParagraphe(db, a720.Id, "7204", "Taxe de conditionnement et de contrôle de qualité");

        var p7205 = AddParagraphe(db, a720.Id, "7205", "Taxe de transferts");
        AddSousParagraphe(db, p7205.Id, "72051", "Taxe de transfert de produits");
        AddSousParagraphe(db, p7205.Id, "72052", "Taxe de transfert du cheptel");

        var p7206 = AddParagraphe(db, a720.Id, "7206", "Taxe d'équipement");
        AddSousParagraphe(db, p7206.Id, "72061", "Taxe sur les nouvelles constructions");
        AddSousParagraphe(db, p7206.Id, "72062", "Taxe sur les agrandissements et agencements");

        AddParagraphe(db, a720.Id, "7207", "Taxe sur les spectacles et réjouissances populaires");
        AddParagraphe(db, a720.Id, "7208", "Taxes de pêche artisanale et traditionnelle");
        AddParagraphe(db, a720.Id, "7209", "Autres taxes de rémunérations et diverses");

        var a721 = AddArticle(db, c72.Id, "721", "Taxe sur les engins");
        AddParagraphe(db, a721.Id, "7210", "Taxe sur les embarcations à moteurs");
        AddParagraphe(db, a721.Id, "7211", "Taxe sur les charrettes, tricycles");
        AddParagraphe(db, a721.Id, "7218", "Taxe sur les autres engins");

        var a728 = AddArticle(db, c72.Id, "728", "Autres recettes non fiscales");

        // -----------------------------
        // CHAPITRE 73 : DROITS ET REDEVANCES
        // -----------------------------
        var c73 = AddChapitre(db, "73", "DROITS ET REDEVANCES");

        var a730 = AddArticle(db, c73.Id, "730", "Droits du domaine");
        AddParagraphe(db, a730.Id, "7300", "Droits de place de marché");
        AddParagraphe(db, a730.Id, "7301", "Droits de location de kiosques et standes");
        AddParagraphe(db, a730.Id, "7302", "Droits de stationnement du bétail");
        AddParagraphe(db, a730.Id, "7303", "Droits de stationnement de véhicules à moteur");
        AddParagraphe(db, a730.Id, "7304", "Droits et produits de fourrière");
        AddParagraphe(db, a730.Id, "7305", "Droits d'exploitation des sites touristiques");
        AddParagraphe(db, a730.Id, "7306", "Droits d'exploitation des eaux superficiaires et souteraines");
        AddParagraphe(db, a730.Id, "7308", "Autres droits du domaine");

        var a731 = AddArticle(db, c73.Id, "731", "Redevances du domaine");
        AddParagraphe(db, a731.Id, "7310", "Redevance d'exploitation des substance de carrière");
        AddParagraphe(db, a731.Id, "7311", "Redevance d'exploitation artisanale des mines");
        AddParagraphe(db, a731.Id, "7312", "Redavance forestère");
        AddParagraphe(db, a731.Id, "7313", "Redevance d'occupation privative du domaine public");
        AddParagraphe(db, a731.Id, "7314", "Redevance topographique");
        AddParagraphe(db, a731.Id, "7315", "Redevances environnementales");
        AddParagraphe(db, a731.Id, "7316", "Redevance d'inhumation");
        AddParagraphe(db, a731.Id, "7317", "Redevance d'exploitation des sites touristiques");

        var a732 = AddArticle(db, c73.Id, "732", "Autres droits et redvances");
        AddParagraphe(db, a732.Id, "7320", "Licence de pêche artisanale et traditionnelle");
        AddParagraphe(db, a732.Id, "7321", "Redevance sur pêche artisanales et traditionnelles");
        AddParagraphe(db, a732.Id, "7322", "Redevance sur permis de construire");
        AddParagraphe(db, a732.Id, "7323", "Droit d'enregistrement pour l'exercice des activités professionnelles");
        AddParagraphe(db, a732.Id, "7324", "Droits d'inscription aux concours de recrutement");

        // -----------------------------
        // CHAPITRE 74 : PRODUITS D'EXPLOITATION DU PATRIMOINE
        // -----------------------------
        var c74 = AddChapitre(db, "74", "PRODUITS D'EXPLOITATION DU PATRIMOINE");

        var a740 = AddArticle(db, c74.Id, "740", "Produits d'exploitation");

        var p7400 = AddParagraphe(db, a740.Id, "7400", "Cotisations des usagers des services");
        AddSousParagraphe(db, p7400.Id, "74001", "Revenu des latrines publiques");
        AddSousParagraphe(db, p7400.Id, "74002", "Revenu des bornes fontaines");
        AddSousParagraphe(db, p7400.Id, "74003", "Revenu des lavoirs");

        AddParagraphe(db, a740.Id, "7401", "Location des meubles, immeubles et des terrains");
        AddParagraphe(db, a740.Id, "7402", "Redevance des abatoirs");
        AddParagraphe(db, a740.Id, "7408", "Autres produits d'exploitations");

        // -----------------------------
        // CHAPITRE 75 : REVENUS DU PORTEFEUILLE
        // -----------------------------
        var c75 = AddChapitre(db, "75", "REVENUS DU PORTEFEUILLE");

        var a750 = AddArticle(db, c75.Id, "750", "Produits des services concédés");
        AddParagraphe(db, a750.Id, "7501", "Marchés");
        AddParagraphe(db, a750.Id, "7502", "Gare-routière");
        AddParagraphe(db, a750.Id, "7503", "Parking et aires de stationnement");
        AddParagraphe(db, a750.Id, "7504", "Abattoirs");
        AddParagraphe(db, a750.Id, "7505", "Boucheries");
        AddParagraphe(db, a750.Id, "7506", "Produits des régies");
        AddParagraphe(db, a750.Id, "7507", "Produits des services à comptabilité distincte");
        AddParagraphe(db, a750.Id, "7508", "Autres produits des services concédés");

        var a751 = AddArticle(db, c75.Id, "751", "Produits financiers");
        AddParagraphe(db, a751.Id, "7510", "Intérêts des prêts et créances");
        AddParagraphe(db, a751.Id, "7511", "Revenus des placements à terme");
        AddParagraphe(db, a751.Id, "7512", "Revenus du placement des valeur de portefeuille");
        AddParagraphe(db, a751.Id, "7518", "Autres produits financiers");

        // -----------------------------
        // CHAPITRE 76 : DOTATIONS, SUBVENTIONS ET RISTOURNES ACCORDEES PAR L'ETAT
        // -----------------------------
        var c76 = AddChapitre(db, "76", "DOTATIONS, SUBVENTIONS ET RISTOURNES ACCORDEES PAR L'ETAT");

        var a760 = AddArticle(db, c76.Id, "760", "Dotations de fonctionnement");
        AddParagraphe(db, a760.Id, "7600", "Dotations de fonctionnement de l'Etat");

        var a761 = AddArticle(db, c76.Id, "761", "Subventions reçues");
        AddParagraphe(db, a761.Id, "7610", "Subventions spécifiques");
        AddParagraphe(db, a761.Id, "7611", "Subvention de fonctionnement de l'Etat");
        AddParagraphe(db, a761.Id, "7612", "Subvention d'appui à la tutelle");
        AddParagraphe(db, a761.Id, "7618", "Autres subventions");

        var a762 = AddArticle(db, c76.Id, "762", "Ristournes reçues");
        AddParagraphe(db, a762.Id, "7620", "Ristournes reçues de l'Etat");
        AddParagraphe(db, a762.Id, "7621", "Ristournes reçues de la tutelle");
        AddParagraphe(db, a762.Id, "7628", "Autres ristournes reçues");

        // -----------------------------
        // CHAPITRE 77 : PRODUITS EXCEPTIONNELS, ANTERIEURS ET DIVERS
        // -----------------------------
        var c77 = AddChapitre(db, "77", "PRODUITS EXCEPTIONNELS, ANTERIEURS ET DIVERS");

        var a770 = AddArticle(db, c77.Id, "770", "Produits exceptionnels");
        AddParagraphe(db, a770.Id, "7700", "Recettes en atténuation sur dépenses");
        AddParagraphe(db, a770.Id, "7701", "Mandats annulés ou atteints par la déchéance quadriennale");

        var p7702 = AddParagraphe(db, a770.Id, "7702", "Amendes et pénalités reçues");
        AddSousParagraphe(db, p7702.Id, "77021", "Amendes de condamnation civile reçues");
        AddSousParagraphe(db, p7702.Id, "77022", "Pénalités de retard perçues sur les marchés");

        AddParagraphe(db, a770.Id, "7703", "Règlement de créances déjà admises en non-valeur");
        AddParagraphe(db, a770.Id, "7704", "Ventes aux enchères publiques d'éléments du patrimoine de la CL");
        AddParagraphe(db, a770.Id, "7708", "Autres produits exceptionnells");

        var a771 = AddArticle(db, c77.Id, "771", "Produits antérieurs");
        AddParagraphe(db, a771.Id, "7710", "Excédent de fonctionnement reporté");
        AddParagraphe(db, a771.Id, "7711", "Reste à recouvrer sur exercices antérieurs");
        AddParagraphe(db, a771.Id, "7718", "Autres produits antérieurs");

        var a772 = AddArticle(db, c77.Id, "772", "Produits divers");
        AddParagraphe(db, a772.Id, "7720", "Produits de ventes des cahiers de charges d'appels d'offre");
        AddParagraphe(db, a772.Id, "7721", "Contributions volontaires reçues d'autres CL");
        AddParagraphe(db, a772.Id, "7728", "Autres produits divers");
        #endregion

        #region depenses de fonctionnement
        // -----------------------------
        // CHAPITRE 60 : ACHATS
        // -----------------------------
        var c60 = new Nommenclature
        {
            Chapitre = "60",
            Intitule = "ACHATS",
            Nature = NatureType.Depense,
            Section = SectionType.Fonctionnement
        };
        db.Nommenclatures.Add(c60);
        db.SaveChanges();

        var a604 = AddArticledf(db, c60.Id, "604", "Achats non stockés de matières et fournitures");
        AddParagraphedf(db, a604.Id, "6040", "Fourniture de carburant et lubrifiant");
        AddParagraphedf(db, a604.Id, "6042", "Achats de produits d'entretien");
        AddParagraphedf(db, a604.Id, "6043", "Fournitures de pneumatiques et de pièces de rechange");
        AddParagraphedf(db, a604.Id, "6044", "Fournitures et consommables informatiques");
        AddParagraphedf(db, a604.Id, "6045", "Fournitures de bureau");
        AddParagraphedf(db, a604.Id, "6046", "Fournitures d'eau");
        AddParagraphedf(db, a604.Id, "6047", "Fournniture d'électricité");
        AddParagraphedf(db, a604.Id, "6048", "Autres Achats non stockés de matières et fournitures");

        var a608 = AddArticledf(db, c60.Id, "608", "Autres achats");
        AddParagraphedf(db, a608.Id, "6080", "Fournitures de matériels ménagers");
        AddParagraphedf(db, a608.Id, "6081", "Achat de petits matériels et outillages");
        AddParagraphedf(db, a608.Id, "6082", "Fournitures d'habillement");
        AddParagraphedf(db, a608.Id, "6083", "Fournitures pharmaceutiques");
        AddParagraphedf(db, a608.Id, "6084", "Fournitures alimentaires");
        AddParagraphedf(db, a608.Id, "6086", "Achat d'intrants");

        // -----------------------------
        // CHAPITRE 61 : TRANSPORT
        // -----------------------------
        var c61 = AddChapitredf(db, "61", "TRANSPORT");
        AddArticledf(db, c61.Id, "610", "Frais de transport du maire et des conseillers");
        AddArticledf(db, c61.Id, "611", "Frais de transport du personnel de la collectivités");
        AddArticledf(db, c61.Id, "618", "Autres frais de transport");

        // -----------------------------
        // CHAPITRE 62 : CHARGES DU PERSONNEL
        // -----------------------------
        var c62 = AddChapitredf(db, "62", "CHARGES DU PERSONNEL");

        var a621 = AddArticledf(db, c62.Id, "621", "Rémunérations directes versées au personnel permanent de la CL");
        AddParagraphedf(db, a621.Id, "6211", "Salaires et traitements");
        AddParagraphedf(db, a621.Id, "6212", "Primes et gratifications");
        AddParagraphedf(db, a621.Id, "6213", "Indemnités de préavis et de licenciement");
        AddParagraphedf(db, a621.Id, "6214", "Frais médicaux versés au personnel de la CL");
        AddParagraphedf(db, a621.Id, "6215", "Allocations familiales versées au personnel de la CL");
        AddParagraphedf(db, a621.Id, "6218", "Autres rémunérations directes versées au personnel de la CL");

        var a622 = AddArticledf(db, c62.Id, "622", "Rémunérations directes versées au personnel temporaire de la CL");
        AddParagraphedf(db, a622.Id, "6221", "Salaires et traitements");
        AddParagraphedf(db, a622.Id, "6222", "Primes et gratifications");
        AddParagraphedf(db, a622.Id, "6223", "Indemnités de préavis et de licenciement");
        AddParagraphedf(db, a622.Id, "6224", "Frais médicaux versés au personnel de la CL");
        AddParagraphedf(db, a622.Id, "6225", "Allocations familiales versées au personnel de la CL");
        AddParagraphedf(db, a622.Id, "6228", "Autres rémunérations directes versées au personnel de la CL");

        var a623 = AddArticledf(db, c62.Id, "623", "Rémunérations directes versées aux fonctionnaires de l'Etat mis à la disposition de la CL");
        AddParagraphedf(db, a623.Id, "6231", "Salaires et traitements");
        AddParagraphedf(db, a623.Id, "6232", "Primes et gratifications");
        AddParagraphedf(db, a623.Id, "6233", "Indemnités pour soins médicaux");
        AddParagraphedf(db, a623.Id, "6234", "Allocations familiales");
        AddParagraphedf(db, a623.Id, "6238", "Autres rémunérations directes versées");

        var a624 = AddArticledf(db, c62.Id, "624", "Indemnités versées au personnel de la CL");
        AddParagraphedf(db, a624.Id, "6241", "Indemnités de logement");
        AddParagraphedf(db, a624.Id, "6242", "Indemnités de représentation du Maire et Adjoints");
        AddParagraphedf(db, a624.Id, "6243", "Indemnités de sessions des membres du conseil de la CL");
        AddParagraphedf(db, a624.Id, "6248", "Autres indemnités versées au personnel de la CL");

        var a625 = AddArticledf(db, c62.Id, "625", "Charges sociales");
        AddParagraphedf(db, a625.Id, "6251", "Cotisation sociale");
        AddParagraphedf(db, a625.Id, "6252", "Actions sociales en faveur du personnel");
        AddParagraphedf(db, a625.Id, "6258", "Autres charges sociales");

        // -----------------------------
        // CHAPITRE 63 : SERVICES EXTERIEURS
        // -----------------------------
        var c63 = AddChapitredf(db, "63", "SERVICES EXTERIEURS");

        var a630 = AddArticledf(db, c63.Id, "630", "Loyers et charges locatives");
        AddParagraphedf(db, a630.Id, "6300", "Location de terrains");
        AddParagraphedf(db, a630.Id, "6301", "Location de bâtiments");
        AddParagraphedf(db, a630.Id, "6302", "Location de matériel de transport");
        AddParagraphedf(db, a630.Id, "6303", "Location d'équipement, de matériel et outillage");

        var p6304 = AddParagraphedf(db, a630.Id, "6304", "Frais d'hébergement et de restauration");
        AddSousParagraphedf(db, p6304.Id, "63040", "Frais de restauration");
        AddSousParagraphedf(db, p6304.Id, "63041", "Frais d'hôtel");
        AddSousParagraphedf(db, p6304.Id, "63048", "Autres frais d'hébergement et de restauration");

        AddParagraphedf(db, a630.Id, "6308", "Autres loyers et charges locatives");

        var a631 = AddArticledf(db, c63.Id, "631", "Entretien et réparation");
        AddParagraphedf(db, a631.Id, "6310", "Entretien et réparation des matériels de transport de la CL");
        AddParagraphedf(db, a631.Id, "6311", "Entretien et réparation des bâtiments");
        AddParagraphedf(db, a631.Id, "6312", "Entretien et réparation de matériel et mobilier de bureau");
        AddParagraphedf(db, a631.Id, "6313", "Entretien et réparation de matériel informatique");
        AddParagraphedf(db, a631.Id, "6314", "Entretien et réparation des équipements");
        AddParagraphedf(db, a631.Id, "6315", "Entretien et réparation des voies et réseaux");
        AddParagraphedf(db, a631.Id, "6316", "Entretien et réparation des cimétières, espaces verts, parcs et jardins publics");
        AddParagraphedf(db, a631.Id, "6317", "Entretien et réparation du matériel de voirie et réseaux");
        AddParagraphedf(db, a631.Id, "6318", "Autres entretiens et réparations");

        var a632 = AddArticledf(db, c63.Id, "632", "Frais d'assurance");
        AddParagraphedf(db, a632.Id, "6320", "Assurances multirisques");
        AddParagraphedf(db, a632.Id, "6321", "Assurances matériels de transport de la CL");
        AddParagraphedf(db, a632.Id, "6328", "Autres frais d'assurances");

        var a633 = AddArticledf(db, c63.Id, "633", "Frais de documentation, de conservation et d'archivage");
        AddParagraphedf(db, a633.Id, "6330", "Frais de documentation générale");
        AddParagraphedf(db, a633.Id, "6331", "Frais de conservation des archives");
        AddParagraphedf(db, a633.Id, "6338", "Autres frais de documentation, de conservation et d'archivage");

        var a634 = AddArticledf(db, c63.Id, "634", "Publicité, publications, télécommunications et relations publiques");
        AddParagraphedf(db, a634.Id, "6340", "Frais d'annonces et d'insertions");
        AddParagraphedf(db, a634.Id, "6341", "Frais de catalogue, d'imprimés et de registres d'état civil");
        AddParagraphedf(db, a634.Id, "6342", "Frais d'échantillons");
        AddParagraphedf(db, a634.Id, "6343", "Frais de foires et d'expositions");
        AddParagraphedf(db, a634.Id, "6344", "Frais de publication");
        AddParagraphedf(db, a634.Id, "6345", "Frais de colloques, séminaires et conférences");

        var p6346 = AddParagraphedf(db, a634.Id, "6346", "Frais de télécommunication");
        AddSousParagraphedf(db, p6346.Id, "63460", "Frais de téléphone");
        AddSousParagraphedf(db, p6346.Id, "63461", "Frais d'internet et site web");
        AddSousParagraphedf(db, p6346.Id, "63462", "Frais de télécopie");
        AddSousParagraphedf(db, p6346.Id, "63468", "Autres frais de télécommunication");

        var p6347 = AddParagraphedf(db, a634.Id, "6347", "Frais d'abonnement au logiciel");
        AddSousParagraphedf(db, p6347.Id, "63470", "Frais d'abonnement aux logiciels");
        AddSousParagraphedf(db, p6347.Id, "63471", "Frais d'abonnement aux sites internet");
        AddSousParagraphedf(db, p6347.Id, "63478", "Autres frais d'abonnement aux logiciels et sites internet");

        AddParagraphedf(db, a634.Id, "6348", "Autres frais de publicité, publications et relations publiques");

        var a635 = AddArticledf(db, c63.Id, "635", "Achat de valeurs et titres");
        AddParagraphedf(db, a635.Id, "6350", "Achat de carnets de reçu");
        AddParagraphedf(db, a635.Id, "6351", "Achat de timbres");
        AddParagraphedf(db, a635.Id, "6352", "Achat de tickets");
        AddParagraphedf(db, a635.Id, "6358", "Autres achats de valeurs et titres");

        var a636 = AddArticledf(db, c63.Id, "636", "Frais bancaires");
        AddParagraphedf(db, a636.Id, "6360", "Frais des agios");
        AddParagraphedf(db, a636.Id, "6361", "Frais sur effets");
        AddParagraphedf(db, a636.Id, "6368", "Autres frais bancaires");

        var a637 = AddArticledf(db, c63.Id, "637", "Rémunérations d'intermédiaires et de conseils");
        AddParagraphedf(db, a637.Id, "6370", "Frais d'honoraires");
        AddParagraphedf(db, a637.Id, "6371", "Frais de justice, d'actes et de contentieux");
        AddParagraphedf(db, a637.Id, "6372", "Rémunérations accordées aux bénévoles");
        AddParagraphedf(db, a637.Id, "6373", "Rémunérations des autres prestataires de services");
        AddParagraphedf(db, a637.Id, "6378", "Autres frais de rémunération d'intermédiaires et de conseils");

        var a638 = AddArticledf(db, c63.Id, "638", "Charges du personnel et des conseillers");

        var p6380 = AddParagraphedf(db, a638.Id, "6380", "Frais de formation des membres du conseil et du personnel de la CL");
        AddSousParagraphedf(db, p6380.Id, "63800", "Frais de formation");
        AddSousParagraphedf(db, p6380.Id, "63801", "Frais de stage");
        AddSousParagraphedf(db, p6380.Id, "63802", "Frais de voyages d'études");
        AddSousParagraphedf(db, p6380.Id, "63808", "Autres frais de formation des membres du conseil et du personnel de la CL");

        var p6381 = AddParagraphedf(db, a638.Id, "6381", "Frais de missions");
        AddSousParagraphedf(db, p6381.Id, "63810", "Frais de missions du Maire, des vices-maires et des conseillers à l'intérieur");
        AddSousParagraphedf(db, p6381.Id, "63811", "Frais de missions du Maire, des vices-maires et des conseillers à l'extérieur");
        AddSousParagraphedf(db, p6381.Id, "63812", "Frais de mission du personnel de la collectivité à l'intérieur");
        AddSousParagraphedf(db, p6381.Id, "63813", "Frais de mission du personnel de la collectivité à l'extérieur");
        AddSousParagraphedf(db, p6381.Id, "63818", "Autres frais de mission");

        var p6388 = AddParagraphedf(db, a638.Id, "6388", "Autres charges externes");
        AddSousParagraphedf(db, p6388.Id, "63880", "Frais de recrutement du personnel");
        AddSousParagraphedf(db, p6388.Id, "63881", "Frais de déménagement");
        AddSousParagraphedf(db, p6388.Id, "63888", "Autres charges externes");

        var a639 = AddArticledf(db, c63.Id, "639", "Frais pour manifestations et cérémonies");

        // -----------------------------
        // CHAPITRE 64 : INTERVENTIONS A CARACTERE ECONOMIQUE ET SOCIAL
        // -----------------------------
        var c64 = AddChapitredf(db, "64", "INTERVENTIONS A CARACTERE ECONOMIQUE ET SOCIAL");

        var a640 = AddArticledf(db, c64.Id, "640", "Dotations");
        AddParagraphedf(db, a640.Id, "6400", "Dotations pour entretiens et réparations des établissements préscolaires et scolaires");
        AddParagraphedf(db, a640.Id, "6401", "Dotations pour entretiens et réparations des établissements sanitaires");
        AddParagraphedf(db, a640.Id, "6402", "Dotations pour entretiens et réparations des centres de culture, d'arts et de sports");
        AddParagraphedf(db, a640.Id, "6403", "Dotations pour entretiens et réparations des voiries");
        AddParagraphedf(db, a640.Id, "6404", "Dotations pour entretiens et réparations des marchés");
        AddParagraphedf(db, a640.Id, "6405", "Dotations pour entretiens et réparations des gares routières");
        AddParagraphedf(db, a640.Id, "6406", "Dotations pour entretiens et réparations des abattoirs");
        AddParagraphedf(db, a640.Id, "6407", "Dotations pour entretiens et réparations des cimetières, parcs, jardins et points d'eau");
        AddParagraphedf(db, a640.Id, "6408", "Dotations de fonctionnement aux services de protection civile et de lutte contre l'incendie");

        var a641 = AddArticledf(db, c64.Id, "641", "Subventions et allocations accordées");
        AddParagraphedf(db, a641.Id, "6410", "Subventions à la caisse de péréquation inter-collectivités locales");
        AddParagraphedf(db, a641.Id, "6411", "Subventions aux associations de jeunesse et de sport");
        AddParagraphedf(db, a641.Id, "6412", "Subventions aux associations culturelles et artistiques");
        AddParagraphedf(db, a641.Id, "6413", "Subventions pour manifestations diverses");
        AddParagraphedf(db, a641.Id, "6414", "Subventions aux organismes socio-économiques");
        AddParagraphedf(db, a641.Id, "6415", "Subventions aux sinistrés et indigents");
        AddParagraphedf(db, a641.Id, "6418", "Autres subventions et allocations");

        var a642 = AddArticledf(db, c64.Id, "642", "Actions socio-économiques");
        AddParagraphedf(db, a642.Id, "6421", "Actions économiques d'intérêt de la CL");
        AddParagraphedf(db, a642.Id, "6422", "Actions de jumelage et de coopération");
        AddParagraphedf(db, a642.Id, "6428", "Autres actions socio-économiques");

        // -----------------------------
        // CHAPITRE 65 : FRAIS FINANCIERS ET CHARGES ASSIMILEES
        // -----------------------------
        var c65 = AddChapitredf(db, "65", "FRAIS FINANCIERS ET CHARGES ASSIMILEES");

        var a650 = AddArticledf(db, c65.Id, "650", "Frais financiers");
        AddParagraphedf(db, a650.Id, "6501", "Frais et commissions sur emprunts auprès des établissements de crédit");
        AddParagraphedf(db, a650.Id, "6502", "Frais et commissions sur autres emprunts");

        AddArticledf(db, c65.Id, "651", "Intérêts sur emprunts");
        AddArticledf(db, c65.Id, "652", "Intérêts sur compte courant");
        AddArticledf(db, c65.Id, "653", "Intérêts sur dettes diverses");
        AddArticledf(db, c65.Id, "654", "Pertes sur cessions de titres");
        AddArticledf(db, c65.Id, "655", "Pertes de charges financières");
        AddArticledf(db, c65.Id, "656", "Charges de régies");
        AddArticledf(db, c65.Id, "658", "Autres frais financiers et charges assimilées");

        // -----------------------------
        // CHAPITRE 66 : CHARGES EXCEPTIONNELLES, ANTERIEURES ET DIVERSES
        // -----------------------------
        var c66 = AddChapitredf(db, "66", "CHARGES EXCEPTIONNELLES, ANTERIEURES ET DIVERSES");

        var a660 = AddArticledf(db, c66.Id, "660", "Charges expcetionnelles");
        AddParagraphedf(db, a660.Id, "6601", "Annulation et réduction des ordres de recettes");

        var p6602 = AddParagraphedf(db, a660.Id, "6602", "Amendes et pénalités payées par la CL");
        AddSousParagraphedf(db, p6602.Id, "66021", "Amendes  payées par la CL");
        AddSousParagraphedf(db, p6602.Id, "66022", "Pénalités payées par la CL");

        AddParagraphedf(db, a660.Id, "6604", "Admission en non valeur");
        AddParagraphedf(db, a660.Id, "6607", "Valeur comptable nette des immobilisations cédées");
        AddParagraphedf(db, a660.Id, "6608", "Autres charges exceptionnelles");

        var a661 = AddArticledf(db, c66.Id, "661", " Charges antérieures");
        AddParagraphedf(db, a661.Id, "6610", "Déficit de fonctionnement reporté");
        AddParagraphedf(db, a661.Id, "6611", "Arriérés de cotisations");
        AddParagraphedf(db, a661.Id, "6612", "Charges des exercices antérieurs");
        AddParagraphedf(db, a661.Id, "6618", "Autres charges exterieures");

        AddArticledf(db, c66.Id, "662", "Prélèvement pour dépenses d'investissements");

        var a663 = AddArticledf(db, c66.Id, "663", "Charges diverses");
        AddParagraphedf(db, a663.Id, "6630", "Pénalités de retard sur marchés payées par la CL");
        AddParagraphedf(db, a663.Id, "6631", "Frais de perception et de recouvrement des impôts et taxes");
        AddParagraphedf(db, a663.Id, "6632", "Indemnités d'expropriation versées");
        AddParagraphedf(db, a663.Id, "6633", "Frais d'inhumation des corps abandonnés");
        AddParagraphedf(db, a663.Id, "6635", "Quotes-parts versées aux quartiers");
        AddParagraphedf(db, a663.Id, "6636", "Quotes-parts versées aux Districts");
        AddParagraphedf(db, a663.Id, "6638", "Autres charges diverses");

        #endregion

        #region recettes d'investissement
        // -----------------------------
        // CHAPITRE 10 : DOTATIONS
        // -----------------------------
        var c10 = new Nommenclature
        {
            Chapitre = "10",
            Intitule = "DOTATIONS",
            Nature = NatureType.Recette,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(c10);
        db.SaveChanges();

        AddArticleri(db, c10.Id, "100", "Dotations initiales");
        AddArticleri(db, c10.Id, "101", "Dotations complémentaires d'équipements");

        // -----------------------------
        // CHAPITRE 11 : PRELEVEMENTS
        // -----------------------------
        var c11 = AddChapitreri(db, "11", "PRELEVEMENTS");
        AddArticleri(db, c11.Id, "110", "Prélèvements sur recettes de fonctionnement");

        // -----------------------------
        // CHAPITRE 12 : FONDS DE RESERVES
        // -----------------------------
        var c12 = AddChapitreri(db, "12", "FONDS DE RESERVES");
        AddArticleri(db, c12.Id, "120", "Fonds de réserves d'investissement");
        AddArticleri(db, c12.Id, "121", "Fonds de réserves d'amortissement des équipements et du mobilier");

        // -----------------------------
        // CHAPITRE 13 : RESULTAT
        // -----------------------------
        var c13 = AddChapitreri(db, "13", "RESULTAT");
        var a139 = AddArticleri(db, c13.Id, "139", "Résultat net");
        AddParagrapheri(db, a139.Id, "1390", "Excédent d'investissements reporté");

        // -----------------------------
        // CHAPITRE 14 : SUBVENTIONS D'EQUIPEMENTS
        // -----------------------------
        var c14 = AddChapitreri(db, "14", "SUBVENTIONS D'EQUIPEMENTS");
        AddArticleri(db, c14.Id, "140", "Subventions d'équipement reçues de l'Etat");
        AddArticleri(db, c14.Id, "148", "Autres subventions d'équipements");

        // -----------------------------
        // CHAPITRE 15 : DONS, LEGS, CONTRIBUTIONS VOLONTAIRES ET TRANSFERTS DE PROPRIETE
        // -----------------------------
        var c15 = AddChapitreri(db, "15", "DONS, LEGS, CONTRIBUTIONS VOLONTAIRES ET TRANSFERTS DE PROPRIETE");

        var a150 = AddArticleri(db, c15.Id, "150", "Produits des dons reçus");
        AddParagrapheri(db, a150.Id, "1500", "Produits des dons en nature avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1501", "Produits des dons en nature sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1502", "Produits des dons en espèce avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1503", "Produits des dons en espèce sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a150.Id, "1504", "Produits des dons en nature avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1505", "Produits des dons en nature sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1506", "Produits des dons en espèces avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1507", "Produits des dons en espèces sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a150.Id, "1508", "Autres produits des dons");

        var a151 = AddArticleri(db, c15.Id, "151", "Produits des legs reçus");
        AddParagrapheri(db, a151.Id, "1510", "Produits des legs en nature avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1511", "Produits des legs en nature sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1512", "Produits des legs en espèce avec affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1513", "Produits des legs en espèce sans affectation particulière reçus de l'Etat");
        AddParagrapheri(db, a151.Id, "1514", "Produits des legs en nature avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1515", "Produits des legs en nature sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1516", "Produits des legs en espèces avec affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1517", "Produits des legs en espèces sans affectation particulière reçus d'autres organismes");
        AddParagrapheri(db, a151.Id, "1518", "Autres produits des legs reçus");

        var a152 = AddArticleri(db, c15.Id, "152", "Contributions volontaires");
        AddParagrapheri(db, a152.Id, "1520", "Contributions volontaires en nature");
        AddParagrapheri(db, a152.Id, "1521", "Contributions volontaires en espèces");
        AddParagrapheri(db, a152.Id, "1528", "Autres contributions");

        var a153 = AddArticleri(db, c15.Id, "153", "Transferts de propriété");
        AddParagrapheri(db, a153.Id, "1530", "Transferts d'immobilisations incorporelles");
        AddParagrapheri(db, a153.Id, "1531", "Transferts d'immobilisations corporelles");

        // -----------------------------
        // CHAPITRE 16 : FONDS DE CONCOURS, D'AIDES ET DE PEREQUATION
        // -----------------------------
        var c16 = AddChapitreri(db, "16", "FONDS DE CONCOURS, D'AIDES ET DE PEREQUATION");

        var a160 = AddArticleri(db, c16.Id, "160", "Fonds de concours");
        AddParagrapheri(db, a160.Id, "1600", "Fonds de concours reçus de l'Etat");
        AddParagrapheri(db, a160.Id, "1601", "Fonds de concours reçus d'autres CL");
        AddParagrapheri(db, a160.Id, "1602", "Fonds de concours reçus d'organismes nationaux");
        AddParagrapheri(db, a160.Id, "1603", "Fonds de concours reçus d'organismes étrangers");
        AddParagrapheri(db, a160.Id, "1608", "Autres fonds de concours reçus");

        var a161 = AddArticleri(db, c16.Id, "161", "Fonds d'aide");
        AddParagrapheri(db, a161.Id, "1610", "Fonds d'aide reçus de l'Etat");
        AddParagrapheri(db, a161.Id, "1611", "Fonds d'aide reçus d'autres CL");
        AddParagrapheri(db, a161.Id, "1612", "Fonds d'aide reçus d'organismes nationaux");
        AddParagrapheri(db, a161.Id, "1613", "Fonds d'aide reçus d'organismes étrangers");
        AddParagrapheri(db, a161.Id, "1618", "Fonds d'aide reçus d'autres organismes");

        var a162 = AddArticleri(db, c16.Id, "162", "Fonds de péréquation reçus");
        AddParagrapheri(db, a162.Id, "1620", "Fonds de concours reçus du Fonds National de Développement Local /ANAFIC");
        AddParagrapheri(db, a162.Id, "1628", "Autres fonds de concours, d'aide et de péréquation");

        // -----------------------------
        // CHAPITRE 17 : EMPRUNTS ET DETTES ASSIMILEES
        // -----------------------------
        var c17 = AddChapitreri(db, "17", "EMPRUNTS ET DETTES ASSIMILEES");

        var a170 = AddArticleri(db, c17.Id, "170", "Emprunts reçus");

        var p1700 = AddParagrapheri(db, a170.Id, "1700", "Emprunts auprès des établissement de crédits");
        AddSousParagrapheri(db, p1700.Id, "17001", "En monnaie nationale");
        AddSousParagrapheri(db, p1700.Id, "17002", "En devises");

        AddParagrapheri(db, a170.Id, "1708", "Autres emprunts");

        var a171 = AddArticleri(db, c17.Id, "171", "Dettes assimilées");
        AddParagrapheri(db, a171.Id, "1710", "Dépôts et cautionnements reçus");
        AddParagrapheri(db, a171.Id, "1711", "Intérêts courus des emprunts");
        AddParagrapheri(db, a171.Id, "1718", "Autres dettes assimilées");

        // -----------------------------
        // CHAPITRE 18 : VALEURS DE PORTEFEUILLE ET DE L'ALIENATION DU PATRIMOINE
        // -----------------------------
        var c18 = AddChapitreri(db, "18", "VALEURS DE PORTEFEUILLE ET DE L'ALIENATION DU PATRIMOINE");

        var a180 = AddArticleri(db, c18.Id, "180", "Vente de valeurs en portefeuille");
        AddParagrapheri(db, a180.Id, "1801", "Vente de terrains");
        AddParagrapheri(db, a180.Id, "1802", "Vente de réserves foncières");
        AddParagrapheri(db, a180.Id, "1803", "Vente de bâtiments");
        AddParagrapheri(db, a180.Id, "1804", "Vente d'équipements, matériels, mobiliers, outillage et actifs biologiques");
        AddParagrapheri(db, a180.Id, "1805", "Vente aux enchères publiques d'éléments du patrimoine de la CL");

        var a181 = AddArticleri(db, c18.Id, "181", "Revenus du secteur minier");
        AddParagrapheri(db, a181.Id, "1810", "Taxe superficiaire");
        AddParagrapheri(db, a181.Id, "1811", "Quote-part sur les taxes minières affectées au développement local");
        AddParagrapheri(db, a181.Id, "1812", "Contributions volontaires des sociétés minières au développement communautaire");
        AddParagrapheri(db, a181.Id, "1818", "Autres recettes minières");

        var a182 = AddArticleri(db, c18.Id, "182", "Revenus d'exploitation d'autres valeurs en portefeuille");
        AddParagrapheri(db, a182.Id, "1820", "Secteur transport");
        AddParagrapheri(db, a182.Id, "1821", "Secteur actif biologique");
        AddParagrapheri(db, a182.Id, "1822", "Secteur agricole");
        AddParagrapheri(db, a182.Id, "1828", "Autres revenus des secteurs d'exploitation");

        // -----------------------------
        // CHAPITRE 19 : RECETTES DIVERSES
        // -----------------------------
        var c19 = AddChapitreri(db, "19", "RECETTES DIVERSES");
        AddArticleri(db, c19.Id, "190", "Recettes en atténuation des dépenses");
        AddArticleri(db, c19.Id, "191", "Revenus antérieurs divers");

        #endregion

        #region depenses d'investissement
        // -----------------------------
        // CHAPITRE 10 : DOTATIONS
        // -----------------------------
        var c10di = new Nommenclature
        {
            Chapitre = "10",
            Intitule = "DOTATIONS",
            Nature = NatureType.Depense,
            Section = SectionType.Investissement
        };
        db.Nommenclatures.Add(c10di);
        db.SaveChanges();

        AddArticledi(db, c10di.Id, "100", "Dotations d'équipement accordées");

        // -----------------------------
        // CHAPITRE 14 : SUBVENTIONS D'EQUIPEMENTS ACCORDEES
        // -----------------------------
        var c14di = AddChapitredi(db, "14", "SUBVENTIONS D'EQUIPEMENTS ACCORDEES");
        AddArticledi(db, c14di.Id, "146", "Subventions d'équipements accordées");
        AddArticledi(db, c14di.Id, "148", "Autres subventions d'équipements");

        // -----------------------------
        // CHAPITRE 16 : FONDS DE CONCOURS, D'AIDES  ET DE PEREQUATION
        // -----------------------------
        var c16di = AddChapitredi(db, "16", "FONDS DE CONCOURS, D'AIDES  ET DE PEREQUATION");
        AddArticledi(db, c16di.Id, "165", "Fonds de concours et d'aides attribués par la CL");
        AddArticledi(db, c16di.Id, "168", "Autres fonds de concours et d'aides attribués par La CL");

        // -----------------------------
        // CHAPITRE 17 : EMPRUNTS ET DETTES ASSIMILES
        // -----------------------------
        var c17di = AddChapitredi(db, "17", "EMPRUNTS ET DETTES ASSIMILES");

        var a176 = AddArticledi(db, c17di.Id, "176", "Remboursement du capital des emprunts");
        AddParagraphedi(db, a176.Id, "1760", "Remboursement du capital des emprunts auprès des établissements de crédit");

        var p1761 = AddParagraphedi(db, a176.Id, "1761", "Remboursement des dettes assimilées");
        AddSousParagraphedi(db, p1761.Id, "17610", "Dépôts et cautionnements versés");

        AddParagraphedi(db, a176.Id, "1768", "Remboursement d'autres emprunts et dettes assimilées");

        // -----------------------------
        // CHAPITRE 18 : VALEURS EN PORTEFAUILLE DE L'ALIENATION DU PATRIMOINE
        // -----------------------------
        var c18di = AddChapitredi(db, "18", "VALEURS EN PORTEFAUILLE DE L'ALIENATION DU PATRIMOINE");

        var a186 = AddArticledi(db, c18di.Id, "186", "Acquisition de valeurs en portefeuille et de patrimoine");
        AddParagraphedi(db, a186.Id, "1861", "Titres");
        AddParagraphedi(db, a186.Id, "1862", "Actions");
        AddParagraphedi(db, a186.Id, "1863", "Participations");
        AddParagraphedi(db, a186.Id, "1868", "Autres valeurs de portefeuille acquises");

        // -----------------------------
        // CHAPITRE 21 : ACQUISITIONS D'IMMOBILISATIONS INCORPORELLES
        // -----------------------------
        var c21 = AddChapitredi(db, "21", "ACQUISITIONS D'IMMOBILISATIONS INCORPORELLES");

        var a210 = AddArticledi(db, c21.Id, "210", "Frais d'études et de recherches");
        AddParagraphedi(db, a210.Id, "2100", "Frais d'études");
        AddParagraphedi(db, a210.Id, "2101", "Frais de recherches");

        var a213 = AddArticledi(db, c21.Id, "213", "Logiciels et sites internet");
        AddParagraphedi(db, a213.Id, "2131", "Logiciels");
        AddParagraphedi(db, a213.Id, "2132", "Sites internet");

        AddArticledi(db, c21.Id, "214", "Documentation technique");
        AddArticledi(db, c21.Id, "215", "Assistance technique");
        AddArticledi(db, c21.Id, "216", "Supervision des travaux");
        AddArticledi(db, c21.Id, "218", "Autres immobilisations incorporelles");

        var a219 = AddArticledi(db, c21.Id, "219", "Immobilisations incorporelles en cours");
        AddParagraphedi(db, a219.Id, "2190", "Frais d'étude en cours");
        AddParagraphedi(db, a219.Id, "2191", "Frais de recherches en cours");

        var p2193 = AddParagraphedi(db, a219.Id, "2193", "Logiciels, sites internet et web en cours");
        AddSousParagraphedi(db, p2193.Id, "21931", "Logiciels en cours");
        AddSousParagraphedi(db, p2193.Id, "21932", "Site internet en cours");
        AddSousParagraphedi(db, p2193.Id, "21933", "Web en cours");

        AddParagraphedi(db, a219.Id, "2194", "Frais de documentation technique en cours");
        AddParagraphedi(db, a219.Id, "2198", "Autres immobilisations incorporelles en cours");

        // -----------------------------
        // CHAPITRE 22 : TERRAINS, SOUS-SOL ET SOL
        // -----------------------------
        var c22 = AddChapitredi(db, "22", "TERRAINS, SOUS-SOL ET SOL");

        var a220 = AddArticledi(db, c22.Id, "220", "Terrains de mines");
        AddParagraphedi(db, a220.Id, "2201", "Terrains miniers en exploitation");

        var a221 = AddArticledi(db, c22.Id, "221", "Terrains agricoles, forestiers, pastoraux et piscicoles");
        AddParagraphedi(db, a221.Id, "2211", "Terrains d'exploitation agricole");
        AddParagraphedi(db, a221.Id, "2212", "Terrains d'exploitation forestière");
        AddParagraphedi(db, a221.Id, "2213", "Terrains d'exploitation pastorale");
        AddParagraphedi(db, a221.Id, "2214", "Terrains d'exploitation piscicoles");
        AddParagraphedi(db, a221.Id, "2218", "Autres terrains agricoles, forestiers, pastoraux et piscicoles");

        var a222 = AddArticledi(db, c22.Id, "222", "Terrains nus");
        AddParagraphedi(db, a222.Id, "2221", "Terrains à bâtir");
        AddParagraphedi(db, a222.Id, "2228", "Autres terrains nus");

        var a223 = AddArticledi(db, c22.Id, "223", "Travaux de mise en valeur des terrains");

        var p2231 = AddParagraphedi(db, a223.Id, "2231", "Travaux de mise en valeur des terrains agricoles, forestiers pastoraux et piscicoles");
        AddSousParagraphedi(db, p2231.Id, "22310", "Travaux de mise en valeur des terrains agricoles");
        AddSousParagraphedi(db, p2231.Id, "22311", "Travaux de mise en valeur des terrains forestiers");
        AddSousParagraphedi(db, p2231.Id, "22312", "Travaux de mise en valeur des terrains pastoraux");
        AddSousParagraphedi(db, p2231.Id, "22313", "Travaux de mise en valeur des terrains piscicoles");

        AddParagraphedi(db, a223.Id, "2234", "Plantations d'arbres d'arbustes et de pépinières");
        AddParagraphedi(db, a223.Id, "2235", "Amélioration de bas-fond");
        AddParagraphedi(db, a223.Id, "2238", "Autres travaux de mise en valeur des terrains");

        // -----------------------------
        // CHAPITRE 23 : BATIMENTS, INSTALLATIONS TECHNIQUES ET AGENCEMENTS
        // -----------------------------
        var c23 = AddChapitredi(db, "23", "BATIMENTS, INSTALLATIONS TECHNIQUES ET AGENCEMENTS");

        var a231 = AddArticledi(db, c23.Id, "231", "Travaux de bâtiments sur sol propre");
        AddParagraphedi(db, a231.Id, "2310", "Bâtiments administratifs");
        AddParagraphedi(db, a231.Id, "2311", "Bâtiments agricoles");
        AddParagraphedi(db, a231.Id, "2312", "Bâtiments affectés au logement du personnel de la CL");
        AddParagraphedi(db, a231.Id, "2313", "Bâtiments culturels et sportifs");
        AddParagraphedi(db, a231.Id, "2314", "Bâtiments en location");
        AddParagraphedi(db, a231.Id, "2315", "Centre d'alphabétisation");
        AddParagraphedi(db, a231.Id, "2316", "Centre de lecture");
        AddParagraphedi(db, a231.Id, "2318", "Autres bâtiments sur sol propre");

        var a232 = AddArticledi(db, c23.Id, "232", "Travaux de bâtiments sur sol d'autrui");
        AddParagraphedi(db, a232.Id, "2320", "Bâtiments administratifs");
        AddParagraphedi(db, a232.Id, "2321", "Bâtiments agricoles");
        AddParagraphedi(db, a232.Id, "2322", "Bâtiments affectés au logement du personnel de la CL");
        AddParagraphedi(db, a232.Id, "2323", "Bâtiments culturels et sportifs");
        AddParagraphedi(db, a232.Id, "2324", "Bâtiments en location");
        AddParagraphedi(db, a232.Id, "2325", "Centre d'alphabétisation");
        AddParagraphedi(db, a232.Id, "2326", "Centre de lecture");
        AddParagraphedi(db, a232.Id, "2328", "Autres bâtiments sur sol d'autrui");

        var a233 = AddArticledi(db, c23.Id, "233", "Travaux d'ouvrages et d'infrastructures");
        AddParagraphedi(db, a233.Id, "2331", "Voies de terre");
        AddParagraphedi(db, a233.Id, "2333", "Voies d'eau");
        AddParagraphedi(db, a233.Id, "2334", "Barrages, digues");
        AddParagraphedi(db, a233.Id, "2338", "Autres ouvrages d'infratructures");

        var a234 = AddArticledi(db, c23.Id, "234", "Travaux d'aménagements, agencements, installations techniques");
        AddParagraphedi(db, a234.Id, "2341", "Installations complexes spécialisées sur sol propre");
        AddParagraphedi(db, a234.Id, "2342", "Installations complexes spécialisées sur sol d'autrui");
        AddParagraphedi(db, a234.Id, "2343", "Installations à caractère spécifique sur sol propre");
        AddParagraphedi(db, a234.Id, "2344", "Installations à caractère spécifique sur d'autrui");
        AddParagraphedi(db, a234.Id, "2345", "Agencements et aménagements des bâtiments");
        AddParagraphedi(db, a234.Id, "2348", "Autres aménagements, agencements, installations techniques");

        var a235 = AddArticledi(db, c23.Id, "235", "Travaux d'aménagement de bureaux");
        AddParagraphedi(db, a235.Id, "2351", "Installations générales");
        AddParagraphedi(db, a235.Id, "2358", "Autres aménagements, agencements, installations techniques");

        AddArticledi(db, c23.Id, "238", "Autres travaux d'installations et d'amenagements");

        var a239 = AddArticledi(db, c23.Id, "239", "Travaux de bâtiments, aménagements, agencements et installations en cours");
        AddParagraphedi(db, a239.Id, "2391", "Bâtiments en cours");
        AddParagraphedi(db, a239.Id, "2392", "Installations en cours");
        AddParagraphedi(db, a239.Id, "2393", "Ouvrages d'infrastructures en cours");
        AddParagraphedi(db, a239.Id, "2394", "Agencements, aménagements, installations techniques en cours");
        AddParagraphedi(db, a239.Id, "2395", "Aménagements de bureaux en cours");
        AddParagraphedi(db, a239.Id, "2398", "Autres installations et aménagements en cours");

        // -----------------------------
        // CHAPITRE 24 : EQUIPEMENTS PUBLICS, MATERIELS, MOBILIERS, OUTILLAGES ET ACTIFS BIOLOGIQUES
        // -----------------------------
        var c24 = AddChapitredi(db, "24", "EQUIPEMENTS PUBLICS, MATERIELS, MOBILIERS, OUTILLAGES ET ACTIFS BIOLOGIQUES");

        var a240 = AddArticledi(db, c24.Id, "240", "Travaux d'équipements publics");
        AddParagraphedi(db, a240.Id, "2400", "Travaux d'équipement sanitaire");
        AddParagraphedi(db, a240.Id, "2401", "Travaux d'équipement scolaire");
        AddParagraphedi(db, a240.Id, "2402", "Travaux d'équipement de centres cluturel et d'art");
        AddParagraphedi(db, a240.Id, "2403", "Travaux d'équipement de centre de sport");
        AddParagraphedi(db, a240.Id, "2404", "Travaux d'équipement gare-routière");
        AddParagraphedi(db, a240.Id, "2405", "Travaux d'équipement des abattoirs");
        AddParagraphedi(db, a240.Id, "2406", "Travaux d'équipement hydraulique");
        AddParagraphedi(db, a240.Id, "2407", "Travaux d'équipement d'électrification");
        AddParagraphedi(db, a240.Id, "2408", "Autres travaux d'équipements publics");

        var a241 = AddArticledi(db, c24.Id, "241", "Travaux d'équipements et actions d'assainissement, d'hygiène et de salubrité");
        AddParagraphedi(db, a241.Id, "2410", "Travaux d'équipement et d'actions d'assainissement");
        AddParagraphedi(db, a241.Id, "2411", "Travaux d'équipement d'hygiène et de salubrité");
        AddParagraphedi(db, a241.Id, "2418", "Autres travaux d'équipements d'hygiène et de salubrité");

        var a242 = AddArticledi(db, c24.Id, "242", "Acquisition matériel et outillage agricole");
        AddParagraphedi(db, a242.Id, "2421", "Acquisition matériel agricole");
        AddParagraphedi(db, a242.Id, "2422", "Acquisition outillage agricole");
        AddParagraphedi(db, a242.Id, "2428", "Acquisition d'autres matériels et outillages agricoles");

        var a244 = AddArticledi(db, c24.Id, "244", "Acquisition matériel et mobilier");
        AddParagraphedi(db, a244.Id, "2441", "Acquisition de matériel de bureau");
        AddParagraphedi(db, a244.Id, "2442", "Acquisition de matériel informatique");
        AddParagraphedi(db, a244.Id, "2444", "Acquisition de mobilier de bureau");
        AddParagraphedi(db, a244.Id, "2447", "Acquisition de matériel et mobilier de logement du personnel de la CL");
        AddParagraphedi(db, a244.Id, "2448", "Acquisition d'autres matériels et mobiliers");

        var a245 = AddArticledi(db, c24.Id, "245", "Acquisition de matériel de transport");
        AddParagraphedi(db, a245.Id, "2451", "Acquisition de matériel automobile");
        AddParagraphedi(db, a245.Id, "2453", "Acquisition de matériel fluvial , lagunaire");
        AddParagraphedi(db, a245.Id, "2454", "Acquisition de matériel naval");
        AddParagraphedi(db, a245.Id, "2456", "Acquisition de matériel de transport à usage locatif");
        AddParagraphedi(db, a245.Id, "2458", "Acquisition d'autres matériels de transport");

        var a246 = AddArticledi(db, c24.Id, "246", "Acquisition d'actifs biologiques");
        AddParagraphedi(db, a246.Id, "2461", "Acquisition de cheptel, d'animaux de trait");
        AddParagraphedi(db, a246.Id, "2462", "Acquisition d'animaux reproducteurs");
        AddParagraphedi(db, a246.Id, "2463", "Acquisition d'animaux de garde");
        AddParagraphedi(db, a246.Id, "2464", "Acquisition d'animaux de transport");
        AddParagraphedi(db, a246.Id, "2465", "Acquisition de plantations agricoles");
        AddParagraphedi(db, a246.Id, "2468", "Acquisition d'autres actifs biologiques");

        var a247 = AddArticledi(db, c24.Id, "247", "Travaux d'agencement et d'aménagement des équipements publics, matériels, mobiliers et actifs biologiques");
        AddParagraphedi(db, a247.Id, "2470", "Travaux d'agencement et d'aménagement des équipements publics");
        AddParagraphedi(db, a247.Id, "2471", "Travaux d'agencement et d'aménagement du matériel");
        AddParagraphedi(db, a247.Id, "2472", "Travaux d'agencement et d'aménagement du mobilier");
        AddParagraphedi(db, a247.Id, "2476", "Travaux d'agencement et d'aménagement des  actifs biologiques");
        AddParagraphedi(db, a247.Id, "2478", "Autres travaux d'agencement et d'aménagement des équipements du matériel et mobilier");

        AddArticledi(db, c24.Id, "248", "Acquisition d'autres équipements publics, matériels, mobiliers et actifs biologiques");

        // -----------------------------
        // CHAPITRE 25 : TITRES DE PARTICIPATION, AVANCES ET ACOMPTES VERSES
        // -----------------------------
        var c25 = AddChapitredi(db, "25", "TITRES DE PARTICIPATION, AVANCES ET ACOMPTES VERSES");

        AddArticledi(db, c25.Id, "250", "Titres de participation");

        var a251 = AddArticledi(db, c25.Id, "251", "Avances et acomptes versés");
        AddParagraphedi(db, a251.Id, "2510", "Avances et acomptes versés sur immobilisations incorporelles");
        AddParagraphedi(db, a251.Id, "2511", "Avances et acomptes versés sur immobilisations corporelles");

        var a253 = AddArticledi(db, c25.Id, "253", "Créances de la collectivité");
        AddParagraphedi(db, a253.Id, "2531", "Retenues de garanties");
        AddParagraphedi(db, a253.Id, "2538", "Autres créances");

        var a255 = AddArticledi(db, c25.Id, "255", "Dépôts et cautionnements versés");
        AddParagraphedi(db, a255.Id, "2551", "Dépôts et cautionnements sur loyer");
        AddParagraphedi(db, a255.Id, "2552", "Dépôts et cautionnements sur l'électricité");
        AddParagraphedi(db, a255.Id, "2553", "Dépôts et cautionnements sur eau");
        AddParagraphedi(db, a255.Id, "2554", "Dépôts et cautionnements sur gaz");
        AddParagraphedi(db, a255.Id, "2555", "Dépôts et cautionnements sur télécommunication");
        AddParagraphedi(db, a255.Id, "2558", "Autres immobilisations financières");

        // -----------------------------
        // CHAPITRE 26 : CHARGES DIVERSES
        // -----------------------------
        var c26 = AddChapitredi(db, "26", "CHARGES DIVERSES");
        AddArticledi(db, c26.Id, "260", "Annulation d'ordre de recettes");
        AddArticledi(db, c26.Id, "261", "Admission en non valeur");
        AddArticledi(db, c26.Id, "262", "Charges des exercices antérieurs");

        // -----------------------------
        // CHAPITRE 29 : RESULTATS
        // -----------------------------
        var c29 = AddChapitredi(db, "29", "RESULTATS");
        AddArticledi(db, c29.Id, "290", "Déficit d'investissement reporté");

        #endregion
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
