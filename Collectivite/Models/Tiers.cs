using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public enum TiersType
    {
        Fournisseur,
        Entreprise,
        Redevable,
        Contribuable,
        Association
    }
    public class Tiers
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "veuillez selectionner le type de tiers")]
        public TiersType Type { get; set; }
        public String? Rccm { get; set; } = null!;
        public String Nom { get; set; } = null!;
        public String? Prenom { get; set; }
        public String Adresse { get; set; } = null!;
        public String? Nif { get; set; }

        [Required(ErrorMessage = "L'adresse email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]  
        [MaxLength(255)]
        public string Email { get; set; } = null!;
        public bool IsActif { get; set; } = true;

        public ICollection<CompteBancaire>? CompteBancaires { get; set; }

        public ICollection<Contrats>? Contrats { get; set; }

        public ICollection<Engagement>? Engagements { get; set; }

        public ICollection<Facture>? Factures { get; set; }
        public ICollection<Recensement>? Recensements { get; set; }



    }
}
