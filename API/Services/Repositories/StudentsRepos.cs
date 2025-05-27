using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class StudentsRepos : IStudent
    {
        private readonly AduDbcontext _context;
        public StudentsRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteStudent(Guid id)
        {
            var sv = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.StudentsInfor)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (sv == null)
                return false;

            // Không cho xoá nếu sinh viên còn đang hoạt động
            if (sv.Statuss == true)
                return false;


            if (sv.StudentsInfor != null && sv.StudentsInfor.Classes != null)
                return false;

            // Xoá phụ thuộc
            if (sv.UserProfile != null)
                _context.UserProfiles.Remove(sv.UserProfile);

            if (sv.StudentsInfor != null)
                _context.StudentsInfors.Remove(sv.StudentsInfor);

            _context.Users.Remove(sv);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<byte[]> ExportStudentsToExcel(List<StudentViewModels> model)
        {
            throw new NotImplementedException();
        }

        public async Task<List<StudentsInfor>> GetAllStudents()
        {
            var lstSv = await _context.StudentsInfors
                .Include(s => s.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(s => s.User)
                    .ThenInclude(u => u.Roles)
                .Where(s => s.User.Roles.Any(r => r.Id == 3))
                .AsSplitQuery()
                .ToListAsync();
            return lstSv;
        }

        public async Task<StudentViewModels> GetById(Guid Id)
        {
            var inforvs = await _context.StudentsInfors
                .Include(u => u.User)
                .ThenInclude(p => p.UserProfile)
                .Include(c => c.Classes)
                .ThenInclude(s => s.Subject)
                .FirstOrDefaultAsync(d=>d.UserId==Id);
            var cls = inforvs.Classes.FirstOrDefault();
            var model = new StudentViewModels()
            {

                id = inforvs.UserId,
                FullName = inforvs.User.UserProfile.FullName,
                UserName = inforvs.User.UserName,
                Email = inforvs.User.Email,
                PhoneNumber = inforvs.User.PhoneNumber,
                StudentCode = inforvs.StudentsCode,
                Gender = inforvs.User.UserProfile.Gender,
                Avatar = inforvs.User.UserProfile.Avatar,
                Address = inforvs.User.UserProfile.Address,
                Dob = inforvs.User.UserProfile.Dob,
                Status = inforvs.User.Statuss.GetValueOrDefault(),
                CVMs = inforvs.Classes?.Select(u => new ClassViewModel
                {
                    ClassName = u.NameClass,
                    SubjectName = u.Subject.SubjectName,
                    Semester = u.Semester,
                    YearSchool = u.YearSchool ?? 0,
                    NumberOfCredits = u.Subject.NumberOfCredits ?? 0
                }).ToList()
            };
            return model;
        }

        public async Task<List<StudentViewModels>> GetStudentsByClass(int Id)
        {
            
            var students = await _context.Classes.Where(x => x.Id == Id)
                .Include(s => s.Students)
                .ThenInclude(u => u.User)
                .ThenInclude(p => p.UserProfile)
                .Select(y => new StudentViewModels
                {
                   UserName = y.Students.FirstOrDefault().User.UserName,
                   StudentCode = y.Students.FirstOrDefault().StudentsCode,
                   FullName = y.Students.FirstOrDefault().User.UserProfile.FullName,
                   Email = y.Students.FirstOrDefault().User.Email,
                   PhoneNumber = y.Students.FirstOrDefault().User.PhoneNumber,
                   Gender = y.Students.FirstOrDefault().User.UserProfile.Gender,
                   Address = y.Students.FirstOrDefault().User.UserProfile.Address,
                   Dob = y.Students.FirstOrDefault().User.UserProfile.Dob,
                   Status = y.Students.FirstOrDefault().User.Statuss.GetValueOrDefault(),
                   CVMs = new List<ClassViewModel>
                   {
                      new ClassViewModel
                      {
                          ClassName = y.NameClass
                      }
                   }
                }).ToListAsync();
                return students;
        }

        public async Task<bool> KhoaMoSinhVienAsync(Guid Id)
        {
            var sv = await _context.Users.FindAsync(Id);
            if (sv == null) return false;
            sv.Statuss = !(sv.Statuss?? true);
            await _context.SaveChangesAsync();
            return true ;
        }

        public async Task<List<StudentViewModels>> Search(string Studencode, string fullName, string username, string email)
        {
            var  query = _context.Users.Include(u=>u.UserProfile).Include(s=>s.StudentsInfor).AsSplitQuery();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                query = query.Where(f => f.UserProfile.FullName.Contains(fullName));
            }
            if (!string.IsNullOrWhiteSpace(Studencode))
            {
                query = query.Where(f => f.StudentsInfor.StudentsCode.Contains(Studencode));
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(f => f.UserName.Contains(username));
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(f => f.Email.Contains(email));
            }

            var result = await query.OrderBy(s=>s.StudentsInfor.StudentsCode).Select(u => new StudentViewModels
            {
                id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                StudentCode = u.StudentsInfor.StudentsCode,
                FullName = u.UserProfile.FullName,
                Gender = u.UserProfile.Gender,
                Avatar = u.UserProfile.Avatar,
                Address = u.UserProfile.Address,
                Dob = u.UserProfile.Dob,
                Status = u.Statuss ?? true

            }).ToListAsync();
            return result;
        

        }

        public async Task UpdateByBeast(StudentViewModels model)
        {
            var upinsv = await _context.Users
                  .Include(p => p.UserProfile)
                  .FirstOrDefaultAsync(d => d.Id == model.id);
           
            upinsv.PassWordHash = model.PassWordHash;
            upinsv.Email = model.Email;
            upinsv.PhoneNumber = model.PhoneNumber;
            upinsv.UserProfile.Avatar = model.Avatar;
            upinsv.UserProfile.Address = model.Address;
            upinsv.UserProfile.Dob = model.Dob;
            _context.Users.Update(upinsv);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatebyBoss(StudentViewModels model)
        {
            var inforvs = await _context.StudentsInfors
                 .Include(u => u.User)
                 .ThenInclude(p => p.UserProfile)
                 .Include(c => c.Classes)
                 .ThenInclude(s => s.Subject)
                 .FirstOrDefaultAsync(d => d.UserId == model.id);

            var cls = inforvs.Classes.FirstOrDefault();
            inforvs.User.UserName = model.UserName;
            inforvs.User.PassWordHash = model.PassWordHash;
            inforvs.User.Email = model.Email;
            inforvs.User.PhoneNumber = model.PhoneNumber;
            inforvs.User.UserProfile.FullName = model.FullName;
            inforvs.User.UserProfile.Gender = model.Gender;
            inforvs.User.UserProfile.Address = model.Address;
            inforvs.User.UserProfile.Avatar = model.Avatar;
            inforvs.User.UserProfile.Dob = model.Dob;
            inforvs.Classes?.Select(u => new ClassViewModel
            {
                ClassName = u.NameClass,
                SubjectName = u.Subject.SubjectName,
                Semester = u.Semester,
                YearSchool = u.YearSchool ?? 0,
                NumberOfCredits = u.Subject.NumberOfCredits ?? 0
            }).ToList();
            _context.StudentsInfors.Update(inforvs);
            await _context.SaveChangesAsync();
        }
    }
}
