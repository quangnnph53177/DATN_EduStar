using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IComplaintRepos
    {
        Task<IEnumerable<ComplaintDTO>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername);
        Task<string> SubmitClassChangeComplaint(ClassChangeComplaintDTO dto, string userCode);
        Task<List<ClassViewModel>> ClassesOfStudent(string userCode);
        Task<bool> ProcessClassChangeComplaint(int complaintId, ProcessComplaintDTO dto, Guid handlerId);
        Task<IEnumerable<ComplaintDTO>> ChiTietKhieuNai(int complaintId);
    }
}
