using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Permisson
{
    public int Id { get; set; }

    public string? PermissonName { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
