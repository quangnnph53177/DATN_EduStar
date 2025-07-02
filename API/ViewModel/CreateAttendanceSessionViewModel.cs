namespace API.ViewModel
{
    public class CreateAttendanceSessionViewModel
    {
        public int? SchedulesId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? SessionCode { get; set; }
        public DateTime ? Createat { get; set; }
    }
}
