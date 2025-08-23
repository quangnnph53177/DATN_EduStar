namespace API.Models
{
    public class TeachingRegistration
    {
        public int Id { get; set; }
        public Guid TeacherId { get; set; }         // Tài khoản giảng viên
        public int ScheduleID { get; set; }            // Lớp học 
        public bool? Status { get; set; }           // Có hiệu lực không
        //public bool? IsConfirmed { get; set; }      // Đã được xác nhận chưa?
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public ApprovedStatus? IsApproved { get; set; }       // null = chờ duyệt, true = đã duyệt, false = từ chối
        public enum ApprovedStatus
        {
            Pending =0 ,
            Approved = 1,
            Rejected = 2,
        }
        public Guid? ApprovedBy { get; set; }       // Admin duyệt

        public DateTime? ApprovedDate { get; set; } // Ngày duyệt

        // Navigation Properties
        public virtual User Teacher { get; set; }
        public virtual Schedule Schedule { get; set; }
        public virtual User? Approver { get; set; }
    }
}
