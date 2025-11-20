using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class BudgetPrimitif
    {
        public enum Statusbudget
        {
           
            APPROVED,
            VALIDATED
        }

        [Key]
        public int Id {  get; set; }

        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;
        [Required(ErrorMessage = "le montant du budget est obligatoire")]
        public int MontantTotal { get; set; } = 0;
        public int MontantDepense { get; set; } = 0;
        public int MontantRecette { get; set; } = 0;
        public DateOnly DateApprobation { get; set; }
        public DateOnly? DateValidation { get; set; }
        public Statusbudget Status { get; set; } = Statusbudget.APPROVED;
        public ICollection<BudgetLine>? BudgetLines { get; set; } = new List<BudgetLine>();

    }
}
