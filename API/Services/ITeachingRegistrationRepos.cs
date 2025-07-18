using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface ITeachingRegistrationRepos
    {
        Task<string> CanRegister(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end);
        Task<string> RegisterTeaching(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end);

        Task<List<TeachingRegistrationVMD>> GetTeacherRegister(string? userName, bool isAdmin);
        Task<string> ConfirmTeachingRegistration(int registrationId);
    }
}
