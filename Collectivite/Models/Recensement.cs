using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Recensement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La ligne budgétaire est obligatoire.")]
        public int BudgetLineId { get; set; }

        [ForeignKey(nameof(BudgetLineId))]
        public BudgetLine BudgetLine { get; set; } = null!; // uniquement pour les recettes fiscales

        [Required(ErrorMessage = "L'exercice est obligatoire.")]
        public int ExerciceId { get; set; }

        [ForeignKey(nameof(ExerciceId))]
        public Exercice Exercice { get; set; } = null!;

        [Required(ErrorMessage = "La commune est obligatoire.")]
        public int CommuneId { get; set; }

        [ForeignKey(nameof(CommuneId))]
        public Commune Commune { get; set; } = null!;

        [Required(ErrorMessage = "Le tiers est obligatoire.")]
        public int TiersId { get; set; }

        [ForeignKey(nameof(TiersId))]
        public Tiers Tiers { get; set; } = null!;

        [Required(ErrorMessage = "Le montant recensé est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant recensé doit être positif.")]
        public double MontantRecense { get; set; }
    }
}
