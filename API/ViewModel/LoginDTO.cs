using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class LoginDTO : IValidatableObject
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                yield return new ValidationResult("Tên đăng nhập không được để trống", new[] { nameof(UserName) });
            }
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
            {
                yield return new ValidationResult("Mật khẩu không được để trống", new[] { nameof(Password) });
            }
            else if (Password.Length < 6)
            {
                yield return new ValidationResult("Mật khẩu phải có ít nhất 6 ký tự", new[] { nameof(Password) });
            }
            else if (Password.Length > 50)
            {
                yield return new ValidationResult("Mật khẩu không được vượt quá 50 ký tự", new[] { nameof(Password) });
            }
        }
    }
    public class ResetPasswordDTO
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                yield return new ValidationResult("Token không được để trống", new[] { nameof(Token) });
            }
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                yield return new ValidationResult("Mật khẩu mới không được để trống", new[] { nameof(NewPassword) });
            }
            else if (NewPassword.Length < 6)
            {
                yield return new ValidationResult("Mật khẩu mới phải có ít nhất 6 ký tự", new[] { nameof(NewPassword) });
            }
        }
    }
    public class LoginResult
    {
        public string Token { get; set; }
        public List<int> RoleId { get; set; }
        public List<string> RoleName { get; set; }
        public string UserName { get; set; }
        public List<string> Permission { get; set; }
    }
}
