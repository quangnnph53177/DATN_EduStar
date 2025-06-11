
using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IUserRepos
    {
        //Task<UserDTO> SearchUser(string? username, string? usercode, string? fullname, string? email);
        Task<IEnumerable<UserDTO>> GetAllUsers(List<int> currentUserRoleIds, string? currentUserName);
        Task<IEnumerable<UserDTO>> GetAllUsersNoTeacher(List<int> currentUserRoleIds, string? currentUserName);
        Task<User> Register(UserDTO user);
        Task CleanupUnconfirmedUsers();
        Task<LoginResult> Login(string userName, string password);
        Task UpdateUser(UserDTO userd);
        Task<bool> ConfirmEmail(string token);
        Task<string> LockUser(string userName);
        Task<string> ChangeRole(string userName, int newRoleId);
        Task ForgetPassword(string email);
        Task<string> ResetPassword(string token, string newPassword);

    }
    public class LoginResult
    {
        public string Token { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }
    }
}
