using Collectivite.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    [Table("BudgetLines")]
    public class BudgetLine
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("BudgetPrimitif")]
        [Required(ErrorMessage = "Le budget primitif est obligatoire")]
        public int BudgetPrimitifId { get; set; }

        public BudgetPrimitif BudgetPrimitif { get; set; } = null!;

        [ForeignKey("Nommenclature")]
        [Required(ErrorMessage = "La nomenclature est obligatoire")]
        public int NommenclatureId { get; set; }

        public Nommenclature Nommenclature { get; set; } = null!;

        [Required(ErrorMessage = "Le montant prévu est obligatoire")]
        [Range(0, int.MaxValue, ErrorMessage = "Le montant prévu doit être un nombre positif")]
        public required int MontantPrevu { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Le montant actuel doit être un nombre positif")]
        public int MontantActu { get; set; }

        // 🔹 Constructeur par défaut
        public BudgetLine()
        {
            MontantActu = MontantPrevu;
        }
    }
}
