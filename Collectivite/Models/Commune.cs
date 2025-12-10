using Collectivite.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO.Packaging;


namespace Collectivite.Models
{
    public class Commune
    {
        public enum TypeCommune { URBAINE,RURALE}
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nom { get; set; } = null!;
        public string Region { get; set; }
        public string Prefecture { get; set; }
        public TypeCommune CommuneType { get; set; }= TypeCommune.URBAINE;

        // Distances (en km) - Non modifiables après création
        public double DistanceChefLieuProvince { get; set; }
        public double DistanceChefLieuRegion { get; set; }
        public double DistanceCapitale { get; set; }

        public DateOnly DateCreation { get; set; }

        // Relations
        public ICollection<DetailCommune>? DetailCommunes { get; set; } = new List<DetailCommune>();
        public ICollection<User>? Users { get; set; } = [];

        public ICollection<Engagement>? Engagements { get; set; }

        public ICollection<Recensement>? Recensements { get;set; }

        /// <summary>
        /// Retourne le nom de la commune ou une chaîne vide
        /// </summary>
        public string NomCommune
        {
            get
            {
                
                if (!string.IsNullOrWhiteSpace(Nom))
                    return Nom;
                else
                    return "";
            }
        }
        /// <summary>
        /// Retourne la région de la commune ou une chaîne vide
        /// </summary>
        public string RegionCommune
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Region))
                    return Region;
                else
                    return "";
            }
        }

        /// <summary>
        /// Retourne la préfecture de la commune ou une chaîne vide
        /// </summary>
        public string PrefectureCommune
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Prefecture))
                    return Prefecture;
                else
                    return "";
            }
        }

        /// <summary>
        /// Retourne le type de commune en chaîne
        /// </summary>
        public string TypCommune  // Correction: TypCommune au lieu de typCommune (convention C#)
        {
            get
            {
                return CommuneType switch
                {
                    TypeCommune.URBAINE => "URBAINE",
                    TypeCommune.RURALE => "RURALE",
                    _ => ""
                };
            }
        }


    }
}