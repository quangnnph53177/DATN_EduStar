using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class ProcessComplaintDTO
    {
        [Required(ErrorMessage = "ID khiếu nại là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID khiếu nại không hợp lệ")]
        public int ComplaintId { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression(@"^(Processing|Approved|Rejected)$",
            ErrorMessage = "Trạng thái phải là: Processing, Approved hoặc Rejected")]
        public string Status { get; set; } = "Processing";

        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
        public string Note { get; set; } = string.Empty;
    }
}
