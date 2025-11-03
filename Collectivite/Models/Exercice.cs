using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Exercice
    {
        public int Id { get; set; }
        public required string Libelle { get; set; }
        public DateOnly DateDebut { get; set; }
        public DateOnly dateFin { get; set; }
        public bool EstCloture { get; set; }
        // ══════════════════════════════════════════════════
        // RELATION AVEC DETAILCOMMUNE (One-to-One)
        // ══════════════════════════════════════════════════
        [ForeignKey("DetailCommune")]
        [Required]
        public int IdDetailCommune { get; set; }

        // Propriété de navigation vers DetailCommune
        public DetailCommune DetailCommune { get; set; } = null!;

        public ICollection<BudgetPrimitif>? BudgetsPrimitifs { get; set; } = new List<BudgetPrimitif>();
    }
}
