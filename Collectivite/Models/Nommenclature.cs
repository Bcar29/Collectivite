using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{

    public enum NatureType
    {
        Recette,
        Depense
    }

    public enum SectionType
    {
        Fonctionnement,
        Investissement,
    }

    [Table("Nommenclatures")]
    public class Nommenclature
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string? Chapitre { get; set; }

        [MaxLength(10)]
        public string? Article { get; set; }

        [MaxLength(10)]
        public string? Paragraphe { get; set; }

        [MaxLength(10)]
        public string? SousParagraphe { get; set; }

        [Required(ErrorMessage = "L'intitulé est obligatoire")]
        [MaxLength(200)]
        public  string? Intitule { get; set; }

        [Required]
        public  NatureType Nature { get; set; }

        [Required]
        public  SectionType Section { get; set; }

        // 🔹 Auto reference pour hiérarchie
        public int? ParentId { get; set; }
        public Nommenclature? Parent { get; set; }

        public List<Nommenclature>? Enfants { get; set; } = new List<Nommenclature>();
    }
}
