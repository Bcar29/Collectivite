using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Contrats
    {
        [Key]
        public int Id { get; set; }
        public string NumeroContrat { get; set; } = null!;
        public DateTime DateSignature { get; set; }
        public DateTime DateEcheance { get; set; }
        public int TiersId { get; set; }
        public Tiers Tiers { get; set; } = null!;
        [Required(ErrorMessage = " l'objet de la depense est obligatoire")]
        public string Objet { get; set; } = null!;

        [Required(ErrorMessage = "Le montant du contrat est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public double MontantContrat { get; set; }
        public byte[]? FichierJoin { get; set; }
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;

        public ICollection<Engagement>? Engagements { get; set; }

        public ICollection<Facture>? Factures { get; set; }
    }
}
