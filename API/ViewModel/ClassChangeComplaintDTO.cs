using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class ClassChangeComplaintDTO
    {
        [Required(ErrorMessage = "Chưa nhập lớp cần đổi.")]
        public int CurrentClassId { get; set; }
        [Required(ErrorMessage = "Chưa nhập lớp muốn đổi.")]
        public int RequestedClassId { get; set; }
        public string Reason { get; set; }
    }
}
