using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    [Table("Exercices")]
    public class Exercice
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100)]
        public string? Libelle { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire")]
        public DateOnly DateDebut { get; set; }
        public DateOnly DateFin { get; set; }
        public bool EstCloture { get; set; }
        public BudgetPrimitif? BudgetPrimitif { get; set; }
        // ══════════════════════════════════════════════════
        // RELATION AVEC DETAILCOMMUNE (One-to-One)
        // ══════════════════════════════════════════════════
        [ForeignKey("DetailCommune")]
        //[Required]
        public int? IdDetailCommune { get; set; }

        // Propriété de navigation vers DetailCommune
        public DetailCommune? DetailCommune { get; set; } = null!;

        public ICollection<Contrats>? Contrats { get; set; }

        public ICollection<Engagement>? Engagements { get; set; }

        public ICollection<Recensement>? Recensements { get; set; }
        //public ICollection<Facture>? Factures { get; set; }



    }
}
