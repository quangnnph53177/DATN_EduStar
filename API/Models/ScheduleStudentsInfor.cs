using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class ScheduleStudentsInfor
    {
        [Key]
        public int Id { get; set; } 

        public int SchedulesId { get; set; }
        public Guid StudentsUserId { get; set; }

        // Navigation properties
        public Schedule? Schedule { get; set; }
        public StudentsInfor? Student { get; set; }
    }
}
