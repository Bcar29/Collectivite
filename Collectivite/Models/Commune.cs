using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Commune
    {
        public int Id { get; set; }
        public required string Nom { get; set; }

          // Informations générales
        public DateTime DateCreation { get; set; }
        public int NombreConseillers { get; set; }
        public int NombreDelegationSpeciale { get; set; }

        // Effectif du personnel
        public int EffectifTotalPersonnel { get; set; }
        public int EffectifPermanent { get; set; }
        public int EffectifTemporaire { get; set; }

        // Distances (en km)
        public double DistanceChefLieuProvince { get; set; }
        public double DistanceChefLieuRegion { get; set; }
        public double DistanceCapitale { get; set; }

        // Données administratives
        public int NombreQuartiers { get; set; }
        public int NombreDistricts { get; set; }
        public int NombreSecteurs { get; set; }

        // Données démographiques
        public int PopulationTotale { get; set; }
        public int PopulationFemmes { get; set; }
        public int PopulationHommes { get; set; }
        public double Superficie { get; set; } // en km²
        public double Densite { get; set; }    // hab/km²

        // Infrastructures
        public int NombreCentresSante { get; set; }
        public int NombreEcoles { get; set; }
        public int NombreEcolesPrimaires { get; set; }
        public int NombreEcolesSecondaires { get; set; }
        public int NombreClassesPrimaires { get; set; }
        public int NombreClassesSecondaires { get; set; }
        public int NombreElevesPrimaires { get; set; }
        public int NombreElevesSecondaires { get; set; }

        // Ressources et associations
        public int NombreForages { get; set; }
        public int NombreOng { get; set; }
        public int NombreOngNationales { get; set; }
        public int NombreOngEtrangeres { get; set; }
        public int NombreGroupements { get; set; }
        public int NombreCooperatives { get; set; }

        // Sécurité et économie
        public int NombreDetenteursArmesFeu { get; set; }
        public int NombreMarches { get; set; }
        public int NombreMarchesJournaliers { get; set; }
        public int NombreMarchesHebdomadaires { get; set; }

        // Relations
        public ICollection<BudgetPrimitif>? BudgetsPrimitifs { get; set; } = new List<BudgetPrimitif>();
        public ICollection<User>? Users { get; set; } = new List<User>();
    }
}
