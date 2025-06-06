using API.Models;

namespace API.Services
{
    public interface ISubject
    {
        Task<List<Subject>> Getall();
    }
}
