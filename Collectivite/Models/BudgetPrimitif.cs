using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class BudgetPrimitif
    {
        public int Id {  get; set; }
        public int CommuneId { get; set; }
        public Commune Commune { get; set; } = null!;
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;
        public required int Montant { get; set; }
        public DateOnly DateVote { get; set; }
        public ICollection<BudgetLine>? BudgetLines { get; set; } = new List<BudgetLine>();

    }
}
