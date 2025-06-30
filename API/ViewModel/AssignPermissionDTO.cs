namespace API.ViewModel
{
    public class AssignPermissionDTO
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; } = null!;
        public List<int> PermissionIds { get; set; } = new();
        public string? PermissionName { get; set; } = null!;
    }
}
