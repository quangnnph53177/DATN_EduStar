using API.Models;

namespace API.Services
{
    public interface IComplaintRepos
    {
        Task<IEnumerable<Complaint>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername);
        Task<Complaint> CreateComplaint(Complaint complaint);
    }
}
