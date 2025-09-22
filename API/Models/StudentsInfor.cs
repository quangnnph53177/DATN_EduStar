using System;
using System.Collections.Generic;

namespace API.Models;

public partial class StudentsInfor
{
    public Guid UserId { get; set; }

    public string? StudentsCode { get; set; }

    // Thêm dòng này để lưu trữ vector đặc trưng khuôn mặt
    public byte[]? FaceFeatures { get; set; }

    public virtual ICollection<AttendanceDetail> AttendanceDetails { get; set; } = new List<AttendanceDetail>();

    public virtual User User { get; set; } = null!;

    public ICollection<ScheduleStudentsInfor> ScheduleStudents { get; set; } = new List<ScheduleStudentsInfor>();
}