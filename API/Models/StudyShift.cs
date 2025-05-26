using System;
using System.Collections.Generic;

namespace API.Models;

public partial class StudyShift
{
    public int Id { get; set; }

    public string? StudyShiftName { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
