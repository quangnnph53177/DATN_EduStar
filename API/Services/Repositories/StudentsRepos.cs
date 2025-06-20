using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Net.WebSockets;
using System.Security.Claims;

namespace API.Services.Repositories
{
    public class StudentsRepos : IStudent
    {
        private readonly AduDbcontext _context;
        private readonly IHttpContextAccessor _accessor;
        private readonly IEmailRepos _email;
        
        public StudentsRepos(AduDbcontext context, IHttpContextAccessor accessor, IEmailRepos emailRepos )
        {
            _context = context;
            _accessor = accessor;
            _email = emailRepos;
        }
        private Guid GetCurrentUserId()
        {
            var userIdStr = _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
        }
        public async Task<bool> DeleteStudent(Guid id)
        {
            var sv = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.StudentsInfor)
                .ThenInclude(si => si.Classes)
                .Include(r=>r.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (sv == null)
            {
                Console.WriteLine("Không tìm thấy người dùng.");
                return false;
            }

            // Kiểm tra có phải sinh viên (RoleId = 3)
            if (sv.Roles.FirstOrDefault().Id != 3)
            {
                Console.WriteLine("Chỉ được xóa người dùng là sinh viên.");
                return false;
            }

            if (sv.Statuss == true)
            {
                Console.WriteLine("Người dùng đang hoạt động, không thể xóa.");
                return false;
            }

            if (sv.StudentsInfor?.Classes != null && sv.StudentsInfor.Classes.Any())
            {
                Console.WriteLine("Người dùng đã được phân lớp, không thể xóa.");
                return false;
            }

            if (sv.UserProfile != null)
                _context.UserProfiles.Remove(sv.UserProfile);

            if (sv.StudentsInfor != null)
                _context.StudentsInfors.Remove(sv.StudentsInfor);

            _context.Users.Remove(sv);
            await _context.SaveChangesAsync();

            Console.WriteLine("Xóa người dùng thành công.");
            return true;
        }

        public async Task<byte[]> ExportStudentsToExcel(List<StudentViewModels> model)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Students");
            worksheet.Cells[1, 1].Value = "STT";
            worksheet.Cells[1, 2].Value = "Họ tên";
            worksheet.Cells[1, 3].Value = "Tên đăng nhập";
            worksheet.Cells[1, 4].Value = "Mã sinh viên";
            worksheet.Cells[1, 5].Value = "Email";
            worksheet.Cells[1, 6].Value = "Số điện thoại";
            worksheet.Cells[1, 7].Value = "Ngày sinh";
            worksheet.Cells[1, 8].Value = "Giới tính";
            worksheet.Cells[1, 9].Value = "Địa chỉ";
            worksheet.Cells[1, 10].Value = "Trạng thái";
            int row = 2;
            int stt = 1;
            foreach (var item in model)
            {
                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = item.FullName;
                worksheet.Cells[row, 3].Value = item.UserName;
                worksheet.Cells[row, 4].Value = item.StudentCode;
                worksheet.Cells[row, 5].Value = item.Email;
                worksheet.Cells[row, 6].Value = item.PhoneNumber;
                worksheet.Cells[row, 7].Value = item.Dob;
                worksheet.Cells[row, 8].Value = item.Gender;
                worksheet.Cells[row, 9].Value = item.Address;
                worksheet.Cells[row, 10].Value = item.Status;
                row++;
            }
            return package.GetAsByteArray();
        }

        public async Task<List<StudentViewModels>> GetAllStudents()
        {
            var lstSv = await _context.StudentsInfors
                .Include(s => s.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(s => s.User)
                    .ThenInclude(u => u.Roles)
                .Where(s => s.User.Roles.Any(r => r.Id == 3))
                .AsSplitQuery()
                .ToListAsync();
          var student = lstSv.Select(u=> new StudentViewModels
          {
              id = u.UserId,
              FullName = u.User?.UserProfile?.FullName ?? "N/A",
              UserName = u.User?.UserName ?? "N/A",
              Email = u.User?.Email ?? "N/A",
              PhoneNumber = u.User?.PhoneNumber ?? "N/A",
              StudentCode = u.StudentsCode ?? "N/A",
              Gender = u.User?.UserProfile?.Gender ?? false,
              Avatar = u.User?.UserProfile?.Avatar ?? "N/A",
              Address = u.User?.UserProfile?.Address ?? "N/A",
              Dob = u.User?.UserProfile?.Dob,
              Status = u.User?.Statuss ?? false
          }).ToList();
          return student;
        }

        public async Task<StudentViewModels> GetById(Guid Id)
        {
            var inforvs = await _context.StudentsInfors
                .Include(u => u.User)
                .ThenInclude(p => p.UserProfile)
                .Include(c => c.Classes)
                .ThenInclude(s => s.Subject)
                .FirstOrDefaultAsync(d => d.UserId == Id);
           
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

            var classEntity = await _context.Classes
        .Include(c => c.Students)
            .ThenInclude(si => si.User)
                .ThenInclude(u => u.UserProfile)
        .FirstOrDefaultAsync(c => c.Id == Id);

            if (classEntity == null || classEntity.Students == null)
                return new List<StudentViewModels>();

            var students = classEntity.Students.Select(s => new StudentViewModels
            {
                id = s.UserId,
                UserName = s.User?.UserName,
                StudentCode = s.StudentsCode,
                FullName = s.User?.UserProfile?.FullName,
                Email = s.User?.Email,
                PhoneNumber = s.User?.PhoneNumber,
                Gender = s.User?.UserProfile?.Gender,
                Address = s.User?.UserProfile?.Address,
                Dob = s.User?.UserProfile?.Dob,
                Status = s.User?.Statuss ?? false,
                CVMs = new List<ClassViewModel>
                {
                    new ClassViewModel
                    {
                        ClassName = classEntity.NameClass
                    }
                }
            }).ToList();

            return students;
        }

        public async Task<bool> KhoaMoSinhVienAsync(Guid Id)
        {
            var sv = await _context.Users.FindAsync(Id);
            if (sv == null) return false;
            sv.Statuss = !(sv.Statuss ?? true);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<StudentViewModels>> Search(string? Studencode, string? fullName, string? username, string? email, bool? gender)
        {
            var query = _context.Users.Include(u => u.UserProfile).Include(s => s.StudentsInfor).AsSplitQuery();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                query = query.Where(f => f.UserProfile.FullName.ToLower().Contains(fullName));
            }
            if (!string.IsNullOrWhiteSpace(Studencode))
            {
                query = query.Where(f => f.StudentsInfor.StudentsCode.ToLower().Contains(Studencode));
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(f => f.UserName.ToLower().Contains(username));
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(f => f.Email.ToLower().Contains(email));
            }
            if (gender.HasValue)
            { 
                query =query.Where(g=>g.UserProfile.Gender==gender.Value);
            }

                var result = await query.OrderBy(s => s.StudentsInfor.StudentsCode).Select(u => new StudentViewModels
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

           

            var olddata = JsonConvert.SerializeObject(new
            {
                upinsv.UserName,
                upinsv.Email,
                upinsv.PhoneNumber,
                upinsv.UserProfile.Avatar,
                upinsv.UserProfile.Address,
                upinsv.UserProfile.Dob,
            });

            // Cập nhật thông tin
            upinsv.UserName = model.UserName;
            upinsv.PassWordHash = model.PassWordHash;
            upinsv.Email = model.Email;
            upinsv.PhoneNumber = model.PhoneNumber;
            upinsv.UserProfile.Avatar = model.Avatar;
            upinsv.UserProfile.Address = model.Address;
            upinsv.UserProfile.Dob = model.Dob;

            var newdata = JsonConvert.SerializeObject(new
            {
                upinsv.UserName,
                upinsv.Email,
                upinsv.PhoneNumber,
                upinsv.UserProfile.Avatar,
                upinsv.UserProfile.Address,
                upinsv.UserProfile.Dob,
            });

            _context.Users.Update(upinsv);

            // Audit log
            var currentUserId = GetCurrentUserId();
            Guid? performeBy = currentUserId != Guid.Empty ? currentUserId : null;

            var audit = new Auditlog
            {
                Userid = upinsv.Id,
                PerformeBy = performeBy,
                NewData = newdata,
                OldData = olddata,
                Active = "UpdateSV",
                Timestamp = DateTime.Now,
            };

            _context.Auditlogs.Add(audit);

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
            var olddataClass = inforvs.Classes.Select(u => new ClassViewModel
            {
                ClassName = u.NameClass,
                SubjectName = u.Subject.SubjectName,
                Semester = u.Semester,
                YearSchool = u.YearSchool ?? 0,
                NumberOfCredits = u.Subject.NumberOfCredits ?? 0
            }).ToList();
            var olddata = JsonConvert.SerializeObject(new
            {
                inforvs.User.UserName,
                inforvs.User.Email,
                inforvs.User.PhoneNumber,
                inforvs.User.UserProfile.FullName,
                inforvs.User.UserProfile.Gender,
                inforvs.User.UserProfile.Avatar,
                inforvs.User.UserProfile.Address,
                inforvs.User.UserProfile.Dob,
                Classes = olddataClass
            });
            inforvs.User.UserName = model.UserName;
            //inforvs.User.PassWordHash = model.PassWordHash;
            inforvs.User.Email = model.Email;
            inforvs.User.PhoneNumber = model.PhoneNumber;
            inforvs.User.UserProfile.FullName = model.FullName;
            inforvs.User.UserProfile.Gender = model.Gender;
            inforvs.User.UserProfile.Address = model.Address;
            //if (avatarFile != null&& avatarFile.Length > 0)
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
            var newataClass = inforvs.Classes.Select(u => new ClassViewModel
            {
                ClassName = u.NameClass,
                SubjectName = u.Subject.SubjectName,
                Semester = u.Semester,
                YearSchool = u.YearSchool ?? 0,
                NumberOfCredits = u.Subject.NumberOfCredits ?? 0
            }).ToList();
            var newData = JsonConvert.SerializeObject(new
            {
                inforvs.User.UserName,
                inforvs.User.Email,
                inforvs.User.PhoneNumber,
                inforvs.User.UserProfile.FullName,
                inforvs.User.UserProfile.Gender,
                inforvs.User.UserProfile.Avatar,
                inforvs.User.UserProfile.Address,
                inforvs.User.UserProfile.Dob,
                Classes = olddataClass
            });
            await _context.SaveChangesAsync();
            var currentUserId = GetCurrentUserId();
            Guid? performeBy = currentUserId != Guid.Empty ? currentUserId : null;
            var audit = new Auditlog
            {
                Userid = inforvs.UserId,
                PerformeBy = performeBy,
                NewData = newData,
                OldData = olddata,
                Active = "UpdateSVByBoss",
                Timestamp = DateTime.Now,
            };
            _context.Auditlogs.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Auditlog>> GetAuditLogs()
        {
            var audit = _context.Auditlogs.Include(a=>a.PerformeByNavigation).Include(x => x.User).ToList();
            return audit;
        }

        public async Task SendNotificationtoClass(int classId, string subject)
        {
            var in4class = await _context.Classes
                 .Include(s => s.Subject)
                 .FirstOrDefaultAsync(c => c.Id == classId);
            var lststudeninclass =await _context.Classes
                .Where(c => c.Id == classId)
                 .Include(s => s.Students)
                 .ThenInclude(u => u.User)
                 .Select(d => d.Students.FirstOrDefault().User.Email).ToListAsync();
            string message = $"Xin chào các bạn sinh viên,\n\n" +
                     $"Đây là thông báo về lớp học:\n" +
                     $"- Tên lớp: {in4class.NameClass}\n" +
                     $"- Môn học: {in4class.Subject.SubjectName}\n" +
                     $"- Học kỳ: {in4class.Semester}\n" +
                     $"- Năm học: {in4class.YearSchool}\n\n" +
                     $"Vui lòng theo dõi thông tin và chuẩn bị học tập.\n\n" +
                     $"Trân trọng,\nBan Quản Trị";
            
            foreach (var email in lststudeninclass)
            {
                await _email.SendEmail(email, subject, message);
            }
        }
    }
}
