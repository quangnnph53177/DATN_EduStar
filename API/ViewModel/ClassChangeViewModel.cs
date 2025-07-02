namespace API.ViewModel
{
    public class ClassChangeViewModel
    {
        public string? ChangeDescription { get; set; }
        public DateTime ChangeDate { get; set; }
        public int? ChangedByUserId { get; set; } // Optional: ID của người thực hiện thay đổi
        public string? ChangedByUserName { get; set; } // Optional: Tên người thực hiện
    }
}