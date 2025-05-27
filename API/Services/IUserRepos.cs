using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IUserRepos
    {
        Task<UserDTO> GetUserByName(string username);
        Task<IEnumerable<UserDTO>> GetAllUsers();
        Task<User> Register(UserDTO user);
        Task<string> Login(string userName, string password);
        Task UpdateUser(UserDTO userd);
    }
}
