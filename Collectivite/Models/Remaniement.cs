using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public enum TypeRemaniement
    {
        en_mois, en_plus

    }
    [Table("Remaniements")]
    public class Remaniement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="le montant est obligatoire")]
        public double Montant { get; set; }

        public int IdBudgetLine { get; set; }
        public BudgetLine BudgetLine { get; set; } = null!;

        [Required(ErrorMessage = "le motif est obligatoire")]
        [Column(TypeName = "text")]
        public string Motif { get; set; } = null!;

        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Le type de remaniement est obligatoire")]
        public TypeRemaniement TypeRemaniement { get; set; }
        
    }
}
