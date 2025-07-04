namespace API.ViewModel
{
    public class StudentAttendanceHistory
    {
        public string SubjectName { get; set; }
        public string ClassName { get; set; }
        public string Shift { get; set; }
        public string WeekDay { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime AttendanceDate { get; set; }
    }
}
