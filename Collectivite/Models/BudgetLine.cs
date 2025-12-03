using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Collectivite.Models
{

    
    
    /// <summary>
    /// Représente une ligne budgétaire avec ses remaniements
    /// </summary>
    [Table("BudgetLines")]
    public class BudgetLine
    {
        #region Propriétés principales

        [Key]
        public int Id { get; set; }

        #endregion

        #region Clés étrangères

        [ForeignKey("BudgetPrimitif")]
        [Required(ErrorMessage = "Le budget primitif est obligatoire")]
        public int BudgetPrimitifId { get; set; }

        [ForeignKey("Nommenclature")]
        [Required(ErrorMessage = "La nomenclature est obligatoire")]
        public int NommenclatureId { get; set; }

        #endregion

        #region Propriétés de montants

        /// <summary>
        /// Montant prévu initialement dans le budget
        /// </summary>
        [Required(ErrorMessage = "Le montant prévu est obligatoire")]
        [Range(0, int.MaxValue, ErrorMessage = "Le montant prévu doit être un nombre positif")]
        public required decimal MontantPrevu { get; set; }

        /// <summary>
        /// Montant actuel (peut être modifié par les remaniements)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Le montant actuel doit être un nombre positif")]
        public decimal MontantActu { get; set; }
        public decimal MontantRealise { get; set; }
        public decimal MontantEntreSortie { get; set; }// montant recouvrement ou paiement 

        #endregion 

        #region Navigation

        /// <summary>
        /// Budget primitif auquel appartient cette ligne
        /// </summary>
        public BudgetPrimitif BudgetPrimitif { get; set; } = null!;

        /// <summary>
        /// Nomenclature associée à cette ligne budgétaire
        /// </summary>
        public Nommenclature Nommenclature { get; set; } = null!;

        /// <summary>
        /// Liste des remaniements appliqués à cette ligne
        /// </summary>
        public ICollection<Remaniement> Remaniements { get; set; } = new List<Remaniement>();

        #endregion

        #region Propriétés calculées (NotMapped)

        /// <summary>
        /// Calcule le montant définitif : MontantPrevu + RemaniementPlus - RemaniementMoins
        /// </summary>
        [NotMapped]
        public decimal MontantDefinitif
        {
            get
            {
                if (Remaniements == null || !Remaniements.Any())
                    return MontantPrevu;

                var remaniementPlus = Remaniements
                    .Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                    .Sum(r => (decimal)r.Montant);

                var remaniementMoins = Remaniements
                    .Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                    .Sum(r => (decimal)r.Montant);

                return MontantPrevu + remaniementPlus - remaniementMoins;
            }
        }

        /// <summary>
        /// Total des remaniements positifs (en plus)
        /// </summary>
        [NotMapped]
        public decimal TotalRemaniementPlus
        {
            get
            {
                if (Remaniements == null || !Remaniements.Any())
                    return 0;

                return Remaniements
                    .Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                    .Sum(r => (decimal)r.Montant);
            }
        }

        /// <summary>
        /// Total des remaniements négatifs (en moins)
        /// </summary>
        [NotMapped]
        public decimal TotalRemaniementMoins
        {
            get
            {
                if (Remaniements == null || !Remaniements.Any())
                    return 0;

                return Remaniements
                    .Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                    .Sum(r => (decimal)r.Montant);
            }
        }

        /// <summary>
        /// Variation totale (RemaniementPlus - RemaniementMoins)
        /// </summary>
        [NotMapped]
        public decimal VariationTotale
        {
            get
            {
                return TotalRemaniementPlus - TotalRemaniementMoins;
            }
        }

        /// <summary>
        /// Pourcentage de variation par rapport au montant prévu
        /// </summary>
        [NotMapped]
        public decimal PourcentageVariation
        {
            get
            {
                if (MontantPrevu == 0)
                    return 0;

                return (VariationTotale / MontantPrevu) * 100;
            }
        }

        [NotMapped]
        public decimal TauxRealisation
        {
            get
            {
                if (MontantDefinitif > 0)
                    return (MontantRealise/MontantDefinitif)*100;
                return 0;
            }
        }

        [NotMapped]
        public decimal TauxEntreSortie
        {
            get
            {
                if (MontantRealise > 0 )
                    return (MontantEntreSortie/MontantRealise)*100;
                return 0;
            }
        }

        [NotMapped]
        public decimal ResteRealise
        {
            get
            {
                return MontantDefinitif - MontantRealise;
            }
        }
        [NotMapped]
        public decimal ResteEntreSortie
        {
            get
            {
                return  MontantRealise - MontantEntreSortie;
            }
        }

        #endregion

        #region Constructeurs

        /// <summary>
        /// Constructeur par défaut
        /// </summary>

        public ICollection<Engagement>? Engagements { get; set; }
        public ICollection<Recensement>? Recensements { get; set; }

        // 🔹 Constructeur par défaut

        public BudgetLine()
        {
            // L'initialisation se fera après l'affectation de MontantPrevu
        }

        #endregion

        #region Méthodes

        /// <summary>
        /// Met à jour le montant actuel en fonction des remaniements
        /// </summary>
        public void UpdateMontantActu()
        {
            MontantActu = (int)MontantDefinitif;
        }

        /// <summary>
        /// Vérifie si cette ligne budgétaire a des remaniements
        /// </summary>
        public bool HasRemaniements()
        {
            return Remaniements != null && Remaniements.Any();
        }

        /// <summary>
        /// Obtient le nombre total de remaniements
        /// </summary>
        public int GetRemaniementCount()
        {
            return Remaniements?.Count ?? 0;
        }

        //public decimal MontantRealise(BudgetLine budgetLine)
        //{
        //    if (budgetLine.Nommenclature.Enfants != null)
        //    {
        //        foreach (var item in budgetLine.Nommenclature.Enfants)
        //        {
        //            if (item == null) continue;
        //            BudgetLine bl = 
        //            MontantRealise(item.);
        //        }
        //    }
        //    else
        //    {

        //    }
        //}

        #endregion
    }
}