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

        [Key]
        public int Id {  get; set; }

        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;
        [Required(ErrorMessage = "le montant du budget est obligatoire")]
        public int Montant { get; set; }
        public DateOnly DateVote { get; set; }
        public ICollection<BudgetLine>? BudgetLines { get; set; } = new List<BudgetLine>();

    }
}
