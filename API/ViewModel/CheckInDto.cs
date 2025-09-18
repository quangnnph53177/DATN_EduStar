using System.ComponentModel.DataAnnotations;
using static API.Models.AttendanceDetail;

namespace API.ViewModel
{
    public class CheckInDto
    {
        [Required(ErrorMessage = "ID điểm danh là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID điểm danh không hợp lệ")]
        public int AttendanceId { get; set; }

        [Required(ErrorMessage = "ID sinh viên là bắt buộc")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [EnumDataType(typeof(AttendanceStatus), ErrorMessage = "Trạng thái không hợp lệ")]
        public AttendanceStatus Status { get; set; }
    }
}
