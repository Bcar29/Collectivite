using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    [Table("DetailsFactures")]
    public class DetailsFacture
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La facture est obligatoire")]
        public int FactureId { get; set; }
        public Facture Facture { get; set; } = null!;

        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(200)]
        public string Libelle { get; set; } = null!;

        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "La quantité doit être positive")]
        public double Quantite { get; set; }

        [Required(ErrorMessage = "Le prix unitaire est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix unitaire doit être positif")]
        public double PrixUnitaire { get; set; }

        [Required(ErrorMessage = "Le montant total est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant total doit être positif")]
        public double MontantTotal { get; set; }
    }
}
