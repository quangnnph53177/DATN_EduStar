using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Schedule
{
    public int Id { get; set; }

    public string? ClassName { get; set; }
    public int? SubjectId { get; set; }
    public int? RoomId { get; set; }
    public int? StudyShiftId { get; set; }
    public SchedulesStatus? Status { get; set; }
    public enum SchedulesStatus
    {
        Sapdienra = 0,
        Dangdienra = 1,
        Ketthuc = 2,
    }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? UsersId { get; set; }
    public virtual ICollection<AttendanceDetailsComplaint> AttendanceDetailsComplaints { get; set; } = new List<AttendanceDetailsComplaint>();
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<ClassChange> ClassChangeCurrentClasses { get; set; } = new List<ClassChange>();
    public virtual ICollection<ClassChange> ClassChangeRequestedClasses { get; set; } = new List<ClassChange>();
    public ICollection<ScheduleStudentsInfor> ScheduleStudents { get; set; } = new List<ScheduleStudentsInfor>();
    public virtual ICollection<SchedulesInDay> ScheduleDays { get; set; } = new List<SchedulesInDay>();

    public virtual Subject? Subject { get; set; }
    public virtual User? User { get; set; }
    public virtual Room? Room { get; set; }
    public virtual StudyShift? StudyShift { get; set; }
}
