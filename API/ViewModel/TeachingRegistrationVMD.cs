using API.Models;

namespace API.ViewModel
{
    public class TeachingRegistrationVMD
    {
        public int Id { get; set; }

        public string TeacherName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string DayName { get; set; } = string.Empty;
        public string ShiftName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsConfirmed { get; set; }
    }
}
