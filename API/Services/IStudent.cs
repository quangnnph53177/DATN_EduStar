using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IStudent
    {
        Task<List<StudentViewModels>> GetAllStudents();
        Task<StudentViewModels> GetById(Guid Id);
        Task UpdatebyBoss(StudentViewModels model);// cập nhập của admin
        Task UpdateByBeast(StudentViewModels model);//cập nhập sủa sinh viên
        Task<bool> KhoaMoSinhVienAsync(Guid Id);
        Task<bool> DeleteStudent(Guid Id);
        Task SendNotificationtoClass(int classId, string subject);
        Task<List<StudentViewModels>> Search(string? Studencode, string? fullName, string? username, string? email,bool? gender, bool? status);
        Task<List<StudentViewModels>> GetStudentsByClass(int Id);
        Task<byte[]> ExportStudentsToExcel(List<StudentViewModels> model);
        Task<List<Auditlog>> GetAuditLogs();
        Task SendAsync(string subject, string message, Guid id);
    }
}
