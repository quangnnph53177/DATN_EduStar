namespace API.ViewModel
{
    public class SchedulesViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public string RoomCode { get; set; }
        public string SubjectName { get; set; }
        public List<Weekday> weekdays { get; set; }
        public string StudyShift { get; set; }
        public TimeOnly? starttime { get; set; }
        public TimeOnly? endtime { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
        public string? Status { get; set; }
        public Guid? UserId { get; set; }
        public List<StudentViewModels>? Students { get; set; }
    }
}
