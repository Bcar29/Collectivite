using System;

namespace Collectivite.Models
{
    /// <summary>
    /// Table de jonction Role ↔ Permission
    /// </summary>
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}

