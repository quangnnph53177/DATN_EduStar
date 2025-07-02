using API.ViewModel;

namespace API.Services
{
    public interface IAttendance
    {
        Task CreateSession(CreateAttendanceSessionViewModel model);
        Task<List<StudentAttendanceViewModel>> GetStudentsForAttendance(int id);
        Task<(bool match, string message)> Recognize(string base64, int attendanceId);
        Task<List<AttendancesessionViewModel>> GetAllSessions(); // Admin or Lecturer
        Task<List<StudentCheckInSessionViewModel>> GetSessionsForStudent(Guid studentId); // For student
    }
}
