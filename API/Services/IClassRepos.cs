using API.Models;
using API.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IClassRepos
    {
        // Đã sửa kiểu trả về từ ClassDetailViewModel thành ClassViewModel
        Task<IEnumerable<ClassViewModel>> GetAllClassesAsync();
        Task<ClassViewModel> GetClassByIdAsync(int id);

        Task AddClassAsync(ClassCreateViewModel classViewModel);
        Task UpdateClassAsync(int id, ClassUpdateViewModel classViewModel);
        Task DeleteClassAsync(int id);

        // Đã sửa kiểu trả về từ ClassDetailViewModel thành ClassViewModel
        Task<IEnumerable<ClassViewModel>> SearchClassesAsync(int id);

        Task<bool> AssignStudentToClassAsync(AssignStudentsRequest request);
        Task<bool> StudentRegisterClassAsync(AssignStudentsRequest request);
        Task<bool> RemoveStudentFromClassAsync(int classId, Guid studentId);
        Task<IEnumerable<ClassChangeViewModel>> GetClassHistoryAsync(int classId);
    }
}