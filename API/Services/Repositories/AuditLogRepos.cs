using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class AuditLogRepos : IAuditLogRepos
    {
        private readonly AduDbcontext _context;
        public AuditLogRepos(AduDbcontext aduDbcontext)
        {
            _context = aduDbcontext;
        }
        public Task LogAsync(Guid? userId, string action, string? oldData, string? newData, Guid? performedBy)
        {
            try
            {
                var log = new Auditlog
                {
                    Userid = userId,
                    Active = action,
                    OldData = oldData,
                    NewData = newData,
                    PerformeBy = performedBy,
                    Timestamp = DateTime.Now
                };
                _context.Auditlogs.Add(log);
                return _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Lỗi ghi AuditLog:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");

                throw new Exception("Lỗi ghi AuditLog chi tiết: " + ex.InnerException?.Message, ex);
            }
        }
        public async Task<List<Auditlog>> GetAuditLogsAsync(List<int> currentUserRoleIds, string? currentUserName)
        {
            var query = _context.Auditlogs
                .Include(a => a.User) // Lấy thông tin người bị thao tác
                .ThenInclude(u => u.Roles)
                .Include(a => a.PerformeByNavigation) // Nếu cần lấy tên người thực hiện (PerformBy)
                .AsQueryable();

            if (!currentUserRoleIds.Contains(1)) // Nếu KHÔNG phải Admin
            {
                // Chỉ xem các log mà mình là đối tượng bị thao tác
                query = query.Where(a => a.PerformeByNavigation != null && a.PerformeByNavigation.UserName == currentUserName);
            }

            return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
        }
    }
}
