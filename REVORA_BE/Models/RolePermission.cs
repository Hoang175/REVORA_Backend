using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        public int RoleId { get; set; }

        public int PermissionId { get; set; }

        public Role Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;
    }
}
