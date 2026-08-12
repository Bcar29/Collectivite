using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    public enum StatusFact
    {
        impayee,
        payee,
        enCours
    }

    [Table("Factures")]
    public class Facture
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le numéro de facture est obligatoire")]
        [MaxLength(50)]
        public string NumeroFacture { get; set; } = null!;

        [Required(ErrorMessage = "La date de facture est obligatoire")]
        public DateTime DateFacture { get; set; }

        [Required(ErrorMessage = "Le montant HT est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant HT doit être positif")]
        public double MontantHT { get; set; }

        [Required(ErrorMessage = "Le taux de TVA est obligatoire")]
        [Range(0, 100, ErrorMessage = "Le taux TVA doit être compris entre 0 et 100")]
        public double TauxTVA { get; set; }

        [Required(ErrorMessage = "Le montant TTC est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant TTC doit être positif")]
        public double MontantTTC { get; set; }

        [Required(ErrorMessage = "La date d'échéance est obligatoire")]
        public DateTime DateEcheance { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        [MaxLength(500)]
        public string Description { get; set; } = null!;

        // 🔹 Relation : un tiers
        [Required]
        public int TiersId { get; set; }
        public Tiers Tiers { get; set; } = null!;

        // 🔹 Relation : un exercice
        [Required]
        public int ExerciceId { get; set; }
        public Exercice Exercice { get; set; } = null!;

        // 🔹 Statut de la facture
        [Required]
        public StatusFact Status { get; set; } = StatusFact.impayee;

        // 🔹 Fichier joint (optionnel)
        public byte[]? FichierJoin { get; set; }

        // 🔹 Engagements liés
        public ICollection<Engagement>? Engagements { get; set; } = new List<Engagement>();

        // 🔹 Détails de la facture
        public ICollection<DetailsFacture> Details { get; set; } = new List<DetailsFacture>();
    }

    
}
