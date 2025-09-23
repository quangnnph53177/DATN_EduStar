using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class CreateAttendanceViewModel 
    {
        
        public int? SchedulesId { get; set; }

       
        public Guid? UserId { get; set; }


        public string? SessionCode { get; set; }

        public DateTime? CreateAt { get; set; }

   
        public DateTime? Starttime { get; set; }

        
        public DateTime? Endtime { get; set; }
    }
}
