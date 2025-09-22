using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class CreateAttendanceViewModel : IValidatableObject
    {
        //[Required(ErrorMessage = "Lịch học là bắt buộc")]
        //[Range(1, int.MaxValue, ErrorMessage = "ID lịch học không hợp lệ")]
        public int? SchedulesId { get; set; }

        //[Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public Guid? UserId { get; set; }

        //[Required(ErrorMessage = "Mã phiên là bắt buộc")]
        //[StringLength(20, MinimumLength = 6, ErrorMessage = "Mã phiên từ 6-20 ký tự")]
        //[RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã phiên chỉ chứa chữ in hoa và số")]
        public string? SessionCode { get; set; }

        public DateTime? CreateAt { get; set; }

        //[Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime? Starttime { get; set; }

        //[Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime? Endtime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Starttime.HasValue && Endtime.HasValue && Endtime <= Starttime)
            {
                yield return new ValidationResult("Thời gian kết thúc phải sau thời gian bắt đầu",
                    new[] { nameof(Endtime) });
            }
        }
    }
}
