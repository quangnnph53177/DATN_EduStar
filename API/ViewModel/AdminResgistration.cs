using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class AdminResgistration:TeachingRegistrationVM
    {
        [Required(ErrorMessage = "Tên giảng viên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên giảng viên từ 2-100 ký tự")]
        public string TeacherName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(256, ErrorMessage = "Email không quá 256 ký tự")]
        public string TeacherEmail { get; set; }
    }
}
