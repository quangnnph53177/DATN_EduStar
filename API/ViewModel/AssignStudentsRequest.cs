using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class AssignStudentsRequest
    {
        [Required(ErrorMessage = "Lịch học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID lịch học không hợp lệ")]
        public int SchedulesId { get; set; }

        [Required(ErrorMessage = "Danh sách sinh viên là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 sinh viên")]
        public List<Guid> StudentIds { get; set; } = new();
    }
}
