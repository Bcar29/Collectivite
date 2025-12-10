using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class DetailExpressionBesoin
    {
        [Key]
        public int Id { get; set; }
        public int ExpressionBesoinId { get; set; }
        public ExpressionBesoin ExpressionBesoin { get; set; } = null!;
        public int NommenclatureId { get; set; }
        public Nommenclature Nommenclature { get; set; } = null!;
        public string Designation { get; set; } = null!;
        public int Quantite { get; set; }

    }
}
