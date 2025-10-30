using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Tel { get; set; }
        public required string Password { get; set; }

        // Relation avec la commune
        public int CommuneId { get; set; }
        public Commune Commune { get; set; } = null!;
    }

}
