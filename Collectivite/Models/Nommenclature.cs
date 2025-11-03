using System;
using System.Collections.Generic;
using System.IO.Packaging;
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
    public class Nommenclature
    {
        public int Id { get; set; }
        public string? Chapitre { get; set; }
        public string? Article { get; set; }
        public string? Paragraphe { get; set; }
        public string? SousParagraphe { get; set; }
        public required string Intitule { get; set; }
        public required NatureType Nature { get; set; }
        public required SectionType Section { get; set; }

        // Auto reference 
        public int? ParentId { get; set; }
        public Nommenclature? Parent { get; set; }
        public List<Nommenclature>? Enfants { get; set; }
    }
}
