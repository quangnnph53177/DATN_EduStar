namespace API.ViewModel
{
    public class StudentViewModels
    {
        public Guid id { get; set; }
        public string UserName { get; set; }
        public string PassWordHash { get; set; } = null!;
        public string StudentCode { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }

        public bool? Gender { get; set; }

        public string? Avatar { get; set; }

        public string? Address { get; set; }

        public DateOnly? Dob { get; set; }
        public bool Status { get; set; }
        public List<ClassViewModel> CVMs { get; set; }
    }
}
