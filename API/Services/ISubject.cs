using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface ISubject
    {
        Task<List<SubjectViewModel>> Getall();
        Task<SubjectViewModel> GetById(int id);
        Task<Subject> CreateSubject(SubjectViewModel sub);
        Task<bool> UpdateSubject(SubjectViewModel subject);
        Task<List<SubjectViewModel>> Search(string? subjectName, int? numberofCredit, string? subcode, bool? status);
        Task<bool> OpenAndClose(int Id);
        Task<bool> DeleteSubject(int Id);
    }
}
