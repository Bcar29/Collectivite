using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    public class SecteurPDL
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = null!;

        public string? Description { get; set; }

        [ForeignKey("ProgrammePDL")]
        [Required]
        public int ProgrammePDLId { get; set; }
        public ProgrammePDL? ProgrammePDL { get; set; }

        // Navigation inverse
        public ICollection<ActivitePDL>? Activites { get; set; }
    }
}