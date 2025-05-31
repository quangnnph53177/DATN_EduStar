using API.ViewModel;

namespace API.Services
{
    public interface IStatistical
    {
        Task<IEnumerable<StudentByClassDTO>> GetStudentByClass();
    }
}
