using API.ViewModel.Validations;
using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class SchedulesDTO : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên lớp là bắt buộc")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên lớp từ 3-100 ký tự")]
        public string? ClassName { get; set; }

        [Required(ErrorMessage = "Môn học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID môn học không hợp lệ")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Phòng học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID phòng không hợp lệ")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Ngày học là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ngày")]
        [MaxLength(7, ErrorMessage = "Không thể chọn quá 7 ngày")]
        public List<int> WeekDayId { get; set; } = new();

        [Required(ErrorMessage = "Ca học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID ca học không hợp lệ")]
        public int StudyShiftId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime? StartDate { get; set; } // Đổi tên từ startdate

        public DateTime? EndDate { get; set; } // Đổi tên từ enddate

        public Guid? TeacherId { get; set; }

        [StringLength(50, ErrorMessage = "Trạng thái không quá 50 ký tự")]
        public string? Status { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && StartDate < DateTime.Today)
            {
                yield return new ValidationResult("Ngày bắt đầu không thể trong quá khứ",
                    new[] { nameof(StartDate) });
            }

            if (StartDate.HasValue && EndDate.HasValue)
            {
                var duration = (EndDate.Value - StartDate.Value).TotalDays;
                if (duration > 365)
                {
                    yield return new ValidationResult("Lịch học không thể dài hơn 1 năm",
                        new[] { nameof(EndDate) });
                }
            }
        }
    }
}

