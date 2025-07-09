using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Schedule
{
    public int Id { get; set; }

    public int? ClassId { get; set; }

    public int? RoomId { get; set; }

    public int? DayId { get; set; }

    public int? StudyShiftId { get; set; }
    public bool? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public virtual ICollection<AttendanceDetailsComplaint> AttendanceDetailsComplaints { get; set; } = new List<AttendanceDetailsComplaint>();

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class? Class { get; set; }

    public virtual DayOfWeekk? Day { get; set; }

    public virtual Room? Room { get; set; }

    public virtual StudyShift? StudyShift { get; set; }
}
