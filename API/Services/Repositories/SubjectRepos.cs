using API.Data;
using API.Models;

namespace API.Services.Repositories
{
    public class SubjectRepos : ISubject
    {
        private readonly AduDbcontext _context;
        public SubjectRepos(AduDbcontext context)
        {
            _context = context;
        }
        public async Task<List<Subject>> Getall()
        {
            var sub =await _context.Subjects.ToListAsync();
            return sub;
        }
    }
}
