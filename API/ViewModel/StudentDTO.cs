using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class StudentDTO
    {
        public Guid id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ, số và _")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Mã sinh viên là bắt buộc")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Mã sinh viên từ 5-20 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã sinh viên chỉ chứa chữ in hoa và số")]
        public string? StudentCode { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(256, ErrorMessage = "Email không quá 256 ký tự")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2-100 ký tự")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        public bool? Gender { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        public DateOnly? Dob { get; set; }
    }
}

