using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collectivite.Models
{
    /// <summary>
    /// Type de document
    /// </summary>
    public enum TypeDocument
    {
        [Display(Name = "CNI / Carte d'Identité")]
        CarteIdentite,

        [Display(Name = "Passeport")]
        Passeport,

        [Display(Name = "RCCM")]
        RCCM,

        [Display(Name = "NIF")]
        NIF,

        [Display(Name = "Quitus Fiscal")]
        QuitusFiscal,

        [Display(Name = "Attestation TVA")]
        AttestationTVA,

        [Display(Name = "Contrat de travail")]
        ContratTravail,

        [Display(Name = "Autre")]
        Autre
    }

    /// <summary>
    /// Document attaché à un tiers (CNI, RCCM, NIF, Quitus, etc.)
    /// </summary>
    public class DocumentTiers
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Tiers")]
        public int TiersId { get; set; }

        [Required]
        public TypeDocument Type { get; set; }

        /// <summary>
        /// Numéro du document (ex: numéro CNI, numéro RCCM, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? NumeroDocument { get; set; }

        [Required]
        [MaxLength(255)]
        public string NomFichier { get; set; } = null!;

        /// <summary>
        /// Chemin du fichier sur le serveur ou dans le système
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string CheminFichier { get; set; } = null!;

        /// <summary>
        /// Extension du fichier (.pdf, .jpg, .png, etc.)
        /// </summary>
        [MaxLength(10)]
        public string? Extension { get; set; }

        /// <summary>
        /// Taille du fichier en octets
        /// </summary>
        public long TailleFichier { get; set; }

        public DateTime DateAjout { get; set; } = DateTime.Now;

        /// <summary>
        /// Date d'expiration du document (si applicable)
        /// </summary>
        public DateTime? DateExpiration { get; set; }

        /// <summary>
        /// Date d'émission du document
        /// </summary>
        public DateTime? DateEmission { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Indique si le document est valide/vérifié
        /// </summary>
        public bool IsValide { get; set; } = true;

        /// <summary>
        /// Indique si le document est obligatoire pour ce type de tiers
        /// </summary>
        [NotMapped]
        public bool IsObligatoire { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELATIONS
        // ═══════════════════════════════════════════════════════════

        public Tiers Tiers { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS CALCULÉES
        // ═══════════════════════════════════════════════════════════

        [NotMapped]
        public string TypeDisplay => Type switch
        {
            TypeDocument.CarteIdentite => "CNI",
            TypeDocument.Passeport => "Passeport",
            TypeDocument.RCCM => "RCCM",
            TypeDocument.NIF => "NIF",
            TypeDocument.QuitusFiscal => "Quitus Fiscal",
            TypeDocument.AttestationTVA => "Attestation TVA",
            TypeDocument.ContratTravail => "Contrat de travail",
            TypeDocument.Autre => "Autre",
            _ => "Inconnu"
        };

        [NotMapped]
        public string TailleFichierFormatee
        {
            get
            {
                if (TailleFichier < 1024)
                    return $"{TailleFichier} octets";
                else if (TailleFichier < 1024 * 1024)
                    return $"{TailleFichier / 1024:F2} Ko";
                else
                    return $"{TailleFichier / (1024 * 1024):F2} Mo";
            }
        }

        [NotMapped]
        public bool EstExpire => DateExpiration.HasValue && DateExpiration.Value < DateTime.Now;

        [NotMapped]
        public bool ExpireBientot => DateExpiration.HasValue &&
                                      DateExpiration.Value > DateTime.Now &&
                                      DateExpiration.Value <= DateTime.Now.AddDays(30);

        [NotMapped]
        public string StatutExpiration
        {
            get
            {
                if (!DateExpiration.HasValue)
                    return "Non applicable";

                if (EstExpire)
                    return "Expiré";

                if (ExpireBientot)
                    return "Expire bientôt";

                return "Valide";
            }
        }
    }
}