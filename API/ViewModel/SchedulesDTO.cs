namespace API.ViewModel
{
    public class SchedulesDTO
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int RoomId { get; set; }
        public int WeekDayId { get; set; }
        public int StudyShiftId { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
    }
}

