using API.Data;
using API.ViewModel;

namespace API.Services.Repositories
{
    public class StatisticalRepos : IStatistical
    {
        private readonly AduDbcontext _context;
        public StatisticalRepos(AduDbcontext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<StudentByClassDTO>> GetStudentByClass()
        {
            var students =  _context.Classes
                 .Select(c => new StudentByClassDTO
                 {
                     ClassName = c.NameClass,
                     Total = c.Students.Count,
                 }).ToList();
            return students;
        }
    }
}
