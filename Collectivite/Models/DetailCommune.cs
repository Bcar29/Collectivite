using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class DetailCommune
    {

        // Informations générales
        [Key]
        public int Id { get; set; }
        [Required]
        public int NombreConseillers { get; set; }
        [Required]
        public int NombreDelegationSpeciale { get; set; }

        // Effectif du personnel
        [Required]
        public int EffectifTotalPersonnel { get; set; }
        [Required]
        public int EffectifPermanent { get; set; }
        [Required]
        public int EffectifTemporaire { get; set; }


        // Données administratives
        [Required]
        public int NombreQuartiers { get; set; }
        [Required]
        public int NombreDistricts { get; set; }
        [Required]
        public int NombreSecteurs { get; set; }

        // Données démographiques
        [Required]
        public int PopulationTotale { get; set; }
        [Required]
        public int PopulationFemmes { get; set; }
        [Required]
        public int PopulationHommes { get; set; }
        [Required]
        public double Superficie { get; set; } // km²
        [Required]
        public double Densite { get; set; }    // hab/km²

        // Infrastructures
        [Required]
        public int NombreCentresSante { get; set; }
        [Required]
        public int NombreEcoles { get; set; }
        [Required]
        public int NombreEcolesPrimaires { get; set; }
        [Required]
        public int NombreEcolesSecondaires { get; set; }
        [Required]
        public int NombreClassesPrimaires { get; set; }
        [Required]
        public int NombreClassesSecondaires { get; set; }
        [Required]
        public int NombreElevesPrimaires { get; set; }
        [Required]
        public int NombreElevesSecondaires { get; set; }

        // Ressources et associations
        [Required]
        public int NombreForages { get; set; }
        [Required]
        public int NombreOng { get; set; }
        [Required]
        public int NombreOngNationales { get; set; }
        [Required]
        public int NombreOngEtrangeres { get; set; }
        [Required]
        public int NombreGroupements { get; set; }
        [Required]
        public int NombreCooperatives { get; set; }

        // Sécurité et économie
        [Required]
        public int NombreDetenteursArmesFeu { get; set; }
        [Required]
        public int NombreMarches { get; set; }
        [Required]
        public int NombreMarchesJournaliers { get; set; }
        [Required]
        public int NombreMarchesHebdomadaires { get; set; }

        [ForeignKey("Commune")]
        [Required]
        public int IdCommune { get; set; }

        public Commune Commune { get; set; } = null!;

        public Exercice? Exercice { get; set; }



    }
}
