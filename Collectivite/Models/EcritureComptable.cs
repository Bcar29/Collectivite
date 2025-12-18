using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class EcritureComptable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La date de l'écriture est obligatoire.")]
        public DateOnly DateEcriture { get; set; }

        [Required(ErrorMessage = "Le compte de débit est obligatoire.")]
        public int CompteDebitId { get; set; }

        [ForeignKey(nameof(CompteDebitId))]
        public CompteComptable CompteDebit { get; set; } = null!;

        [Required(ErrorMessage = "Le compte de crédit est obligatoire.")]
        public int CompteCreditId { get; set; }

        [ForeignKey(nameof(CompteCreditId))]
        public CompteComptable CompteCredit { get; set; } = null!;

        [Required(ErrorMessage = "Le montant de l'écriture est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à zéro.")]
        public decimal Montant { get; set; }

        // L’écriture peut provenir d’un ordre de recette ou d’un mandat (dépense)
        public int? OrdreRecetteId { get; set; }

        [ForeignKey(nameof(OrdreRecetteId))]
        public OrdreRecette? OrdreRecette { get; set; }

        public int? MandatId { get; set; }

        [ForeignKey(nameof(MandatId))]
        public Mandat? Mandat { get; set; }

        // L’écriture peut être liée à un mouvement
        public int? MouvementId { get; set; }
        [ForeignKey(nameof(MouvementId))]
        public Mouvement? Mouvement { get; set; }

        public int? idExercice { get; set; }
        [ForeignKey(nameof(idExercice))]
        public Exercice? Exercice { get; set; }

    }
}
