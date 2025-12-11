using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Collectivite.Models.Commune;

namespace Collectivite.Models
{
    public enum TypeMois
    {
        Janvier,
        Fevrier,
        Mars,
        Avril,
        Mai,
        Juin,
        Juillet,
        Aout,
        Septembre,
        Octobre,
        Novembre,
        Decembre

    }
    public class Mandat
    {
        public enum StatutMandat { Non_Payé,Partiel,Payé}
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le numéro du mandat est obligatoire")]
        [MaxLength(50, ErrorMessage = "Le numéro du mandat ne doit pas dépasser 50 caractères")]
        public string NumeroMandat { get; set; } = null!;

        [MaxLength(50, ErrorMessage = "Le numéro du bordereau ne doit pas dépasser 50 caractères")]
        public string? Bordereau { get; set; }

        [Required(ErrorMessage = "Le mois du mandat est obligatoire")]
        public TypeMois Mois { get; set; }

        // 🔹 Relation avec Engagement
        [ForeignKey("Engagement")]
        [Required(ErrorMessage = "L'engagement est obligatoire")]
        public int EngagementId { get; set; }
        public Engagement Engagement { get; set; } = null!;

        [Required(ErrorMessage = "Le montant brut est obligatoire")]
        //[Range(0, double.MaxValue, ErrorMessage = "Le montant brut doit être positif")]
        public decimal MontantBrut { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "La valeur de la RTS doit être positive")]
        public decimal Rts { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "Les autres précomptes doivent être positifs")]
        public decimal AutresPrecomptes { get; set; }

        [Required(ErrorMessage = "Le montant net est obligatoire")]
        //[Range(0, double.MaxValue, ErrorMessage = "Le montant net doit être positif")]
        public decimal MontantNet { get; set; }

        [Required(ErrorMessage = "Le montant en lettres est obligatoire")]
        [MaxLength(255, ErrorMessage = "Le montant en lettres ne doit pas dépasser 255 caractères")]
        public string MontantLettre { get; set; } = null!; // "Arrêté à la somme de : ..."

        [Required(ErrorMessage = "La date d'émission est obligatoire")]
        public DateTime DateEmission { get; set; }

        [Required(ErrorMessage = "L'objet du mandat est obligatoire")]
        [MaxLength(255, ErrorMessage = "L'objet ne doit pas dépasser 255 caractères")]
        public string Objet { get; set; } = null!;

        // 🔹 Fichier joint (facultatif)
        public byte[]? FichierJoin { get; set; }
        public sbyte? FichierName { get; set; }

        // 🔹 Date de paiement (facultatif)
        public DateTime? DatePaiement { get; set; }

        public StatutMandat status { get; set; } = StatutMandat.Non_Payé;
        public ICollection<EcritureComptable>? EcritureComptables { get; set; } = new List<EcritureComptable>();

        // Methode qui recupere le status du mandat

        public string MandatStatut  
        {
            get
            {
                return status switch
                {
                    StatutMandat.Non_Payé => "Non Payé",
                    StatutMandat.Partiel => "Partiel",
                    StatutMandat.Payé => "Payé",
                    _ => ""
                };
            }
        }
    }
}
