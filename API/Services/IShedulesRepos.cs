using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IShedulesRepos
    {
        Task<List<Schedule>> GetAll();
        Task<List<SchedulesViewModel>> GetByStudent(Guid Id);
        Task AutogenerateSchedule();
    }
}
