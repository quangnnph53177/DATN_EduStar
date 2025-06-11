namespace API.ViewModel
{
    public class UserDTO
    {
        public string? UserName { get; set; } = null!;

        public string? PassWordHash { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();

        public bool Statuss { get; set; } = true;

        public DateTime? CreateAt { get; set; }


        // UserProfile
        public string? UserCode { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public DateTime? Dob { get; set; }
    }
}
