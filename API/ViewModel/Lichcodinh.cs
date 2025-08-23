namespace API.ViewModel
{
    public class Lichcodinh
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string RoomCode { get; set; }
        public string SubjectName { get; set; }
        public string StudyShift { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? Endtime { get; set; }
        public string? TeacherName { get; set; }
        public List<Weekday> weekdays { get; set; }
        public string? Status { get; set; }
    }
}
