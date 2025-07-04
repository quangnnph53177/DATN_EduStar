using static API.Models.AttendanceDetail;

namespace API.ViewModel
{
    public class CheckInDto
    {
        public int AttendanceId { get; set; }
        public Guid StudentId { get; set; }
        public AttendanceStatus Status { get; set; } // enum: Present, Absent, Late
    }
}
