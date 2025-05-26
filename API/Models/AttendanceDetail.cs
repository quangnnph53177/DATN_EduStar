using System;
using System.Collections.Generic;

namespace API.Models;

public partial class AttendanceDetail
{
    public int Id { get; set; }

    public Guid? StudentId { get; set; }

    public int? AttendanceId { get; set; }

    public string? Statuss { get; set; }

    public string? Description { get; set; }

    public virtual Attendance? Attendance { get; set; }

    public virtual StudentsInfor? Student { get; set; }
}
