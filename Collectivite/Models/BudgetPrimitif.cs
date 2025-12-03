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
            DRAFT,
            APPROVED,
            VALIDATED
        }

        [Key]
        public int Id {  get; set; }

        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;
        [Required(ErrorMessage = "le montant du budget est obligatoire")]
        public decimal MontantTotal { get; set; } = 0;
        public decimal MontantDepense { get; set; } = 0;
        public decimal MontantRecette { get; set; } = 0;
        public DateOnly? DateApprobation { get; set; }
        public DateOnly? DateValidation { get; set; }

        // Fichier ajouté
        public byte[]? FichierValidation { get; set; }
        public string? FileName { get; set; }
        public Statusbudget Status { get; set; } = Statusbudget.DRAFT;
        public ICollection<BudgetLine>? BudgetLines { get; set; } = new List<BudgetLine>();

    }
}
