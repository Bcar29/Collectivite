using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class BonCommande
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Numero { get; set; } = null!;
        public byte[] FichierJoin { get; set; } = null!;
        public DateTime DateCreation { get; set; }
        public ICollection<DetailBonCommande> Details { get; set; } = new List<DetailBonCommande>();
    }
}
