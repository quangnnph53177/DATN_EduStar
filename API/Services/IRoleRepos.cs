using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IRoleRepos
    {
        Task<bool> AssignPermissionsAsync(int roleId, List<int> permissionIds);
        Task<Role> CreateRole (RoleDTO role);
        Task<List<Role>> GetAllRoles();

    }
}
