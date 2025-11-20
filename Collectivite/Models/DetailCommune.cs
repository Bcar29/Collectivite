using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    public class DetailCommune
    {
        // Informations générales
        [Key]
        public int Id { get; set; }

        public int NombreConseillers { get; set; } = 0;
        public int NombreDelegationSpeciale { get; set; } = 0;

        // Effectif du personnel
        public int EffectifTotalPersonnel { get; set; } = 0;
        public int EffectifPermanent { get; set; } = 0;
        public int EffectifTemporaire { get; set; } = 0;

        // Données administratives
        public int NombreQuartiers { get; set; } = 0;
        public int NombreDistricts { get; set; } = 0;
        public int NombreSecteurs { get; set; } = 0;

        // Données démographiques
        public int PopulationTotale { get; set; } = 0;
        public int PopulationFemmes { get; set; } = 0;
        public int PopulationHommes { get; set; } = 0;

        [Required]
        public double Superficie { get; set; }

        public double Densite { get; set; } = 0;

        // Infrastructures sanitaires
        public int NombreCentresSante { get; set; } = 0;
        public int NombrePostesSante { get; set; } = 0;
        public int NombreSanteAmelioree { get; set; } = 0;

        // Éducation
        public int NombreEcoles { get; set; } = 0;
        public int NombreEcolesCollege { get; set; } = 0;
        public int NombreEcolesLycee { get; set; } = 0;
        public int NombreEcolesPrimaire { get; set; } = 0;
        public int NombreEcolesPrescolaire { get; set; } = 0;

        public int NombreClassesCollege { get; set; } = 0;
        public int NombreClassesLycee { get; set; } = 0;
        public int NombreClassesPrimaire { get; set; } = 0;
        public int NombreClassesPrescolaire { get; set; } = 0;

        public int NombreElevesCollege { get; set; } = 0;
        public int NombreElevesLycee { get; set; } = 0;
        public int NombreElevesPrimaire { get; set; } = 0;
        public int NombreElevesPrescolaire { get; set; } = 0;

        // Ressources et associations
        public int NombreForages { get; set; } = 0;
        public int NombreAssociation { get; set; } = 0;
        public int NombrePointsEau { get; set; } = 0;

        public int NombreOng { get; set; } = 0;
        public int NombreOngNationales { get; set; } = 0;
        public int NombreOngEtrangeres { get; set; } = 0;

        public int NombreGroupements { get; set; } = 0;
        public int NombreCooperatives { get; set; } = 0;

        // Sécurité et économie
        public int NombreDetenteursArmesFeu { get; set; } = 0;

        public int NombreMarches { get; set; } = 0;
        public int NombreMarchesJournaliers { get; set; } = 0;
        public int NombreMarchesHebdomadaires { get; set; } = 0;

        // Relations
        [ForeignKey("Commune")]
        [Required]
        public int IdCommune { get; set; }

        public Commune Commune { get; set; } = null!;
        public Exercice? Exercice { get; set; }
    }
}