using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class ClassChangeComplaintDTO
    {
        [Required(ErrorMessage = "Chưa nhập lớp cần đổi")]
        [Range(1, int.MaxValue, ErrorMessage = " lớp hiện tại không hợp lệ")]
        public int CurrentClassId { get; set; }

        [Required(ErrorMessage = "Chưa nhập lớp muốn đổi")]
        [Range(1, int.MaxValue, ErrorMessage = " lớp muốn đổi không hợp lệ")]
        public int RequestedClassId { get; set; }

        [Required(ErrorMessage = "Lý do là bắt buộc")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do từ 10-500 ký tự")]
        public string Reason { get; set; }

        ///// <summary>
        ///// Ảnh minh chứng khiếu nại (tuỳ chọn)
        ///// </summary>
        //public IFormFile? ProofFile { get; set; }
    }
}
