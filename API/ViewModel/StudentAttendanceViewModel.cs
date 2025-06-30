namespace API.ViewModel
{
    public class StudentAttendanceViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; } = "Chưa điểm danh"; // Mặc định
    }
}
