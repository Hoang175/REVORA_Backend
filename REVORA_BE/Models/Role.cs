using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Roles")]
    public class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = null!;

        public ICollection<User> Users { get; set; } = new HashSet<User>();

        public ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
    }
}
