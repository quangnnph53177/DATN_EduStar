namespace API.ViewModel
{
    public class StudentDTO
    {
        public Guid id { get; set; }
        public string? UserName { get; set; }
        public string? StudentCode { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? Address { get; set; }

        public DateOnly? Dob { get; set; }
    }
}
