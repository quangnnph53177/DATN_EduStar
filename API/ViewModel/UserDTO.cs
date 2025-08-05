using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    public class UserDTO 
    {
        public Guid? Id { get; set; }

        public string? UserName { get; set; }
        public string? PassWordHash { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public List<int>? RoleIds { get; set; } = new();

        public bool Statuss { get; set; } = true;
        public bool? IsConfirm { get; set; } = true;
        public DateTime? CreateAt { get; set; }

        // Thông tin hồ sơ người dùng
        public string? UserCode { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
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
    }
}
