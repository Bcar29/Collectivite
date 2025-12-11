using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Tel { get; set; }

        // ⚠️ IMPORTANT : Stocker uniquement le hash en base de données
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Propriété NON MAPPÉE pour la saisie du mot de passe
        [NotMapped]
        public string Password { get; set; } = string.Empty;

        public int CommuneId { get; set; }
        public Commune? Commune { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
