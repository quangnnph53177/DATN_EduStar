using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IAuditLogRepos
    {
        public Task LogAsync(Guid? userId, string action, string? oldData, string? newData, Guid? performedBy);
        public Task<List<Auditlog>> GetAuditLogsAsync(List<int> currentUserRoleIds, string? currentUserName);
    }
}
