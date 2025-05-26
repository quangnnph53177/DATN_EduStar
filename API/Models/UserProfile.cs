using System;
using System.Collections.Generic;

namespace API.Models;

public partial class UserProfile
{
    public Guid UserId { get; set; }

    public string? FullName { get; set; }

    public bool? Gender { get; set; }

    public string? Avatar { get; set; }

    public string? Address { get; set; }

    public DateOnly? Dob { get; set; }

    public virtual User User { get; set; } = null!;
}
