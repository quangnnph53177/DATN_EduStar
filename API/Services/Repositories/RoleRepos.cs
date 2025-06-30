
using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class RoleRepos : IRoleRepos
    {
        private readonly AduDbcontext _context;
        public RoleRepos(AduDbcontext aduDbcontext)
        {
            _context = aduDbcontext;
        }
        public async Task<bool> AssignPermissionsAsync(int roleId, List<int> permissionIds)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
                throw new Exception("Vai trò không tồn tại");
            var permissions = await _context.Permissions
                .Where(p=>permissionIds.Contains(p.Id))
                .ToListAsync();
            role.Permissions.Clear();
            foreach (var permission in permissions)
            {
                role.Permissions.Add(permission);
            }
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<Role> CreateRole(RoleDTO role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role), "Vai trò không được null");
            if (string.IsNullOrWhiteSpace(role.RoleName))
                throw new ArgumentException("Tên vai trò không được để trống", nameof(role.RoleName));
            if (await _context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
                throw new InvalidOperationException($"Vai trò với tên '{role.RoleName}' đã tồn tại");

            // Map RoleDTO to Role
            var newRole = new Role
            {
                RoleName = role.RoleName
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();
            return newRole;
        }
        public async Task<List<Role>> GetAllRoles()
        {
            // Lấy tất cả các vai trò và bao gồm Permissions liên quan
            // Sử dụng Include để lấy dữ liệu liên kết
            if (_context.Roles == null)
                throw new InvalidOperationException("Không tìm thấy bảng Roles trong cơ sở dữ liệu");
            if (_context.Permissions == null)
                throw new InvalidOperationException("Không tìm thấy bảng Permissions trong cơ sở dữ liệu");
            return await _context.Roles
                .Include(r => r.Permissions)
                .ToListAsync();
        }
    }
}
