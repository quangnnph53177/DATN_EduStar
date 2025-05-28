using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Permission
{
    public int Id { get; set; }

    public string? PermissionName { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
