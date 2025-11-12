using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class OrdreRecette
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le numéro de l'ordre de recette est obligatoire")]
        [MaxLength(50, ErrorMessage = "Le numéro d'ordre ne doit pas dépasser 50 caractères")]
        public string NumeroOrdre { get; set; } = null!;

        // 🔹 Relation avec la ligne budgétaire
        [ForeignKey("BudgetLine")]
        [Required(ErrorMessage = "La ligne budgétaire est obligatoire")]
        public int BudgetLineId { get; set; }
        public BudgetLine BudgetLine { get; set; } = null!;

        // 🔹 Relation avec l'exercice
        [ForeignKey("Exercice")]
        [Required(ErrorMessage = "L'exercice est obligatoire")]
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;

        // 🔹 Relation avec la commune
        [ForeignKey("Commune")]
        [Required(ErrorMessage = "La commune est obligatoire")]
        public int CommuneId { get; set; }
        public Commune Commune { get; set; } = null!;

        // 🔹 Comptable
        
        [Required(ErrorMessage = "Le nom du comptable est obligatoire")]
        public string? Comptable { get; set; } = null!;

        // 🔹 Relation avec le tiers
        [ForeignKey("Tiers")]
        //[Required(ErrorMessage = "Le tiers est obligatoire")]
        public int? TiersId { get; set; }
        public Tiers? Tiers { get; set; } = null!;

        [Column(TypeName = "text")]
        public string? Motifs { get; set; }

        // 🔹 Montant de l’ordre
        [Required(ErrorMessage = "Le montant de l'ordre est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif")]
        public double MontantOrdre { get; set; }

        // 🔹 Montant en lettres
        [Required(ErrorMessage = "Le montant en lettres est obligatoire")]
        [MaxLength(255, ErrorMessage = "Le montant en lettres ne doit pas dépasser 255 caractères")]
        public string MontantOrdreLettre { get; set; } = null!;

        // 🔹 Date d’émission
        [Required(ErrorMessage = "La date de l'ordre est obligatoire")]
        public DateTime DateOrdre { get; set; } = DateTime.Now;

        public ICollection<EcritureComptable>? EcritureComptables { get; set; } = new List<EcritureComptable>();

        public override string ToString()
        {
            return $"OrdreRecette(Id={Id}, NumeroOrdre={NumeroOrdre}, MontantOrdre={MontantOrdre}, DateOrdre={DateOrdre.ToShortDateString()})";
        }
    }

}
