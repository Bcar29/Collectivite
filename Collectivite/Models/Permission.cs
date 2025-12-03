using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Collectivite.Models
{
    /// <summary>
    /// Représente une permission atomique (ex : Budget.Validate).
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(150)]
        public required string Code { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

