using System.ComponentModel.DataAnnotations;

namespace API.ViewModel
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}
