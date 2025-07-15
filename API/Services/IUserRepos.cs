
using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IUserRepos
    {
        Task<IEnumerable<UserDTO>> GetAllUsers(List<int> currentUserRoleIds, string? currentUserName, bool excludeTeacher = false);
        Task<User> Register(UserDTO user, IFormFile? imgFile);
        Task<List<User>> CleanupUnconfirmedUsers();
        Task<LoginResult> Login(string userName, string password);
        Task UpdateUser(UserDTO userd, IFormFile imgFile);
        Task<string> ConfirmEmail(string token);
        Task<string> LockUser(string userName, Guid currentUserId);
        Task<string> ChangeRole(string userName, int newRoleId);
        //Task<TeacherWithClassesViewModel> GetStudentByTeacher(Guid? teacherId);
        Task ForgetPassword(string email);
        Task<string> ResetPassword(string token, string newPassword);

    }
    public class LoginResult
    {
        public string Token { get; set; }
        public List<int> RoleId { get; set; }
        public List<string> RoleName { get; set; }
        public string UserName { get; set; }
        public List<string> Permission { get; set; }
    }
}
