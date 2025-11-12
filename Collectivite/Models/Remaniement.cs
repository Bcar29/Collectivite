using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Collectivite.Models
{
    /// <summary>
    /// Types de remaniement budgétaire
    /// </summary>
    public enum TypeRemaniement
    {
        /// <summary>
        /// Remaniement en moins (diminue le budget)
        /// </summary>
        [Display(Name = "En moins")]
        en_moins = 0,

        /// <summary>
        /// Remaniement en plus (augmente le budget)
        /// </summary>
        [Display(Name = "En plus")]
        en_plus = 1
    }

    /// <summary>
    /// Représente un remaniement budgétaire
    /// </summary>
    [Table("Remaniements")]
    public class Remaniement
    {
        #region Propriétés principales

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public double Montant { get; set; }

        [Required(ErrorMessage = "Le motif est obligatoire")]
        [StringLength(500, ErrorMessage = "Le motif ne peut pas dépasser 500 caractères")]
        [Column(TypeName = "text")]
        public string Motif { get; set; } = null!;

        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Le type de remaniement est obligatoire")]
        public TypeRemaniement TypeRemaniement { get; set; }

        #endregion

        #region Clés étrangères

        [ForeignKey("BudgetLine")]
        [Required(ErrorMessage = "La ligne budgétaire est obligatoire")]
        public int IdBudgetLine { get; set; }

        #endregion

        #region Navigation

        /// <summary>
        /// Ligne budgétaire associée à ce remaniement
        /// </summary>
        public BudgetLine BudgetLine { get; set; } = null!;

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Indique si ce remaniement augmente le budget
        /// </summary>
        public bool IsAugmentation()
        {
            return TypeRemaniement == TypeRemaniement.en_plus;
        }

        /// <summary>
        /// Indique si ce remaniement diminue le budget
        /// </summary>
        public bool IsDiminution()
        {
            return TypeRemaniement == TypeRemaniement.en_moins;
        }

        /// <summary>
        /// Retourne le montant avec le signe approprié
        /// </summary>
        [NotMapped]
        public double MontantSigne
        {
            get
            {
                return TypeRemaniement == TypeRemaniement.en_plus ? Montant : -Montant;
            }
        }

        #endregion
    }
}