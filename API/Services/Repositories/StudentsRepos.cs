using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;

namespace API.Services.Repositories
{
    public class StudentsRepos : IStudent
    {
        private readonly AduDbcontext _context;
        private readonly IHttpContextAccessor _accessor;
        private readonly IEmailRepos _email;

        public StudentsRepos(AduDbcontext context, IHttpContextAccessor accessor, IEmailRepos emailRepos)
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
                .Include(si => si.Schedules)
                .Include(r => r.Roles)
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

            if (sv.Schedules != null && sv.Schedules.Any())
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
                .OrderByDescending(s => s.StudentsCode)
                .ToListAsync();
            var student = lstSv.Select(u => new StudentViewModels
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
            var inforsv = await _context.StudentsInfors
                 .Include(c => c.User)
                     .ThenInclude(c => c.UserProfile)
                 .Include(c => c.User)
                     .ThenInclude(c => c.Roles)
                 .Include(c => c.ScheduleStudents)
                     .ThenInclude(c => c.Schedule)
                         .ThenInclude(c => c.Subject)
                 .Include(c => c.ScheduleStudents)
                     .ThenInclude(c => c.Schedule)
                         .ThenInclude(c => c.Room)
                 .Include(c => c.ScheduleStudents)
                     .ThenInclude(c => c.Schedule)
                         .ThenInclude(c => c.StudyShift)
                 .Include(c => c.ScheduleStudents)
                     .ThenInclude(c => c.Schedule)
                         .ThenInclude(c => c.ScheduleDays)
                             .ThenInclude(c => c.DayOfWeekk)
                 .FirstOrDefaultAsync(d => d.UserId == Id);

            if (inforsv == null)
                throw new Exception("Không tìm thấy thông tin sinh viên");

            var model = new StudentViewModels
            {
                id = inforsv.UserId,
                FullName = inforsv.User?.UserProfile?.FullName ?? "N/A",
                UserName = inforsv.User?.UserName ?? "N/A",
                Email = inforsv.User?.Email ?? "N/A",
                PhoneNumber = inforsv.User?.PhoneNumber ?? "N/A",
                StudentCode = inforsv.StudentsCode ?? "N/A",
                Gender = inforsv.User?.UserProfile?.Gender,
                Avatar = inforsv.User?.UserProfile?.Avatar,
                Address = inforsv.User?.UserProfile?.Address,
                Dob = inforsv.User?.UserProfile?.Dob,
                RoleId = inforsv.User?.Roles?.FirstOrDefault()?.Id ?? 0,
                Status = inforsv.User?.Statuss ?? false,
                CVMs = inforsv.ScheduleStudents?
                    .Where(ss => ss.Schedule != null)
                    .Select(ss => new ClassViewModel
                    {
                        schedulesId = ss.Schedule.Id,
                        ClassName = ss.Schedule.ClassName ?? "N/A",
                        SubjectName = ss.Schedule.Subject?.SubjectName ?? "N/A",
                        RoomCode = ss.Schedule.Room?.RoomCode ?? "N/A",
                        StudyShiftName = ss.Schedule.StudyShift?.StudyShiftName ?? "N/A",
                        starttime = ss.Schedule.StudyShift?.StartTime,
                        endtime = ss.Schedule.StudyShift?.EndTime,
                        WeekDay = ss.Schedule.ScheduleDays != null
                            ? string.Join(", ", ss.Schedule.ScheduleDays
                                    .Where(sd => sd.DayOfWeekk != null)
                                    .Select(sd => sd.DayOfWeekk.Weekdays.ToString()))
                            : "N/A",
                        TeacherName = ss.Schedule.User?.UserProfile?.FullName ?? "N/A"
                    }).ToList()
            };

            return model;

        }



        public async Task<List<StudentViewModels>> GetStudentsByClass(int Id)
        {

            var classEntity = await _context.ScheduleStudentsInfors
            .Include(c => c.Student)
            .ThenInclude(si => si.User)
            .ThenInclude(u => u.UserProfile)
            .Where(ss => ss.Schedule.Id == Id).ToListAsync();

            var students = classEntity.Select(ss => new StudentViewModels
            {
                id = ss.Student.UserId,
                UserName = ss.Student.User?.UserName,
                StudentCode = ss.Student.StudentsCode,
                FullName = ss.Student.User?.UserProfile?.FullName,
                Email = ss.Student.User?.Email,
                PhoneNumber = ss.Student.User?.PhoneNumber,
                Gender = ss.Student.User?.UserProfile?.Gender,
                Address = ss.Student.User?.UserProfile?.Address,
                Dob = ss.Student.User?.UserProfile?.Dob,
                Status = ss.Student.User?.Statuss ?? false,
                CVMs = new List<ClassViewModel>
        {
            new ClassViewModel
            {
                ClassName = ss.Schedule.ClassName
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

        public async Task<List<StudentViewModels>> Search(string? Studencode, string? fullName, string? username, string? email, bool? gender, bool? status)
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
                query = query.Where(g => g.UserProfile.Gender == gender.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(s => s.Statuss == status.Value);
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

        public async Task UpdateByBeast(StudentViewModels model, IFormFile? imgFile = null)
        {
            var upinsv = await _context.Users
                .Include(p => p.UserProfile)
                .FirstOrDefaultAsync(d => d.Id == model.id);

            if (upinsv == null)
                throw new Exception("Không tìm thấy sinh viên.");

            var olddata = JsonConvert.SerializeObject(new
            {
                upinsv.UserName,
                upinsv.Email,
                upinsv.PhoneNumber,
                Avatar = upinsv.UserProfile?.Avatar,
                Address = upinsv.UserProfile?.Address,
                Dob = upinsv.UserProfile?.Dob,
            });

            // Cập nhật thông tin cơ bản
            upinsv.UserName = model.UserName;
            if (!string.IsNullOrWhiteSpace(model.PassWordHash))
            {
                upinsv.PassWordHash = model.PassWordHash; // Nên hash lại password nếu cần
            }
            upinsv.Email = model.Email;
            upinsv.PhoneNumber = model.PhoneNumber;

            // Đảm bảo UserProfile tồn tại
            if (upinsv.UserProfile == null)
            {
                upinsv.UserProfile = new UserProfile { UserId = upinsv.Id };
            }

            // Xử lý avatar nếu có file mới
            if (imgFile != null && imgFile.Length > 0)
            {
                try
                {
                    var avatarPath = await SaveAvatar(imgFile, upinsv.UserProfile.Avatar);
                    upinsv.UserProfile.Avatar = avatarPath;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Lỗi khi lưu avatar: {ex.Message}");
                }
            }

            // Cập nhật các thông tin khác
            upinsv.UserProfile.Address = model.Address;
            upinsv.UserProfile.Dob = model.Dob;

            var newdata = JsonConvert.SerializeObject(new
            {
                upinsv.UserName,
                upinsv.Email,
                upinsv.PhoneNumber,
                Avatar = upinsv.UserProfile?.Avatar,
                Address = upinsv.UserProfile?.Address,
                Dob = upinsv.UserProfile?.Dob,
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
        private async Task<string> SaveAvatar(IFormFile imgFile, string? oldPath = null)
        {
            var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(imgFile.FileName).ToLowerInvariant();

            if (!validImageFormats.Contains(ext))
                throw new Exception("Định dạng ảnh không hợp lệ. Chỉ hỗ trợ jpg, jpeg, png");

            var avatarDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(avatarDir))
                Directory.CreateDirectory(avatarDir);

            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                var fullOldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
                if (File.Exists(fullOldPath))
                    File.Delete(fullOldPath);
            }

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(avatarDir, uniqueName);

            await using var stream = new FileStream(savePath, FileMode.Create);
            await imgFile.CopyToAsync(stream);

            return $"/images/avatars/{uniqueName}";
        }

        // Sửa method UpdatebyBoss trong StudentsRepos
        public async Task UpdatebyBoss(StudentViewModels model)
        {
            await Validate(model, isupdate: true);
            var inforvs = await _context.Users
                 .Include(u => u.UserProfile)
                 .Include(r => r.Roles)
                 .Include(p => p.StudentsInfor)
                 .ThenInclude(c => c.ScheduleStudents).Include(c => c.Schedules)
                .ThenInclude(s => s.Subject)
                .ThenInclude(S => S.Classes).ThenInclude(r => r.Room)
                .ThenInclude(S => S.Schedules).ThenInclude(r => r.StudyShift)
                .ThenInclude(S => S.Schedules).ThenInclude(s => s.ScheduleDays).ThenInclude(r => r.DayOfWeekk)
                 .FirstOrDefaultAsync(d => d.Id == model.id);

            if (inforvs == null)
                throw new Exception("Không tìm thấy người dùng.");

            // Đảm bảo UserProfile tồn tại
            if (inforvs.UserProfile == null)
            {
                inforvs.UserProfile = new UserProfile { UserId = inforvs.Id };
            }

            var olddataClass = inforvs.Schedules?.Select(u => new ClassViewModel
            {
                ClassName = u.ClassName,
                SubjectName = u.Subject?.SubjectName,
                RoomCode = u.Room?.RoomCode,
                StudyShiftName = u.StudyShift?.StudyShiftName,
                starttime = u.StudyShift?.StartTime,
                endtime = u.StudyShift?.EndTime,
                WeekDay = u.ScheduleDays?.FirstOrDefault()?.DayOfWeekk?.Weekdays.ToString(),
                TeacherName = u.User?.UserProfile?.FullName
            }).ToList() ?? new List<ClassViewModel>();

            var olddata = JsonConvert.SerializeObject(new
            {
                inforvs.UserName,
                inforvs.Email,
                inforvs.PhoneNumber,
                FullName = inforvs.UserProfile.FullName,
                Gender = inforvs.UserProfile.Gender,
                Avatar = inforvs.UserProfile.Avatar,
                Address = inforvs.UserProfile.Address,
                Dob = inforvs.UserProfile.Dob,
                Classes = olddataClass
            });

            // Cập nhật thông tin User
            inforvs.UserName = model.UserName;
            inforvs.Email = model.Email;
            inforvs.PhoneNumber = model.PhoneNumber;

            // Cập nhật thông tin UserProfile
            inforvs.UserProfile.FullName = model.FullName;
            inforvs.UserProfile.Gender = model.Gender;
            inforvs.UserProfile.Address = model.Address;
            inforvs.UserProfile.Avatar = model.Avatar;
            inforvs.UserProfile.Dob = model.Dob;

            _context.Users.Update(inforvs);

            var newataClass = inforvs.Schedules?.Select(u => new ClassViewModel
            {
                ClassName = u.ClassName,
                SubjectName = u.Subject?.SubjectName,
                RoomCode = u.Room?.RoomCode,
                StudyShiftName = u.StudyShift?.StudyShiftName,
                starttime = u.StudyShift?.StartTime,
                endtime = u.StudyShift?.EndTime,
                WeekDay = u.ScheduleDays?.FirstOrDefault()?.DayOfWeekk?.Weekdays.ToString(),
                TeacherName = u.User?.UserProfile?.FullName
            }).ToList() ?? new List<ClassViewModel>();

            var newData = JsonConvert.SerializeObject(new
            {
                inforvs.UserName,
                inforvs.Email,
                inforvs.PhoneNumber,
                FullName = inforvs.UserProfile.FullName,
                Gender = inforvs.UserProfile.Gender,
                Avatar = inforvs.UserProfile.Avatar,
                Address = inforvs.UserProfile.Address,
                Dob = inforvs.UserProfile.Dob,
                Classes = newataClass
            });

            await _context.SaveChangesAsync();

            var currentUserId = GetCurrentUserId();
            Guid? performeBy = currentUserId != Guid.Empty ? currentUserId : null;
            var audit = new Auditlog
            {
                Userid = inforvs.Id,
                PerformeBy = performeBy,
                NewData = newData,
                OldData = olddata,
                Active = "UpdateProfile", // Đổi tên cho rõ ràng
                Timestamp = DateTime.Now,
            };
            _context.Auditlogs.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Auditlog>> GetAuditLogs()
        {
            var audit = _context.Auditlogs.Include(a => a.PerformeByNavigation).Include(x => x.User).ToList();
            return audit;
        }
        public async Task SendNotificationtoClass(int classId, string subject)
        {
            var in4class = await _context.Schedules
                 .Include(s => s.Subject)
                 .Include(s => s.Room)
                 .Include(s => s.StudyShift)
                 .Include(s => s.ScheduleDays)
                 .ThenInclude(s => s.DayOfWeekk)
                 .FirstOrDefaultAsync(c => c.Id == classId);
            var lststudeninclass = await _context.Schedules
                .Where(c => c.Id == classId)
                 .Include(s => s.ScheduleStudents).ThenInclude(c => c.Student)
                 .ThenInclude(u => u.User)
                 .Select(d => d.User.Email).ToListAsync();
            string message = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <title>Thông báo lớp học</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f6f8;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 650px;
            margin: 40px auto;
            background-color: #ffffff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        h2 {{
            color: #2c3e50;
        }}
        p {{
            font-size: 15px;
            color: #555;
            margin-bottom: 10px;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .info-table td {{
            padding: 10px;
            border: 1px solid #e1e1e1;
        }}
        .info-table td.label {{
            background-color: #f9f9f9;
            font-weight: bold;
            width: 160px;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 13px;
            color: #888;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>📚 Thông báo lịch học lớp {in4class.ClassName}</h2>
        <p>Xin chào các bạn sinh viên,</p>
        <p>Dưới đây là thông tin chi tiết về lớp học sắp tới:</p>

        <table class='info-table'>
            <tr>
                <td class='label'>Tên lớp:</td>
                <td>{in4class.ClassName}</td>
            </tr>
            <tr>
                <td class='label'>Môn học:</td>
                <td>{in4class.Subject.SubjectName}</td>
            </tr>
         
            <tr>
                <td class='label'>Phòng học:</td>
                <td>{in4class.Room.RoomCode}</td>
            </tr>
            <tr>
                <td class='label'>Ca học:</td>
                <td>{in4class.StudyShift.StudyShiftName} 
                ({in4class.StudyShift.StartTime:hh\\:mm} - {in4class.StudyShift.EndTime:hh\\:mm})</td>
            </tr>
            <tr>
                <td class='label'>Thứ:</td>
                <td>{in4class.ScheduleDays.FirstOrDefault().DayOfWeekk.Weekdays}</td>
            </tr>
            <tr>
                <td class='label'>Giảng viên phụ trách:</td>
                <td>{in4class.User?.UserProfile?.FullName}</td>
            </tr>
        </table>

        <p>📌 Vui lòng theo dõi kỹ lịch học và chuẩn bị học tập đầy đủ.</p>
        <p>Chúc các bạn học tốt và đạt kết quả cao!</p>

        <div class='footer'>
            Trân trọng,<br>
            Ban quản lý đào tạo
        </div>
    </div>
</body>
</html>";


            foreach (var email in lststudeninclass)
            {
                await _email.SendEmail(email, subject, message);
            }
        }
        public async Task SendAsync(string subject, string message, Guid id)
        {
            var lststudent = await _context.Users.FirstOrDefaultAsync(c => c.Id == id);

            await _email.SendEmail(lststudent.Email, subject, message);
        }
        public async Task Validate(StudentViewModels model, bool isupdate = false)
        {
            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                throw new ArgumentNullException("Tên đăng nhập không được để trống");
            }
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@gmail.com"))
            {
                throw new ArgumentNullException("Email không hợp lệ ");
            }
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                throw new ArgumentNullException("Họ tên không được để trống");
            }
            if (string.IsNullOrWhiteSpace(model.Address))
            {
                throw new ArgumentNullException("Địa chỉ không được để trống");
            }
            if (model.Dob == null)
            {
                throw new ArgumentException("Vui lòng thêm ngày sinh");
            }
            if (model.Gender == null)
            {
                throw new ArgumentException("Vui lòng chọn giới tính");
            }
            if (!isupdate)
            {
                if (await _context.Users.AllAsync(c => c.UserName == model.UserName))
                {
                    throw new ArgumentException("Tên đăng nhập đã tồn tại");
                }
                if (await _context.Users.AllAsync(e => e.Email == model.Email))
                {
                    throw new ArgumentException("Email đã tồn tại");
                }
            }
            else
            {
                if (await _context.Users.AnyAsync(u => u.UserName == model.UserName && u.Id != model.id))
                {
                    throw new ArgumentException("Tên đăng nhập đã tồn tại");
                }
                if (await _context.Users.AnyAsync(e => e.Email == model.Email && e.Id != model.id))
                {
                    throw new ArgumentException("Email đã tồn tại");
                }
            }
        }
    }
}
