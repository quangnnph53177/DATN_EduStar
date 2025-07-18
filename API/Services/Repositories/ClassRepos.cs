using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;

namespace API.Services
{
    public class ClassRepos : IClassRepos
    {
        private readonly AduDbcontext _context;

        public ClassRepos(AduDbcontext context)
        {
            _context = context;
        }

        // Helper method để map từ Model sang ViewModel
        // Đã loại bỏ mapping cho Description và NumberOfCredits vì chúng không có trong Model
        private ClassViewModel MapToClassViewModel(Class classEntity)
        {
                
            if (classEntity == null) return null;
            var isTeacher = classEntity.User?.Roles?.Any(r => r.Id == 2) == true;

            return new ClassViewModel
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.NameClass,
                Semester = classEntity.SemesterId,

                // Thuộc tính SubjectName có trong Model Subject
                SubjectName = classEntity.Subject?.SubjectName,
                TeacherName = isTeacher? classEntity.User?.UserName : null,

                // Thuộc tính YearSchool có trong Model Class
                YearSchool = classEntity.YearSchool ?? 0, // Dùng ?? để xử lý giá trị null

                // Thuộc tính Description và NumberOfCredits không có trong các Model của bé,
                // nên không thể map được.
                // Description = classEntity.Description, // Dòng này sẽ gây lỗi biên dịch
                // NumberOfCredits = classEntity.Subject?.NumberOfCredits ?? 0, // Dòng này sẽ gây lỗi biên dịch

                Students = classEntity.Students
                            .Select(s => new StudentViewModels
                            {
                                id = s.UserId,
                                StudentCode = s.StudentsCode,
                                UserName = s.User?.UserName,
                                Email = s.User?.Email,
                                PhoneNumber = s.User?.PhoneNumber,
                                FullName = s.User?.UserProfile?.FullName,
                                Avatar = s.User?.UserProfile?.Avatar,
                                Address = s.User?.UserProfile?.Address,
                                Dob = s.User?.UserProfile?.Dob,
                                Status = s.User?.Statuss,
                            }).ToList()
            };
        }

        public async Task<IEnumerable<ClassViewModel>> GetAllClassesAsync()
        {
            var classes = await _context.Classes
                                        .Include(c => c.Subject)
                                        .Include(s => s.User)
                                                .ThenInclude(u => u.UserProfile)
                                        .Include(c => c.User) // Bao gồm thông tin giảng viên
                                            .ThenInclude(u=>u.Roles)
                                        .Include(c => c.Students)
                                            .ThenInclude(s => s.User)
                                                .ThenInclude(u => u.UserProfile)                                        
                                        .ToListAsync();

            return classes.Select(c => MapToClassViewModel(c));
        }

        public async Task<ClassViewModel> GetClassByIdAsync(int id)
        {
            var classEntity = await _context.Classes
                                            .Include(c => c.Subject)
                                            .Include(c => c.Students)
                                                .ThenInclude(s => s.User)
                                                    .ThenInclude(u => u.UserProfile)
                                            .Include(c => c.User)
                                                .ThenInclude(u => u.Roles)
                                            .Include(c => c.User) // Bao gồm thông tin giảng viên
                                                .ThenInclude(u => u.UserProfile)
                                            .FirstOrDefaultAsync(c => c.Id == id);

            return MapToClassViewModel(classEntity);
        }

        public async Task AddClassAsync(ClassCreateViewModel classViewModel)
        {
            var teacher = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u =>
                    u.Id == classViewModel.TeacherId &&
                    u.Roles.Any(r => r.Id == 2)&&
                    u.Id == classViewModel.TeacherId &&
                    u.Roles.Any(r => r.Id == 2)
                );

            if (teacher == null)
                throw new Exception("Không tìm thấy giảng viên phù hợp.");

            var newClass = new Class
            {
                NameClass = classViewModel.ClassName,
                SubjectId = classViewModel.SubjectId,
                SemesterId = classViewModel.SemesterId,
                YearSchool = classViewModel.YearSchool,
                UsersId = teacher.Id,
            };

            await _context.Classes.AddAsync(newClass);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClassAsync(int id, ClassUpdateViewModel classViewModel)
        {
            var classToUpdate = await _context.Classes.FindAsync(id);
            if (classToUpdate == null)
            {
                throw new KeyNotFoundException($"Class with ID {id} not found.");
            }

            if (classViewModel.ClassName != null)
            {
                classToUpdate.NameClass = classViewModel.ClassName;
            }
            if (classViewModel.SubjectId.HasValue)
            {
                classToUpdate.SubjectId = classViewModel.SubjectId;
            }
            if (classViewModel.Semester != null)
            {
                classToUpdate.SemesterId = classViewModel.Semester;
            }
            if (classViewModel.YearSchool.HasValue)
            {
                classToUpdate.YearSchool = classViewModel.YearSchool;
            }

            if (!string.IsNullOrWhiteSpace(classViewModel.TeacherName))
            {
                var teacher = await _context.Users
                    .Include(u => u.Roles)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u =>
                        u.UserProfile.FullName == classViewModel.TeacherName &&
                        u.Roles.Any(r => r.Id == 2 )
                    );

                if (teacher == null)
                {
                    throw new Exception($"Không tìm thấy giảng viên có tên '{classViewModel.TeacherName}'.");
                }

                classToUpdate.UsersId = teacher.Id;
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClassAsync(int id)
        {
            var classToDelete = await _context.Classes.FindAsync(id);
            if (classToDelete != null)
            {
                _context.Classes.Remove(classToDelete);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ClassViewModel>> SearchClassesAsync(int id)
        {
            var classes = await _context.Classes
                                        .Where(c => c.Id == id)
                                        .Include(c => c.Subject)
                                        .Include(c => c.Students)
                                            .ThenInclude(s => s.User)
                                                .ThenInclude(u => u.UserProfile)
                                        .ToListAsync();

            return classes.Select(c => MapToClassViewModel(c));
        }

        // Sửa lỗi 500: Thêm AssignedDate vào bảng trung gian
        public async Task<bool> AssignStudentToClassAsync(AssignStudentsRequest request)
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists || request.StudentIds == null || !request.StudentIds.Any())
                return false;

            var validStudents = await _context.StudentsInfors
                .Where(s => request.StudentIds.Contains(s.UserId))
                .Select(s => s.UserId)
                .ToListAsync();

            // Tránh thêm sinh viên đã có trong lớp
            var existingEntries = await _context.Set<Dictionary<string, object>>("StudentInClass")
                .Where(sc => (int)sc["ClassId"] == request.ClassId && validStudents.Contains((Guid)sc["StudentId"]))
                .Select(sc => (Guid)sc["StudentId"])
                .ToListAsync();

            var newStudentIds = validStudents.Except(existingEntries).ToList();

            // Tạo bản ghi mới
            var newEntries = newStudentIds.Select(id => new Dictionary<string, object>
            {
                ["ClassId"] = request.ClassId,
                ["StudentId"] = id,
                ["AssignedDate"] = DateTime.Now
            }).ToList();

            // Thêm vào DB
            foreach (var entry in newEntries)
            {
                _context.Set<Dictionary<string, object>>("StudentInClass").Add(entry);
            }

            // Tăng StudentCount
            var cls = await _context.Classes.FindAsync(request.ClassId);
            if (cls != null)
            {
                cls.StudentCount += newEntries.Count;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StudentRegisterClassAsync(AssignStudentsRequest request)
        {
            return await AssignStudentToClassAsync(request);
        }

        public async Task<bool> RemoveStudentFromClassAsync(int classId, Guid studentId)
        {
            var entryToRemove = await _context.Set<Dictionary<string, object>>("StudentInClass")
                                              .FirstOrDefaultAsync(sc => (int)sc["ClassId"] == classId && (Guid)sc["StudentId"] == studentId);

            if (entryToRemove == null)
            {
                return false;
            }

            _context.Set<Dictionary<string, object>>("StudentInClass").Remove(entryToRemove);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<ClassChangeViewModel>> GetClassHistoryAsync(int classId)
        {
            var history = await _context.ClassChanges
                                        .Include(cc => cc.Complaint)
                                        .Where(c => c.CurrentClassId == classId || c.RequestedClassId == classId)
                                        .Select(c => new ClassChangeViewModel
                                        {
                                            ChangeDescription = $"Loại khiếu nại: {c.Complaint.ComplaintType}",
                                            ChangeDate = (DateTime)c.Complaint.CreateAt,
                                        }).ToListAsync();
            return history;
        }

        public async Task UpdateClassAsyncdat(int id, ClassCreateViewModel classViewModel)
        {
            var classToUpdate = await _context.Classes.FindAsync(id);
            if (classToUpdate == null)
            {
                throw new KeyNotFoundException($"Class with ID {id} not found.");
            }

            if (classViewModel.ClassName != null)
            {
                classToUpdate.NameClass = classViewModel.ClassName;
            }
            if (classViewModel.SubjectId.HasValue)
            {
                classToUpdate.SubjectId = classViewModel.SubjectId;
            }
            if (classViewModel.SemesterId != null)
            {
                classToUpdate.SemesterId = classViewModel.SemesterId;
            }
            if (classViewModel.YearSchool.HasValue)
            {
                classToUpdate.YearSchool = classViewModel.YearSchool;
            }

            if (classViewModel.TeacherId.HasValue)
            {
                var teacher = await _context.Users
                    .Include(u => u.Roles)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u =>
                        u.Id == classViewModel.TeacherId &&
                        u.Roles.Any(r => r.Id == 2 )
                    );

                if (teacher == null)
                {
                    throw new Exception($"Không tìm thấy giảng viên có tên '{classViewModel.TeacherId}'.");
                }

                classToUpdate.UsersId = teacher.Id;
            }
            await _context.SaveChangesAsync();
        }
    }
}