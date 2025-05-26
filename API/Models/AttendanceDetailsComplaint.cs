using System;
using System.Collections.Generic;

namespace API.Models;

public partial class AttendanceDetailsComplaint
{
    public int ComplaintId { get; set; }

    public int? SchedulesId { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Schedule? Schedules { get; set; }
}
