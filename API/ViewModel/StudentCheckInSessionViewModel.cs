namespace API.ViewModel
{
    public class StudentCheckInSessionViewModel
    {
        public int AttendanceId { get; set; }               // Id phiên điểm danh
        public string ClassName { get; set; }               // Tên lớp
        public string SubjectName { get; set; }             // Tên môn học
        public DateTime? StartTime { get; set; }             // Thời gian bắt đầu
        public DateTime? EndTime { get; set; }               // Thời gian kết thúc
        public string Status { get; set; }                  // Trạng thái: "Chưa điểm danh" / "Đã điểm danh"
    }
}
