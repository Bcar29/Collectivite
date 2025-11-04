using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Commune
    {
        public int Id { get; set; }
        public required string Nom { get; set; }
        public ICollection<BudgetPrimitif>? BudgetsPrimitifs { get; set; } = new List<BudgetPrimitif>();
        public ICollection<User>? Users { get; set; } = new List<User>();
        public ICollection<Exercice>? Exercices { get; set; } = new List<Exercice>();
    }
}
