using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Collectivite.Models
{
    /// <summary>
    /// Représente un rôle applicatif (Maire, Secrétaire Général, Receveur, etc.).
    /// </summary>
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigations
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

