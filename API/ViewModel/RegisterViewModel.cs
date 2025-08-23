using Microsoft.AspNetCore.Mvc.Rendering;

namespace API.ViewModel
{
    public class RegisterViewModel
    {
        public UserDTO User { get; set; } = new();
        public List<SelectListItem> RoleLists { get; set; } = new();
    }
}
