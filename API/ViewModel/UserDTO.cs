using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class UserDTO
    {
        public Guid? Id { get; set; }
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        public string? UserName { get; set; } = null!;
        //[Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự.")]

        public string? PassWordHash { get; set; } = null!;
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]

        public string? Email { get; set; }
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số.")]
        public string PhoneNumber { get; set; } = null!;

        public List<int> RoleIds { get; set; } = new List<int>();

        public bool Statuss { get; set; } = true;

        public DateTime? CreateAt { get; set; }


        // UserProfile
        public string? UserCode { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? Avatar { get; set; }
        [MaxLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự.")]
        public string? Address { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? Dob { get; set; }


        // 👉 Tạo Initials để hiển thị viết tắt tên người dùng
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return "NA";

                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                    return parts[0].Substring(0, 1).ToUpper();

                // Lấy chữ cái đầu của từ đầu tiên và từ cuối cùng (VD: Nguyễn Văn A -> NA)
                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }

    }
}
