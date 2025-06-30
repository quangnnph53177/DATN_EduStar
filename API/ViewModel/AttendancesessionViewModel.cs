namespace API.ViewModel
{
    public class AttendancesessionViewModel
    {
        public int Schedulesid { get; set; }
        public int ClassId { get; set; }
        public string NameClass { get; set; }
        //public int subjectid { get; set; }
        public string subjectname { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string StudyShiftName { get; set; }
        public DateTime StudyDate { get; set; }           // Ngày học (hôm nay)
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string DayOfWeek { get; set; }
        public List<StudentAttendanceViewModel> Students { get; set; }
    }
}
