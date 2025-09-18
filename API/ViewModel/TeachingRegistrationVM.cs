using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class TeachingRegistrationVM
    {
        [Required(ErrorMessage = "ID đăng ký là bắt buộc")]
        public int regisID { get; set; }

        [Required(ErrorMessage = "ID lịch học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID lịch học không hợp lệ")]
        public int SchedulesId { get; set; }

        [Required(ErrorMessage = "Tên lớp là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên lớp không quá 100 ký tự")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Tên môn học là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên môn học không quá 200 ký tự")]
        public string SubjectName { get; set; } // Sửa từ SujectName

        [Required(ErrorMessage = "Tên phòng là bắt buộc")]
        [StringLength(20, ErrorMessage = "Tên phòng không quá 20 ký tự")]
        public string RoomName { get; set; }

        [Required(ErrorMessage = "Tên ca học là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên ca học không quá 50 ký tự")]
        public string ShiftName { get; set; }

        public TimeOnly? starttime { get; set; }
        public TimeOnly? endtime { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<Weekday> DayNames { get; set; } = new();

        public DateTime CreateAt { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(50, ErrorMessage = "Trạng thái không quá 50 ký tự")]
        public string Status { get; set; }

        [StringLength(20, ErrorMessage = "Màu trạng thái không quá 20 ký tự")]
        public string StatusColor { get; set; }

        [StringLength(100, ErrorMessage = "Người duyệt không quá 100 ký tự")]
        public string ApprovedBy { get; set; }

        public DateTime ApprovedDate { get; set; }
    }
}
