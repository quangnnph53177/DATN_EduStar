namespace API.ViewModel
{
    public class AssignStudentsRequest
    {
        public int SchedulesId { get; set; }
        public List<Guid> StudentIds { get; set; }
    }
}
