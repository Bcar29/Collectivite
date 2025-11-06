using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Packaging;


namespace Collectivite.Models
{
    public class Commune
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? Nom { get; set; }

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
    }
}