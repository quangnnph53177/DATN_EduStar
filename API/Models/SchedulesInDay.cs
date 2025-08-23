namespace API.Models
{
    public class SchedulesInDay
    {

        public int Id { get; set; }

        public int ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }

        public int DayOfWeekkId { get; set; }
        public DayOfWeekk? DayOfWeekk { get; set; }
    }

}
