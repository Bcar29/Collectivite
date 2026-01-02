using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Collectivite.Models
{
    public class CompetenceCollectivite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Numero { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        // Navigation inverse
        public ICollection<ActivitePDL>? Activites { get; set; }
    }
}