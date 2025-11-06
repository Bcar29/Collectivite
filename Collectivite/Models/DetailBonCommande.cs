using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class DetailBonCommande
    {
        [Key]
        public int Id { get; set; }
        public int BonCommandeId { get; set; }
        public BonCommande BonCommande { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Quantite { get; set; }
        public double PrixUnitaire { get; set; }
        public double Total => Quantite * PrixUnitaire;
    }
}
