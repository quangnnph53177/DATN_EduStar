using API.Models;

namespace API.Services
{
    public interface ISemesterRepos
    {
        Task<Semester?> GetCurrentSemester();
    }
}
