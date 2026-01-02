using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class PDL
    {
        public int Id { get; set; }
        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }
        public string? Description { get; set; }
        public string? FicName { get; set; }
        public byte[]? FickierJoin { get; set; }
        public ICollection<ActivitePDL>? Activites { get; set; }
        public ICollection<Exercice>? Exercices { get; set; }
    }
}
