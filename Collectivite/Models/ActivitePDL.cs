using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    public class ActivitePDL
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Description { get; set; } = null!;

        public string? Resultat { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public string FinancementInterne { get; set; } = null!;
        public string FinancementExterne { get; set; } = null!;

        // ════════════════════════════════════════════════════════
        // Relations Many-to-One (N ↔ 1)
        // ════════════════════════════════════════════════════════
        [ForeignKey("PDL")]
        [Required]
        public int PDLId { get; set; }
        public PDL? PDL { get; set; }
        [ForeignKey("SecteurPDL")]
        [Required]
        public int SecteurPDLId { get; set; }
        public SecteurPDL? SecteurPDL { get; set; }

        [ForeignKey("CompetenceCollectivite")]
        [Required]
        public int CompetenceCollectiviteId { get; set; }
        public CompetenceCollectivite? CompetenceCollectivite { get; set; }

        [ForeignKey("ODD")]
        [Required]
        public int ODDId { get; set; }
        public ODD? ODD { get; set; }

        // ════════════════════════════════════════════════════════
        // Relations Many-to-Many (N ↔ N) - APPROCHE AUTOMATIQUE
        // ════════════════════════════════════════════════════════
        public ICollection<BeneficiairePDL>? Beneficiaires { get; set; }
        public ICollection<ActeurPDL>? Acteurs { get; set; }
        public ICollection<StructureExecutionPDL>? StructureExecutions { get; set; }
    }
}