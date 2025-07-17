using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface ITeachingRegistrationRepos
    {
        //Task<List<Class>> GetClasses(string userName);
        //Task<List<DayOfWeekk>> GetDays();
        //Task<List<StudyShift>> GetStudyShifts();
        //Task<List<Room>> GetRooms(int dayId, int shiftId, DateTime start, DateTime end);

        Task<string> CanRegister(Guid teacherId, int classId, int dayId, int shiftId, int semesterId, DateTime start, DateTime end);
        Task<string> RegisterTeaching(Guid teacherId, int classId, int semesterId, int dayId, int shiftId, DateTime start, DateTime end);

        Task<List<TeachingRegistrationVMD>> GetTeacherRegister(string? userName, bool isAdmin);
        Task<string> ConfirmTeachingRegistration(int registrationId);
        //Task<bool> RegisterSchedule(Guid teacherId, int classId, int dayId, int shiftId, int roomId, DateTime start, DateTime end);
    }
}
