using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class ComplaintRepos : IComplaintRepos
    {
        private readonly AduDbcontext _context;
        public ComplaintRepos(AduDbcontext aduDbcontext)
        {
            _context = aduDbcontext;
        }

        public async Task<IEnumerable<ComplaintDTO>> GetAllComplaints(List<int> currentUserRoleIds, string? currentUsername)
        {
            var query = _context.Complaints
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.ProcessedByNavigation)
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
                ResponseNote = c.ResponseNote
            }).ToList();
        }

        public async Task<string> SubmitClassChangeComplaint(ClassChangeComplaintDTO dto,Guid studentId)
        {
            var currentClassExits = _context.Classes.FirstOrDefault(c => c.Id == dto.CurrentClassId);
            var DesiredClassExits = _context.Classes.FirstOrDefault(c => c.Id == dto.RequestedClassId);
            var student = await GetStudentWithClasses(studentId);

            bool isInCurrentClass = student.Classes.Any(c => c.Id == dto.CurrentClassId);
            if (!isInCurrentClass)
                throw new Exception("Sinh viên không thuộc lớp hiện tại.");

            if (dto.CurrentClassId == dto.RequestedClassId)
                throw new Exception("Lớp hiện tại và lớp yêu cầu không thể giống nhau.");

            if (currentClassExits == null || DesiredClassExits == null)
            {
                throw new Exception("Lớp học không tồn tại.");
            }
            if(currentClassExits.SubjectId != DesiredClassExits.SubjectId)
            {
                throw new Exception("Không thể chuyển lớp khác môn học.");
            }

            //Tránh xung đột thời gian học

            //Chỉ cho phép nếu lớp còn chỗ trống
           
            //Đang có đơn khiếu nại trước đó
            int subjectid = currentClassExits.SubjectId ?? throw new Exception("SubjectId cannot be null.");
            bool isPendingComplaint = await _context.Complaints
                .Where(c =>
                c.StudentId == studentId &&
                c.ComplaintType == "ClassChange")
                .Join(_context.ClassChanges, Complaint => Complaint.Id,
                ClassChange => ClassChange.ComplaintId,
                (Complaint, ClassChange) => new { Complaint, ClassChange })
                .Join(_context.Classes,
                cc => cc.ClassChange.RequestedClassId,
                c => c.Id,
                (cc, c) => new { cc.Complaint, Subjectid = c.SubjectId })
                .AnyAsync(x => x.Subjectid == subjectid);
            if (isPendingComplaint)
            {
                throw new Exception("Bạn đã đổi lớp trước đó.");
            }
            var complaint = new Complaint
            {
                StudentId = studentId,
                ComplaintType = "ClassChange",
                Reason = dto.Reason,
                //ProofUrl = dto.ProofUrl,
                Statuss = "Pending",
                CreateAt = DateTime.Now,
                ClassChange = new ClassChange
                {
                    CurrentClassId = dto.CurrentClassId,
                    RequestedClassId = dto.RequestedClassId
                }
            };
            await _context.Complaints.AddAsync(complaint);
            await _context.SaveChangesAsync();
            return complaint.Id.ToString();
        }
        public async Task<StudentsInfor> GetStudentWithClasses(Guid studentId)
        {
            var student = await _context.StudentsInfors
                .Include(s => s.Classes)
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (student == null)
            {
                throw new Exception("Không tìm thấy sinh viên với mã này.");
            }

            //var classNames = student.Classes.Select(c => c.NameClass).ToList();
            //Console.WriteLine($"Lớp của sinh viên {studentCode}: {string.Join(", ", classNames)}");
            return student;
        }
        
        public Task<List<Class>> GetClassesInSameSubject(int currentClassId)
        {
            var currentClass = _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == currentClassId);
            if (currentClass == null)
            {
                throw new Exception("Lớp học không tồn tại.");
            }
            return _context.Classes
                .AsNoTracking()
                .Where(c => c.SubjectId == currentClass.Result.SubjectId && c.Id != currentClassId)
                .ToListAsync();

        }
        public async Task<List<ClassCreateViewModel>> GetClassesOfStudent(Guid studentId)
        {
            var student = await _context.StudentsInfors
               .Include(s => s.Classes)
               .FirstOrDefaultAsync(s => s.UserId == studentId);
            if (student == null)
            {
                throw new Exception("Không tìm thấy sinh viên với mã này.");
            }
            return student.Classes.Select(c => new ClassCreateViewModel
            {
                ClassName = c.NameClass,
                SemesterId = c.SemesterId,
                SubjectId = c.SubjectId ?? 0,
                YearSchool = (int)c.YearSchool
            }).ToList();
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
                    .Include(si => si.Classes)
                    .FirstOrDefaultAsync(si => si.UserId == studentId);

                if (studentInfo == null)
                    throw new Exception("Không tìm thấy thông tin sinh viên.");

                // Xóa lớp cũ nếu tồn tại trong danh sách
                var oldClass = await _context.Classes.FindAsync(oldClassId);
                if (oldClass != null && studentInfo.Classes.Contains(oldClass))
                {
                    studentInfo.Classes.Remove(oldClass);
                }

                // Thêm lớp mới nếu chưa có
                var newClass = await _context.Classes.FindAsync(newClassId);
                if (newClass != null && !studentInfo.Classes.Contains(newClass))
                {
                    studentInfo.Classes.Add(newClass);
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

    }
}
