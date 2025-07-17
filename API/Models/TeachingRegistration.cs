namespace API.Models
{
    public class TeachingRegistration
    {
        public int Id { get; set; }

        public Guid TeacherId { get; set; }         // Tài khoản giảng viên
        public int ClassId { get; set; }            // Lớp học (môn học được gán sẵn theo Class.SubjectId)
        public int DayId { get; set; }              // Thứ trong tuần
        public int StudyShiftId { get; set; }       // Ca học

        public DateTime? StartDate { get; set; }    // Bắt đầu giảng dạy
        public DateTime? EndDate { get; set; }      // Kết thúc giảng dạy
        public bool? Status { get; set; }           // Có hiệu lực không

        public bool? IsConfirmed { get; set; }      // Đã được xác nhận chưa?
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = null!;
        public virtual User Teacher { get; set; }
        public virtual Class Class { get; set; }
        public virtual DayOfWeekk Day { get; set; }
        public virtual StudyShift StudyShift { get; set; }
    }
}
