using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class ComplaintRepos : IComplaintRepos
    {
        private readonly AduDbcontext _context;
        public ComplaintRepos(AduDbcontext aduDbcontext)
        {
            _context = aduDbcontext;
        }

        public async Task<Complaint> CreateComplaint(Complaint complaint)
        {
            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();
            return complaint;
        }

        public async Task<IEnumerable<Complaint>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername)
        {
            var query = _context.Complaints
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.ProcessedByNavigation)
                .AsQueryable();

            if (!currentUserRoleIds.Contains(1)) // Nếu không phải Admin
            {
                // Chỉ xem khiếu nại của chính mình
                query = query.Where(c => c.Student != null && c.Student.UserName == currentUsername);
            }

            return await query
                .OrderByDescending(c => c.CreateAt)
                .ToListAsync();
        }
    }
}
