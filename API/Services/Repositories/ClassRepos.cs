using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace API.Services.Repositories
{
    public class ClassRepos : IClassRepos
    {
        private readonly AduDbcontext _context;
        private const int MaxClassNameLength = 90;
        private const int MaxSemesterLength = 10;
        private const int MaxSubjectNameLength = 200;
        private const int MinYearSchool = 2000;
        private const int MaxCredits = 10;
        private const int MinCredits = 1;

        public ClassRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task AddClassAsync(ClassViewModel classViewModel)
        {
            if (classViewModel == null)
            {
                throw new ArgumentNullException(nameof(classViewModel), "Class data cannot be null.");
            }

            // Validation được thực hiện trước khi cố gắng thêm vào DB
            await ValidateClassViewModelAsync(classViewModel, isUpdate: false);

            try
            {
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectName == classViewModel.SubjectName);

                if (subject == null)
                {
                    throw new ArgumentException($"Subject with name '{classViewModel.SubjectName}' does not exist.", nameof(classViewModel.SubjectName));
                }

                var existingClass = await _context.Classes
                    .AnyAsync(c => c.NameClass == classViewModel.ClassName.Trim() &&
                                   c.Semester == classViewModel.Semester.Trim());
                if (existingClass)
                {
                    throw new InvalidOperationException($"A class with name '{classViewModel.ClassName}' and semester '{classViewModel.Semester}' already exists.");
                }

                var classEntity = new Class
                {
                    NameClass = classViewModel.ClassName.Trim(),
                    SubjectId = subject.Id,
                    Semester = classViewModel.Semester.Trim(),
                    YearSchool = classViewModel.YearSchool // ClassViewModel.YearSchool đã là int, và đã được validate
                };

                await _context.Classes.AddAsync(classEntity);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Concurrency error occurred while creating class. The class might have been modified or deleted by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error saving class to database. Check database constraints (e.g., foreign key, unique constraints, data integrity).", ex);
            }
            catch (ArgumentException)
            {
                throw; // Ném lại ArgumentException từ ValidateClassViewModelAsync hoặc kiểm tra Subject
            }
            catch (InvalidOperationException)
            {
                throw; // Ném lại InvalidOperationException từ kiểm tra trùng lặp
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while creating class.", ex);
            }
        }

        public async Task DeleteClassAsync(int id)
        {
            try
            {
                var classToDelete = await _context.Classes.FindAsync(id);
                if (classToDelete == null)
                {
                    throw new KeyNotFoundException($"Class with ID {id} not found.");
                }

                _context.Classes.Remove(classToDelete);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Concurrency error occurred while deleting class. The class might have been modified or deleted by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error deleting class from database. It might be referenced by other entities (e.g., students).", ex);
            }
            catch (KeyNotFoundException)
            {
                throw; // Ném lại KeyNotFoundException đã bắt
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while deleting class.", ex);
            }
        }

        public async Task<IEnumerable<ClassViewModel>> GetAllClassesAsync()
        {
            try
            {
                return await _context.Classes
                    .Include(c => c.Subject)
                    .Select(c => new ClassViewModel
                    {
                        ClassName = c.NameClass,
                        SubjectName = c.Subject.SubjectName,
                        Semester = c.Semester,
                        // Fix lỗi CS0266: Sử dụng ?? để cung cấp giá trị mặc định nếu c.YearSchool là null
                        YearSchool = c.YearSchool ?? 0, // Hoặc một giá trị mặc định hợp lý khác, ví dụ: DateTime.UtcNow.Year
                        NumberOfCredits = c.Subject.NumberOfCredits ?? 0
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while retrieving all classes.", ex);
            }
        }

        public async Task<ClassViewModel> GetClassByIdAsync(int id)
        {
            try
            {
                var classEntity = await _context.Classes
                    .Include(c => c.Subject)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (classEntity == null)
                {
                    return null;
                }

                return new ClassViewModel
                {
                    ClassName = classEntity.NameClass,
                    SubjectName = classEntity.Subject.SubjectName,
                    Semester = classEntity.Semester,
                    // Fix lỗi CS0266: Sử dụng ?? để cung cấp giá trị mặc định nếu classEntity.YearSchool là null
                    YearSchool = classEntity.YearSchool ?? 0, // Hoặc một giá trị mặc định hợp lý khác
                    NumberOfCredits = classEntity.Subject.NumberOfCredits ?? 0 // Hoặc một giá trị mặc định hợp lý khác
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while retrieving class with ID {id}.", ex);
            }
        }

        public async Task UpdateClassAsync(int id, ClassViewModel classViewModel)
        {
            if (classViewModel == null)
            {
                throw new ArgumentNullException(nameof(classViewModel), "Class data cannot be null.");
            }

            // Validation được thực hiện trước khi cố gắng cập nhật DB
            await ValidateClassViewModelAsync(classViewModel, isUpdate: true);

            try
            {
                var classToUpdate = await _context.Classes.FindAsync(id);
                if (classToUpdate == null)
                {
                    throw new KeyNotFoundException($"Class with ID {id} not found for update.");
                }

                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectName == classViewModel.SubjectName);

                if (subject == null)
                {
                    throw new ArgumentException($"Subject with name '{classViewModel.SubjectName}' does not exist.", nameof(classViewModel.SubjectName));
                }

                // Kiểm tra trùng lặp nếu ClassName hoặc Semester bị thay đổi
                if (classToUpdate.NameClass.Trim() != classViewModel.ClassName.Trim() ||
                    classToUpdate.Semester.Trim() != classViewModel.Semester.Trim())
                {
                    var existingClass = await _context.Classes
                        .AnyAsync(c => c.NameClass == classViewModel.ClassName.Trim() &&
                                       c.Semester == classViewModel.Semester.Trim() &&
                                       c.Id != id); // Loại trừ chính lớp học đang cập nhật
                    if (existingClass)
                    {
                        throw new InvalidOperationException($"A class with name '{classViewModel.ClassName}' and semester '{classViewModel.Semester}' already exists for another class.");
                    }
                }

                // Cập nhật các thuộc tính của entity từ ViewModel
                classToUpdate.NameClass = classViewModel.ClassName.Trim();
                classToUpdate.SubjectId = subject.Id;
                classToUpdate.Semester = classViewModel.Semester.Trim();
                classToUpdate.YearSchool = classViewModel.YearSchool; // ClassViewModel.YearSchool đã là int

                _context.Classes.Update(classToUpdate);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Concurrency error occurred while updating class. The class might have been modified or deleted by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error updating class in database. Check database constraints (e.g., foreign key, unique constraints, data integrity).", ex);
            }
            catch (KeyNotFoundException)
            {
                throw; // Ném lại KeyNotFoundException
            }
            catch (ArgumentException)
            {
                throw; // Ném lại ArgumentException
            }
            catch (InvalidOperationException)
            {
                throw; // Ném lại InvalidOperationException
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while updating class.", ex);
            }
        }

        private async Task ValidateClassViewModelAsync(ClassViewModel classViewModel, bool isUpdate)
        {
            if (string.IsNullOrWhiteSpace(classViewModel.ClassName))
            {
                throw new ArgumentException("Class name is required.", nameof(classViewModel.ClassName));
            }
            if (classViewModel.ClassName.Length > MaxClassNameLength)
            {
                throw new ArgumentException($"Class name cannot exceed {MaxClassNameLength} characters.", nameof(classViewModel.ClassName));
            }

            if (string.IsNullOrWhiteSpace(classViewModel.SubjectName))
            {
                throw new ArgumentException("Subject name is required.", nameof(classViewModel.SubjectName));
            }
            if (classViewModel.SubjectName.Length > MaxSubjectNameLength)
            {
                throw new ArgumentException($"Subject name cannot exceed {MaxSubjectNameLength} characters.", nameof(classViewModel.SubjectName));
            }

            if (string.IsNullOrWhiteSpace(classViewModel.Semester))
            {
                throw new ArgumentException("Semester is required.", nameof(classViewModel.Semester));
            }
            if (classViewModel.Semester.Length > MaxSemesterLength)
            {
                throw new ArgumentException($"Semester cannot exceed {MaxSemesterLength} characters.", nameof(classViewModel.Semester));
            }
            if (!Regex.IsMatch(classViewModel.Semester, @"^(Spring|Fall|Summer)\d{4}$"))
            {
                throw new ArgumentException("Semester must be in format like 'Spring2023' or 'Fall2024'.", nameof(classViewModel.Semester));
            }

            // YearSchool trong ClassViewModel là int, nên nó không thể là null.
            // Nếu không phải update và giá trị <= 0, báo lỗi.
            if (!isUpdate && classViewModel.YearSchool <= 0)
            {
                throw new ArgumentException("YearSchool is required and must be greater than 0 for new classes.", nameof(classViewModel.YearSchool));
            }
            // Kiểm tra YearSchool nằm trong khoảng hợp lệ
            // Lấy năm hiện tại
            int currentYear = DateTime.UtcNow.Year;
            // Cho phép năm hiện tại và năm tiếp theo để tạo lớp học tương lai gần
            if (classViewModel.YearSchool < MinYearSchool || classViewModel.YearSchool > currentYear + 1)
            {
                throw new ArgumentException($"YearSchool must be between {MinYearSchool} and {currentYear + 1}.", nameof(classViewModel.YearSchool));
            }

            if (classViewModel.NumberOfCredits < MinCredits || classViewModel.NumberOfCredits > MaxCredits)
            {
                throw new ArgumentException($"NumberOfCredits must be between {MinCredits} and {MaxCredits}.", nameof(classViewModel.NumberOfCredits));
            }

            var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectName == classViewModel.SubjectName);
            if (!subjectExists)
            {
                throw new ArgumentException($"Subject with name '{classViewModel.SubjectName}' does not exist. Please create the subject first.", nameof(classViewModel.SubjectName));
            }
        }

        public async Task<IEnumerable<ClassViewModel>> SearchClassesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllClassesAsync();

            keyword = keyword.Trim().ToLower();
            return await _context.Classes
                .Include(c => c.Subject)
                .Where(c =>
                    c.NameClass.ToLower().Contains(keyword) ||
                    c.Semester.ToLower().Contains(keyword) ||
                    (c.Subject != null && c.Subject.SubjectName.ToLower().Contains(keyword))
                )
                .Select(c => new ClassViewModel
                {
                    ClassName = c.NameClass,
                    SubjectName = c.Subject != null ? c.Subject.SubjectName : "",
                    Semester = c.Semester,
                    YearSchool = c.YearSchool ?? 0,
                    NumberOfCredits = c.Subject != null ? c.Subject.NumberOfCredits ?? 0 : 0
                })
                .ToListAsync();
        }

        public async Task<bool> AssignStudentToClassAsync(int classId, Guid studentId)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == classId);
            var student = await _context.StudentsInfors.FindAsync(studentId);

            if (classEntity == null || student == null)
                return false;

            if (!classEntity.Students.Any(s => s.UserId == studentId))
            {
                classEntity.Students.Add(student);
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> StudentRegisterClassAsync(int classId, Guid studentId)
        {
            // Có thể thêm kiểm tra điều kiện đăng ký ở đây nếu cần
            return await AssignStudentToClassAsync(classId, studentId);
        }

        public async Task<bool> RemoveStudentFromClassAsync(int classId, Guid studentId)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
                return false;

            var student = classEntity.Students.FirstOrDefault(s => s.UserId == studentId);
            if (student != null)
            {
                classEntity.Students.Remove(student);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<string>> GetClassHistoryAsync(int classId)
        {
            var history = await _context.ClassChanges
                .Where(cc => cc.CurrentClassId == classId || cc.RequestedClassId == classId)
                .Select(cc => $"ComplaintId: {cc.ComplaintId}, From: {cc.CurrentClassId}, To: {cc.RequestedClassId}")
                .ToListAsync();

            return history;
        }

    }
}