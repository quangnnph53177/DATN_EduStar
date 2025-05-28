using System;
using System.Collections.Generic;

namespace API.Models;

public partial class DayOfWeek
{
    public int Id { get; set; }

    public string? Weekdays { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
