using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class ComplaintRepos : IComplaintRepos
    {
        private readonly AduDbcontext _context;
        private readonly ILogger<ComplaintRepos> _logger;
        public ComplaintRepos(AduDbcontext aduDbcontext, ILogger<ComplaintRepos> logger)
        {
            _context = aduDbcontext;
            _logger = logger;
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!validImageFormats.Contains(ext))
            {
                _logger.LogError("Định dạng ảnh không hợp lệ");
                throw new Exception("Định dạng ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png");
            }

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(uploadDir, uniqueName);

            await using var stream = new FileStream(savePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folderName}/{uniqueName}";
        }

        public async Task<IEnumerable<ComplaintDTO>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername)
        {
            var query = _context.Complaints
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.ProcessedByNavigation)
                 .Include(c => c.ClassChange)
                    .ThenInclude(cc => cc.CurrentClass)
                .Include(c => c.ClassChange)
                    .ThenInclude(cc => cc.RequestedClass)
                .AsQueryable();

            if (!currentUserRoleIds.Contains(1) && !currentUserRoleIds.Contains(2)) // Nếu không phải Admin
            {
                // Chỉ xem khiếu nại của chính mình
                query = query.Where(c => c.Student != null && c.Student.UserName == currentUsername);
            }
            var complaints = await query.OrderByDescending(c => c.CreateAt).ToListAsync();
            // Map sang DTO
            return complaints.Select(c => new ComplaintDTO
            {
                Id = c.Id,
                ComplaintType = c.ComplaintType,
                Statuss = c.Statuss,
                Reason = c.Reason,
                ProofUrl = c.ProofUrl,
                StudentName = c.Student?.UserName,
                ProcessedByName = c.ProcessedByNavigation?.UserName,
                CreateAt = c.CreateAt,
                ProcessedAt = c.ProcessedAt,
                ResponseNote = c.ResponseNote,
                CurrentClassId = c.ClassChange?.CurrentClassId,
                RequestedClassId = c.ClassChange?.RequestedClassId,
                CurrentClassName = c.ClassChange?.CurrentClass?.ClassName,
                RequestedClassName = c.ClassChange?.RequestedClass?.ClassName
            }).ToList();
        }

        public async Task<string> SubmitClassChangeComplaint(ClassChangeComplaintDTO dto, string userCode, IFormFile imgFile)
        {
            // Tìm sinh viên theo userCode
            var student = await _context.StudentsInfors
                .FirstOrDefaultAsync(s => s.StudentsCode == userCode);

            if (student == null)
                throw new Exception("Không tìm thấy sinh viên với mã userCode.");

            var currentClass = await _context.Schedules.FindAsync(dto.CurrentClassId);
            var requestedClass = await _context.Schedules.FindAsync(dto.RequestedClassId);

            if (currentClass == null || requestedClass == null)
                throw new Exception("Một trong hai lớp không tồn tại.");
            if (dto.CurrentClassId == dto.RequestedClassId)
                throw new Exception("Lớp hiện tại và lớp yêu cầu không thể giống nhau.");
            if (currentClass.SubjectId != requestedClass.SubjectId)
                throw new Exception("Không thể chuyển lớp khác môn học.");

            var enrolledClasses = await ClassesOfStudent(userCode);
            if (!enrolledClasses.Any(c => c.schedulesId == dto.CurrentClassId))
                throw new Exception("Sinh viên không thuộc lớp hiện tại.");

            int subjectId = currentClass.SubjectId ?? throw new Exception("Môn học không được null.");
            //int semesterId = currentClass.SemesterId ?? throw new Exception("SemesterId không được null.");

            // Kiểm tra có khiếu nại trước đó không
            bool hasPendingComplaint = await _context.Complaints
                .Where(c =>
                    c.Student.StudentsInfor.StudentsCode == userCode &&
                    c.ComplaintType == "ClassChange")
                .Join(_context.ClassChanges, complaint => complaint.Id, classChange => classChange.ComplaintId,
                    (complaint, classChange) => new { complaint, classChange })
                .Join(_context.Schedules, cc => cc.classChange.RequestedClassId, cls => cls.Id,
                    (cc, cls) => new { cc.complaint, SubjectId = cls.SubjectId })
                .AnyAsync(x => x.SubjectId == subjectId /*&& x.SemesterId == semesterId*/);

            if (hasPendingComplaint)
                throw new Exception("Bạn đã gửi khiếu nại chuyển lớp cho môn học này trước đó.");

            // Xử lý upload minh chứng (bắt buộc)
            string proofPath = await SaveFileAsync(imgFile, "complaints");


            var newComplaint = new Complaint
            {
                StudentId = student.UserId,
                ComplaintType = "ClassChange",
                Reason = dto.Reason,
                Statuss = "Pending",
                CreateAt = DateTime.Now,
                ProofUrl = proofPath,
                ClassChange = new ClassChange
                {
                    CurrentClassId = dto.CurrentClassId,
                    RequestedClassId = dto.RequestedClassId
                }
            };
            await _context.Complaints.AddAsync(newComplaint);
            await _context.SaveChangesAsync();
            return newComplaint.Id.ToString();
        }
        public async Task<List<ClassViewModel>> ClassesOfStudent(string userCode)
        {
            return await _context.ScheduleStudentsInfors
                .Include(ss => ss.Schedule)
                    .ThenInclude(s => s.Subject)
                .Include(ss => ss.Student) // StudentsInfor
                .Where(ss => ss.Student.StudentsCode == userCode)
                .Select(ss => new ClassViewModel
                {
                    schedulesId = ss.Schedule.Id,
                    ClassName = ss.Schedule.ClassName,
                    SubjectName = ss.Schedule.Subject != null ? ss.Schedule.Subject.SubjectName : null,
                    
                    TeacherName = ss.Schedule.User != null ? ss.Schedule.User.UserName : null, // nếu Schedule có User (teacher)
                })
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> ProcessClassChangeComplaint(int complaintId, ProcessComplaintDTO dto, Guid handlerId)
        {
            // Tìm khiếu nại đổi lớp
            var complaint = await _context.Complaints
                .Include(c => c.ClassChange)
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.ComplaintType == "ClassChange");

            if (complaint == null)
                throw new Exception("Không tìm thấy khiếu nại đổi lớp.");

            if (complaint.Statuss == "Approved" || complaint.Statuss == "Rejected")
                throw new Exception("Khiếu nại đã được xử lý.");

            // Kiểm tra người xử lý hợp lệ (Admin hoặc Teacher)
            var handler = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == handlerId &&
                                          u.Roles.Any(r => r.RoleName == "Admin" || r.RoleName == "Teacher"));

            if (handler == null)
                throw new Exception("Người xử lý không hợp lệ.");

            // Nếu duyệt: cập nhật danh sách lớp cho sinh viên
            if (dto.Status == "Approved")
            {
                var studentId = complaint.StudentId;
                var oldClassId = complaint.ClassChange?.CurrentClassId;
                var newClassId = complaint.ClassChange?.RequestedClassId;

                if (studentId == null || oldClassId == null || newClassId == null)
                    throw new Exception("Thiếu thông tin lớp hoặc sinh viên.");

                var studentInfo = await _context.StudentsInfors
                    .Include(si => si.ScheduleStudents)
                    .FirstOrDefaultAsync(si => si.UserId == studentId);

                if (studentInfo == null)
                    throw new Exception("Không tìm thấy thông tin sinh viên.");

                // Xóa lớp cũ nếu tồn tại trong danh sách
                var oldScheduleStudent = studentInfo.ScheduleStudents
                    .FirstOrDefault(ss => ss.SchedulesId == oldClassId);
                if (oldScheduleStudent != null)
                {
                    studentInfo.ScheduleStudents.Remove(oldScheduleStudent);
                }

                // Thêm lớp mới nếu chưa có
                var existingScheduleStudent = studentInfo.ScheduleStudents
                    .FirstOrDefault(ss => ss.SchedulesId == newClassId);
                if (existingScheduleStudent == null)
                {
                    var studentsUserId = studentInfo.ScheduleStudents.FirstOrDefault()?.StudentsUserId
                      ?? studentInfo.UserId;

                    var newScheduleStudent = new ScheduleStudentsInfor
                    {
                        StudentsUserId = studentsUserId,
                        SchedulesId = newClassId.Value
                    };
                    studentInfo.ScheduleStudents.Add(newScheduleStudent);
                }
            }
            else if (dto.Status == "Rejected")
            {
            }
            // Cập nhật trạng thái, phản hồi
            complaint.Statuss = dto.Status;
            complaint.ResponseNote = dto.Note;
            complaint.ProcessedBy = handlerId;
            complaint.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync(); // Lưu trước để tránh lỗi ràng buộc
            return true;
        }
        public async Task<IEnumerable<ComplaintDTO>> ChiTietKhieuNai(int complaintId)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.ProcessedByNavigation)
                .Include(c => c.ClassChange)
                    .ThenInclude(cc => cc.CurrentClass)
                .Include(c => c.ClassChange)
                    .ThenInclude(cc => cc.RequestedClass)
                .FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null)
                throw new Exception("Không tìm thấy khiếu nại.");
            return new List<ComplaintDTO>
            {
                new ComplaintDTO
                {
                    Id = complaint.Id,
                    ComplaintType = complaint.ComplaintType,
                    Statuss = complaint.Statuss,
                    Reason = complaint.Reason,
                    ProofUrl = complaint.ProofUrl,
                    StudentName = complaint.Student?.UserName,
                    ProcessedByName = complaint.ProcessedByNavigation?.UserName,
                    CreateAt = complaint.CreateAt,
                    ProcessedAt = complaint.ProcessedAt,
                    ResponseNote = complaint.ResponseNote,
                    CurrentClassId = complaint.ClassChange?.CurrentClassId,
                    RequestedClassId = complaint.ClassChange?.RequestedClassId,
                    CurrentClassName = complaint.ClassChange?.CurrentClass?.ClassName,
                    RequestedClassName = complaint.ClassChange?.RequestedClass?.ClassName
                }
            };
        }
    }
}
