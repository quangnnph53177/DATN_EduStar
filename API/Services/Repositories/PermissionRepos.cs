using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class PermissionRepos : IPermissionRepos
    {
        private readonly AduDbcontext _context;
        public PermissionRepos(AduDbcontext aduDbcontext)
        {
            _context = aduDbcontext;
        }
        public async Task<Permission> CreatePermissionAsync(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName))
                throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));
            var exits = await _context.Permissions.AnyAsync(p=>p.PermissionName == permissionName);
            var permission = new Permission { PermissionName = permissionName };
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }
        public async Task<List<Permission>> GetPermissionsAsync()
        {
            if (_context.Permissions == null)
                throw new Exception("Permissions table is not initialized.");
            if (!await _context.Permissions.AnyAsync())
                throw new Exception("No permissions found in the database.");
            return await _context.Permissions.ToListAsync();
        }
    }
}
