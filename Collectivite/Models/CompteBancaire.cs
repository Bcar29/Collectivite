using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    [Index(nameof(IBAN), IsUnique = true)]
    [Index(nameof(BIC), IsUnique = true)]
    public class CompteBancaire
    {
        [Key]
        public int Id { get; set; }
        public int TiersId { get; set; }
        public Tiers Tiers { get; set; } = null!;
        [Required(ErrorMessage ="le numero de compte est obligatoire")]
        public string IBAN { get; set; } = null!;
        public string BIC { get; set; } = null!;
        [Required(ErrorMessage ="la banque est obligatoire")]
        public string Banque { get; set; } = null!;
        [Required(ErrorMessage = "le pays est obligatoire")]
        public string Pays { get; set; } = null!;
    }
}
