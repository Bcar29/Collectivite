using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class ProgrammePDL
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<SecteurPDL>? SecteursPDL { get; set; }
    }
}
