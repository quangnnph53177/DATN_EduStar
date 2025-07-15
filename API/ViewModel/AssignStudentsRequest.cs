namespace API.ViewModel
{
    public class AssignStudentsRequest
    {
        public int ClassId { get; set; }
        public List<Guid> StudentIds { get; set; }
    }
}
