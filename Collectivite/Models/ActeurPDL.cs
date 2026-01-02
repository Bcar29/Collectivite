using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Collectivite.Models
{
    public class ActeurPDL
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nom { get; set; } = null!;

        public string? Description { get; set; }

        // Navigation inverse
        public ICollection<ActivitePDL>? Activites { get; set; }
    }
}