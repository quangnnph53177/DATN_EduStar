namespace API.ViewModel
{
    public class IndexAttendanceViewModel
    {
        public int AttendanceId { get; set; }
        public string? SessionCode {  get; set; }
        public string? ClassName { get; set; }
        public string? SubjectName { get; set; }
        public string? ShiftStudy { get; set; }
        public string? RoomCode  { get; set; }
        public string? WeekDay { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<StudentAttendanceViewModel> stinclass { get; set; }
    }
}
