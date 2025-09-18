//using API.Models;
using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class SubjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên môn học là bắt buộc")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên môn học từ 3-200 ký tự")]
        public string SubjectName { get; set; } = null!;

        [Required(ErrorMessage = "Mã môn học là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Mã môn học từ 2-50 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã môn chỉ chứa chữ in hoa và số")]
        public string SubjectCode { get; set; } = null!; // Đổi từ subjectCode

        [StringLength(500, ErrorMessage = "Mô tả không quá 500 ký tự")]
        public string? Description { get; set; }

        public bool? Status { get; set; }
    }
}
