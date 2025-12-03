using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Engagement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "L'exercice est obligatoire.")]
        public int ExerciceId { get; set; }

        [ForeignKey(nameof(ExerciceId))]
        public Exercice Exercice { get; set; } = null!;

        [Required(ErrorMessage = "La commune est obligatoire.")]
        public int CommuneId { get; set; }

        [ForeignKey(nameof(CommuneId))]
        public Commune Commune { get; set; } = null!;

        [Required(ErrorMessage = "La ligne budgétaire est obligatoire.")]
        public int BudgetLineId { get; set; }

        [ForeignKey(nameof(BudgetLineId))]
        public BudgetLine BudgetLine { get; set; } = null!;

        //[Required(ErrorMessage = "Le tiers est obligatoire.")]
        public int? TiersId { get; set; }

        [ForeignKey(nameof(TiersId))]
        public Tiers? Tiers { get; set; } = null!;

        [Required(ErrorMessage = "L'objet de l'engagement est obligatoire.")]
        [Column(TypeName = "text")]
        public string Objet { get; set; } = null!;

        [Required(ErrorMessage = "La date de l'engagement est obligatoire.")]
        [DataType(DataType.Date)]
        public DateTime DateEngagement { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Les crédits budgétaires sont obligatoires.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public decimal CreditsBudgetaires { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public decimal EngagementsAnterieurs { get; set; }

        [Required(ErrorMessage = "Le montant de l'engagement est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public decimal MontantEngagement { get; set; }

        public string MontantLettre { get; set; } = null!;
        public byte[]? FichierJoin { get; set; }
        public string? FichierName { get; set; }
        public int? ContratId { get; set; }
        [ForeignKey(nameof(ContratId))]
        public Contrats? Contrat { get; set; }

        public int? FactureId { get; set; }
        [ForeignKey(nameof(FactureId))]
        public Facture? Facture { get; set; }

        public Mandat? Mandat { get; set; }

        [NotMapped]
        public decimal CumulEngagement => EngagementsAnterieurs + MontantEngagement;

        // Constructeur par défaut
        public Engagement()
        {
            DateEngagement = DateTime.Now;
        }

        public ICollection<BonCommande>? BonCommandes { get; set; } = new List<BonCommande>();
    }
}
