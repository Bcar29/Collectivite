using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class Contrats
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string NumeroContrat { get; set; } = null!;
        public DateOnly DateSignature { get; set; }
        public DateOnly DateEcheance { get; set; }
        public int TiersId { get; set; }
        public Tiers Tiers { get; set; } = null!;

        [Required(ErrorMessage = " l'objet du contrat est obligatoire")]

        

        public string Objet { get; set; } = null!;

        [Required(ErrorMessage = "Le montant du contrat est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public double MontantContrat { get; set; }
        public byte[]? FichierJoin { get; set; }
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;

        public ICollection<Engagement>? Engagements { get; set; } = new List<Engagement>();

        public ICollection<Facture>? Factures { get; set; } = new List<Facture>();
    }
}
