using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class SemesterRepos : ISemesterRepos
    {
        private readonly AduDbcontext _context;

        public SemesterRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task<Semester?> GetCurrentSemester()
        {
            return await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
        }
    }
}
