using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
     public class Mouvement
    {
        [Key]
        public int id {  get; set; }

        public DateOnly Date {  get; set; }
        public decimal Montant { get; set; }
        public byte[]? FichierJoint { get; set; }
        public string? FileName { get; set; }
        public string? RefVirement { get; set; }
        public string? NumBanqueBenef {  get; set; }
        public string? RefChèque { get; set; }
        public int idCompteComptable { get; set; }
        public CompteComptable CompteComptable { get; set; } = null!;
        public int? idOrdreRecette { get; set; }
        [ForeignKey(nameof(idOrdreRecette))]
        public OrdreRecette? OrdreRecette { get; set; }
        public int? idMandat {  get; set; }
        [ForeignKey(nameof(idMandat))]
        public Mandat? Mandat { get; set; } 

        public int? idExercice { get; set; }
        [ForeignKey(nameof(idExercice))]
        public Exercice? Exercice { get; set; }

    }
}
