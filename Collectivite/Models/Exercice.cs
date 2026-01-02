using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Collectivite.Models
{
    [Table("Exercices")]
    public class Exercice
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100)]
        public string? Libelle { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire")]
        public DateOnly DateDebut { get; set; }

        public DateOnly DateFin { get; set; }
        public bool EstCloture { get; set; }
        [ForeignKey("PDL")]
        public int? PDLId { get; set; }
        public PDL? PDL { get; set; }
        public BudgetPrimitif? BudgetPrimitif { get; set; }

        //[ForeignKey("DetailCommune")]
        //public int? IdDetailCommune { get; set; }

        public DetailCommune? DetailCommune { get; set; } = null!;

        public ICollection<Contrats>? Contrats { get; set; }
        public ICollection<Engagement>? Engagements { get; set; }
        public ICollection<Recensement>? Recensements { get; set; }


        // ⭐ MÉTHODE POUR EXTRAIRE L'ANNÉE DU LIBELLÉ ⭐
        public int? GetAnnee()
        {
            if (string.IsNullOrWhiteSpace(Libelle))
                return null;

            // Cherche un nombre dans le libellé
            var match = Regex.Match(Libelle, @"\d+");

            if (match.Success && int.TryParse(match.Value, out int annee))
                return annee;

            return null;
        }
        
    }
}
