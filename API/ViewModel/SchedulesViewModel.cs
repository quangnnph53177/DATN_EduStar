namespace API.ViewModel
{
    public class SchedulesViewModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public string RoomCode { get; set; }
        public string SubjectName { get; set; }
        public string WeekDay { get; set; }
        public string StudyShift { get; set; }
        public TimeOnly? starttime { get; set; }
        public TimeOnly? endtime {  get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
        public int? StudentCount { get; set; }
        public Guid? UserId { get; set; }
    }
}
