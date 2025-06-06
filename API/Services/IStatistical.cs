using API.ViewModel;

namespace API.Services
{
    public interface IStatistical
    {
        Task<IEnumerable<StudentByClassDTO>> GetStudentByClass();
        Task<IEnumerable<StudentByGenderDTO>> GetStudentByGender();
        Task<IEnumerable<StudentByAddressDTO>> GetStudentByAddress();
        Task<IEnumerable<StudentByStatusDTO>> GetStudentByStatus();
       // Task<IEnumerable<RoomStudy>> GetRoomStudies();
    }
}
