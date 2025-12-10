using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class ExpressionBesoin
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string Numero { get; set; } = null!;
        public DateTime DateCreation { get; set; }
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;
        public ICollection<DetailExpressionBesoin> Details { get; set; } = new List<DetailExpressionBesoin>();

    }
}
