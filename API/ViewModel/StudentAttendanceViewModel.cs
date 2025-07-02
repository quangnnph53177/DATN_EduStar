namespace API.ViewModel
{
    public class StudentAttendanceViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public bool? HasCheckedIn { get; set; }
        public string Status { get; set; } = "Chưa điểm danh"; // Mặc định
    }
}
