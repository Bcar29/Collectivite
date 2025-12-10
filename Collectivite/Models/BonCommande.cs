using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Collectivite.Models
{
    public class BonCommande
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Numero { get; set; } = null!;
        public byte[] FichierJoin { get; set; } = null!;
        public int EngagementId { get; set; }
        public Engagement Engagement { get; set; } = null!;
        public DateTime DateCreation { get; set; }
        public ICollection<DetailBonCommande> Details { get; set; } = new List<DetailBonCommande>();
    }
}
