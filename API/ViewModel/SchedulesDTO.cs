namespace API.ViewModel
{
    public class SchedulesDTO
    {
        public int Id { get; set; }
        public string? ClassName { get; set; }
        public int SubjectId { get; set; }
        public int RoomId { get; set; }
        public List<int> WeekDayId { get; set; }
        public int StudyShiftId { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
        public Guid? TeacherId { get; set; }
        public string? Status { get; set; }
    }
}

