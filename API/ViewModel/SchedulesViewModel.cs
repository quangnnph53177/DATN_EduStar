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
        public TimeOnly? endtime
        {
            get; set;
        }
    }
}
