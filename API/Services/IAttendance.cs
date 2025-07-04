using API.ViewModel;

namespace API.Services
{
    public interface IAttendance
    {
        Task CreateSession(CreateAttendanceViewModel model);
        Task<List<IndexAttendanceViewModel>> GetAllSession();
        Task<IndexAttendanceViewModel> GetByIndex(int attendance);
        Task<bool> CheckInStudent(CheckInDto dto);
        Task<List<StudentAttendanceHistory>> GetHistoryForStudent(Guid studentId);
    }
}
