using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    public class UserDTO : IValidatableObject
    {
        public Guid? Id { get; set; }

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_@]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ, số, @ và _")]
        public string? UserName { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6-100 ký tự")]
        public string? PassWordHash { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(256, ErrorMessage = "Email không quá 256 ký tự")]
        public string? Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0")]
        public string? PhoneNumber { get; set; }

        public List<int>? RoleIds { get; set; } = new();

        public bool Statuss { get; set; } = true;
        public bool IsConfirm { get; set; } = true;
        public DateTime? CreateAt { get; set; }

        [StringLength(20, ErrorMessage = "Mã người dùng không quá 20 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã người dùng chỉ chứa chữ in hoa và số")]
        public string? UserCode { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2-100 ký tự")]
        public string? FullName { get; set; }

        public bool? Gender { get; set; }

        [Url(ErrorMessage = "URL avatar không hợp lệ")]
        [StringLength(500, ErrorMessage = "URL avatar không quá 500 ký tự")]
        public string? Avatar { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự")]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Dob { get; set; }

        public List<string>? ClassName { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return "NA";

                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                    return parts[0][0].ToString().ToUpper();

                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Dob.HasValue && Dob.Value.Date >= DateTime.Today)
            {
                yield return new ValidationResult("Ngày sinh phải trong quá khứ", new[] { nameof(Dob) });
            }

            if (Dob.HasValue && (DateTime.Today.Year - Dob.Value.Year) < 16)
            {
                yield return new ValidationResult("Người dùng phải từ 16 tuổi trở lên", new[] { nameof(Dob) });
            }

            if (RoleIds != null && RoleIds.Any(id => id < 1))
            {
                yield return new ValidationResult("ID vai trò không hợp lệ", new[] { nameof(RoleIds) });
            }
        }
    }
}
