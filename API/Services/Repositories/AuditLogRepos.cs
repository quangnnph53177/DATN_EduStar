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
                    Timestamp = DateTime.UtcNow
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
               .Include(a => a.User) // để lấy User.UserName
               .ThenInclude(u => u.Roles)
               .AsQueryable();

            if (currentUserRoleIds.Contains(1))
            {
                // Admin xem toàn bộ
            }
            //else if (currentUserRoleIds.Contains(2))
            //{
            //    // Giảng viên: log của chính mình hoặc của sinh viên
            //    query = query.Where(a =>
            //        a.User != null && (
            //            a.User.UserName == currentUserName ||
            //            a.User.Roles.Any(r => r.Id == 3)
            //        ));
            //}
            else
            {
                // Sinh viên: chỉ xem log của mình
                query = query.Where(a => a.User != null && a.User.UserName == currentUserName);
            }

            return await query.ToListAsync();
        }
    }
}
