using System;
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

        public DateTime DateCreation { get; set; }

        // Relation avec ExpressionBesoin
        public int ExpressionBesoinId { get; set; }
        public ExpressionBesoin ExpressionBesoin { get; set; } = null!;

        // Relation avec Engagements (1 bon → N engagements)
        public ICollection<Engagement> Engagements { get; set; } = new List<Engagement>();

        // Relation avec Détails
        public ICollection<DetailBonCommande> Details { get; set; } = new List<DetailBonCommande>();
    }
}