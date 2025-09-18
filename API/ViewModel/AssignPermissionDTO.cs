using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class AssignPermissionDTO
    {
        [Required(ErrorMessage = "Role ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Role ID không hợp lệ")]
        public int RoleId { get; set; }

        [StringLength(50, ErrorMessage = "Tên role không quá 50 ký tự")]
        public string? RoleName { get; set; }

        [Required(ErrorMessage = "Danh sách quyền là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 quyền")]
        public List<int> PermissionIds { get; set; } = new();

        [StringLength(100, ErrorMessage = "Tên quyền không quá 100 ký tự")]
        public string? PermissionName { get; set; }
    }
}
