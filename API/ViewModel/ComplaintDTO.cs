using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class ComplaintDTO
    {
        public int Id { get; set; }

        [StringLength(50, ErrorMessage = "Loại khiếu nại không quá 50 ký tự")]
        public string? ComplaintType { get; set; }

        [StringLength(50, ErrorMessage = "Trạng thái không quá 50 ký tự")]
        public string? Statuss { get; set; }

        [StringLength(500, ErrorMessage = "Lý do không quá 500 ký tự")]
        public string? Reason { get; set; }

        [Url(ErrorMessage = "URL không hợp lệ")]
        [StringLength(500, ErrorMessage = "URL không quá 500 ký tự")]
        public string? ProofUrl { get; set; }

        [StringLength(100, ErrorMessage = "Tên sinh viên không quá 100 ký tự")]
        public string? StudentName { get; set; }

        [StringLength(100, ErrorMessage = "Tên người xử lý không quá 100 ký tự")]
        public string? ProcessedByName { get; set; }

        public DateTime? CreateAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú phản hồi không quá 500 ký tự")]
        public string? ResponseNote { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID lớp hiện tại không hợp lệ")]
        public int? CurrentClassId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID lớp yêu cầu không hợp lệ")]
        public int? RequestedClassId { get; set; }

        public string? CurrentClassName { get; set; }
        public string? RequestedClassName { get; set; }
    }
}
