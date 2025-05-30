using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Auditlog
{
    public int Id { get; set; }

    public Guid? Userid { get; set; }

    public string? Active { get; set; }

    public string? OldData { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? NewData { get; set; }

    public Guid? PerformeBy { get; set; }

    public virtual User? PerformeByNavigation { get; set; }

    public virtual User? User { get; set; }
}
