using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IClassRepos
    {
       Task<IEnumerable<ClassViewModel>> GetAllClassesAsync();
        Task<ClassViewModel> GetClassByIdAsync(int id);
        Task AddClassAsync(ClassViewModel classViewModel);
        Task UpdateClassAsync(int id, ClassViewModel classViewModel);
        Task DeleteClassAsync(int id);
    }
}
