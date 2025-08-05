using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace API.ViewModel
{
    public class UserRegisterDTO:IValidatableObject
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public List<int>? RoleIds { get; set; } = new();

        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string? Address { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(UserName))
                yield return new ValidationResult("Tên đăng nhập là bắt buộc.", new[] { nameof(UserName) });
            else if (UserName.Length < 3)
            {
                yield return new ValidationResult("Tên đăng nhập phải có ít nhất 3 ký tự", new[] { nameof(UserName) });
            }
            else if (UserName.Length > 50)
            {
                yield return new ValidationResult("Tên đăng nhập không được vượt quá 50 ký tự", new[] { nameof(UserName) });
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(UserName, @"^[a-zA-Z0-9_@]+$"))
            {
                yield return new ValidationResult("Tên đăng nhập chỉ được chứa chữ cái, số, @ và dấu gạch dưới", new[] { nameof(UserName) });
            }

            if (string.IsNullOrWhiteSpace(Password))
                yield return new ValidationResult("Mật khẩu là bắt buộc.", new[] { nameof(Password) });
            else if (Password.Length < 6)
                yield return new ValidationResult("Mật khẩu phải có ít nhất 6 ký tự.", new[] { nameof(Password) });
            else if (Password.Length > 50)
                yield return new ValidationResult("Mật khẩu không được vượt quá 50 ký tự.", new[] { nameof(Password) });

            if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email))
                yield return new ValidationResult("Email không hợp lệ.", new[] { nameof(Email) });

            if (string.IsNullOrWhiteSpace(PhoneNumber) || !Regex.IsMatch(PhoneNumber, @"^\d{10}$"))
                yield return new ValidationResult("Số điện thoại phải gồm đúng 10 chữ số.", new[] { nameof(PhoneNumber) });

            if (RoleIds == null || !RoleIds.Any())
                yield return new ValidationResult("Người dùng phải có ít nhất một vai trò.", new[] { nameof(RoleIds) });

            if (!string.IsNullOrWhiteSpace(Address) && Address.Length > 300)
                yield return new ValidationResult("Địa chỉ không được quá 300 ký tự.", new[] { nameof(Address) });

            if (Dob.HasValue && Dob.Value.Date >= DateTime.Today)
                yield return new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(Dob) });
            if (Gender==null)
                yield return new ValidationResult("Giới tính là bắt buộc.", new[] { nameof(Gender) });
        }
    }
}
