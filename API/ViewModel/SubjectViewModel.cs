//using API.Models;
using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class SubjectViewModel
    { 
        public int Id { get; set; }

        public string SubjectName { get; set; } = null!;
        public string subjectCode { get; set; } = null!;
        public string? Description { get; set; }
        public int? NumberOfCredits { get; set; }
        public int? SemesterId { get; set; }
        public bool? Status { get; set; }
    }
}
