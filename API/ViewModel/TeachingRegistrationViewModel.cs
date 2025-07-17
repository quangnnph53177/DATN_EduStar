using API.Models;

namespace API.ViewModel
{
    public class TeachingRegistrationViewModel
    {
        public int ClassId { get; set; }
        public int DayId { get; set; }
        public int StudyShiftId { get; set; }
        public int SemesterId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
