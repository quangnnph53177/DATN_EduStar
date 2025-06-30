using API.Models;

namespace API.Services
{
    public interface IPermissionRepos
    {
        Task<Permission> CreatePermissionAsync(string permissionName);
        Task<List<Permission>> GetPermissionsAsync();
    }
}
