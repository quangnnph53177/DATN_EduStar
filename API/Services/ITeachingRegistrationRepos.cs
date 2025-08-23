using API.Models;
using API.ViewModel;
using static API.Models.TeachingRegistration;

namespace API.Services
{
    public interface ITeachingRegistrationRepos
    {
        //Task<string> CanRegister(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end);
        //Task<string> RegisterTeaching(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end);
        //Task<List<TeachingRegistrationVMD>> GetTeacherRegister(string? userName, bool isAdmin);
        //Task<string> ConfirmTeachingRegistration(int registrationId);
        Task<List<SchedulesViewModel>> GetAvailableSchedules(Guid? teacherID = null);
        Task<string> CanRegister(Guid teacherId, int schedulesID);
        Task<string> TeacherRegistration(Guid teacherId, int schedulesID);
        Task<List<TeachingRegistrationVM>> GetTeacherRegistrations(Guid teacherId);
        Task<List<AdminResgistration>> GetAllRegistration(string? Status = null);
        Task<string> ApproveRegistration(int registrationId, ApprovedStatus approve, Guid adminId);
        Task<string> CancelRegistration(int registrationId, Guid teacherId);
    }
}
