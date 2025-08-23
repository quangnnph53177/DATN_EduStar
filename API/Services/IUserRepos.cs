using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IUserRepos
    {
        Task<IEnumerable<UserDTO>> GetAllUsers(List<int> currentUserRoleIds, string? currentUserName, bool excludeTeacher = false);
        Task<User> Register(UserRegisterDTO user, IFormFile? imgFile);
        Task<List<User>> CleanupUnconfirmedUsers();
        Task<LoginResult> Login(string userName, string passWord);
        Task UpdateUser(UserDTO userd, IFormFile imgFile);
        Task<string> ConfirmEmail(string token);
        Task<string> LockUser(string userName, Guid currentUserId);
        Task<string> ChangeRole(string userName, int newRoleId);
        Task ForgetPassword(string email);
        Task<string> ResetPassword(string token, string newPassword);
        Task<User> CreateSV(UserDTO usd, IFormFile? imgFile);

    }
}
