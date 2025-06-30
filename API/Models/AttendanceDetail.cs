using System;
using System.Collections.Generic;

namespace API.Models;

public partial class AttendanceDetail
{
    public int Id { get; set; }

    public Guid? StudentId { get; set; }

    public int? AttendanceId { get; set; }
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        Leave,
        Unknown
    }
    public AttendanceStatus Status { get; set; }
    public DateTime? CheckinTime { get; set; }
    public string? ImagePath { get; set; }
    public string? Description { get; set; }

    public virtual Attendance? Attendance { get; set; }

    public virtual StudentsInfor? Student { get; set; }
}
