namespace API.ViewModel
{
    public class TeacherClassViewModel
    {
        public int ScheduleId { get; set; }
        public string ClassName { get; set; }
        public string SubjectName { get; set; }
        public string StudyShiftName { get; set; }
        public List<string> WeekDays { get; set; }
        public string RoomCode { get; set; }
        public bool CanCreateSession { get; set; } // Có thể tạo phiên không
        public bool HasActiveSession { get; set; } // Đã có phiên hoạt động
    }
}
