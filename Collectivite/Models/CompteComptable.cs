using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class CompteComptable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le numéro du compte est obligatoire.")]
        [StringLength(20, ErrorMessage = "Le numéro de compte ne doit pas dépasser 20 caractères.")]
        [Column(TypeName = "varchar(20)")]
        public string NumeroCompte { get; set; } = null!;

        [Required(ErrorMessage = "L'intitulé du compte est obligatoire.")]
        [StringLength(255, ErrorMessage = "L'intitulé du compte ne doit pas dépasser 255 caractères.")]
        [Column(TypeName = "varchar(255)")]
        public string IntituleCompte { get; set; } = null!;

        // Relation réflexive
        public int? ContrePartieId { get; set; }

        [ForeignKey("ContrePartieId")]
        public virtual CompteComptable? ContrePartie { get; set; }

        public virtual ICollection<CompteComptable> SousComptes { get; set; } = new List<CompteComptable>();
    }
}
