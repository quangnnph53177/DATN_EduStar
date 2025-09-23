using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class CheckInByFaceDto
    {
        //[Required(ErrorMessage = "ID điểm danh là bắt buộc")]
        //[Range(1, int.MaxValue, ErrorMessage = "ID điểm danh không hợp lệ")]
        public int AttendanceId { get; set; }

        [Required(ErrorMessage = "Tệp ảnh khuôn mặt là bắt buộc")]
        public IFormFile FaceImage { get; set; }
    }
}
