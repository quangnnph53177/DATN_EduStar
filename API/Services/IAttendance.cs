using API.ViewModel;

namespace API.Services
{
    public interface IAttendance
    {
        Task CreateSession(CreateAttendanceViewModel model);
        Task<List<IndexAttendanceViewModel>> GetAllSession();
        Task<List<TeacherClassViewModel>> GetTeacherClasses(Guid teacherId);
        Task<IndexAttendanceViewModel> GetByIndex(int attendance);
        Task<bool> CheckInStudent(CheckInDto dto);
        Task<List<StudentAttendanceHistory>> GetHistoryForStudent(Guid studentId);
        Task<List<IndexAttendanceViewModel>> Search(int? classId, int? studyShiftid, int? roomid, int? subjectid);

        Task<bool> CheckInByFace(int attendanceId, Guid studentId, byte[] faceImageBytes);
        Task<List<IndexAttendanceViewModel>> GetActiveSessionsForStudent(Guid studentId);
    }
}