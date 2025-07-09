using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IComplaintRepos
    {
        Task<IEnumerable<ComplaintDTO>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername);
        Task<string> SubmitClassChangeComplaint(ClassChangeComplaintDTO dto,Guid studentId);
        Task<List<ClassCreateViewModel>> GetClassesOfStudent(Guid studentId);
        Task<List<Class>> GetClassesInSameSubject(int currentClassId);

        Task<bool> ProcessClassChangeComplaint(int complaintId, ProcessComplaintDTO dto,Guid handlerId);
    }
}
