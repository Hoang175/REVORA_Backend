using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Permissions")]
    public class Permission
    {
        public int PermissionId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
    }
}
