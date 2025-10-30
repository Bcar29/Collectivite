using Collectivite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class BudgetLine
    {
        public int Id { get; set; }
        public int BudgetPrimitifId { get; set; }
        public BudgetPrimitif BudgetPrimitif { get; set; } = null!;
        public int NommenclatureId { get; set; }
        public Nommenclature Nommenclature { get; set; } = null!;
        public required int MontantPrevu { get; set; }
        public int MontantActu { get; set; }

    }
}
