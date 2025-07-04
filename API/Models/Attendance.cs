using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Attendance
{
    public int Id { get; set; }

    public int? SchedulesId { get; set; }

    public Guid? UserId { get; set; }
    public string? SessionCode { get; set; }
    public DateTime? CreateAt { get; set; }
    public DateTime? Starttime { get; set; }
    public DateTime? Endtime { get; set; }

    public virtual ICollection<AttendanceDetail> AttendanceDetails { get; set; } = new List<AttendanceDetail>();

    public virtual Schedule? Schedules { get; set; }

    public virtual User? User { get; set; }
}
