using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required(ErrorMessage = "La date de fin est obligatoire")]
        public DateOnly DateFin { get; set; }

        [Required]
        public bool EstCloture { get; set; }

        // 🔹 Relation : Un exercice a un seul budget primitif
        public BudgetPrimitif? BudgetPrimitif { get; set; }
       
    }
}
