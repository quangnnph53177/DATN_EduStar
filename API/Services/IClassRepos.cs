using API.Models;

namespace API.Services
{
    public interface IClassRepos
    {
        Task<List<Class>> GetAllClassesAsync();
        Task<Class> GetClassByIdAsync(int id);
        Task CreateClassAsync(Class newClass);
        Task UpdateClassAsync(int id,Class updatedClass);
        Task DeleteClassAsync(int id);
    }
}
