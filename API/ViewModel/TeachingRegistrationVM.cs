namespace API.ViewModel
{
    public class TeachingRegistrationVM
    {
        public  int regisID { get; set; }
        public  int SchedulesId { get; set; }
        public string ClassName { get; set; }
        public string SujectName { get; set; }
        public  string RoomName { get; set; }
        public string ShiftName { get; set ; }
        public TimeOnly? starttime { get; set; }
        public TimeOnly? endtime { get; set; }
        public DateTime  StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Weekday> DayNames { get; set; }
        public DateTime CreateAt {  get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string ApprovedBy {  get; set; }
        public DateTime ApprovedDate { get; set; }
    }
}
