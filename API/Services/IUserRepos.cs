using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IUserRepos
    {
        Task<User> GetUserById(Guid id);
        Task<IEnumerable<User>> GetAllUsers();
        Task<User> Register(UserDTO user);
        Task<string> Login(string userName, string password);
        Task<User> UpdateUser(User user);
    }
}
