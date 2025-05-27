using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class ClassRepos : IClassRepos
    {
        private readonly AduDbcontext _context;

        public ClassRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task CreateClassAsync(Class newClass)
        {
            if (newClass == null) throw new ArgumentNullException(nameof(newClass), "Class data cannot be null.");
            if (string.IsNullOrWhiteSpace(newClass.NameClass)) throw new ArgumentException("Class name cannot be empty.", nameof(newClass.NameClass));
            if (newClass.SubjectId <= 0) throw new ArgumentException("SubjectId must be a positive integer.", nameof(newClass.SubjectId));

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == newClass.SubjectId);
            if (!subjectExists) throw new InvalidOperationException($"Subject with ID {newClass.SubjectId} does not exist.");

            var classExists = await _context.Classes
                                            .AnyAsync(c => c.NameClass == newClass.NameClass &&
                                                           c.Semester == newClass.Semester &&
                                                           c.YearSchool == newClass.YearSchool);
            if (classExists) throw new InvalidOperationException($"A class with name '{newClass.NameClass}' in semester '{newClass.Semester}' and year '{newClass.YearSchool}' already exists.");

            await _context.Classes.AddAsync(newClass);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClassAsync(int id)
        {
            //if (id <= 0) throw new ArgumentException("Class ID must be a positive integer.", nameof(id));

            //var classToDelete = await _context.Classes.FindAsync(id);
            //if (classToDelete == null) throw new KeyNotFoundException($"Class with ID {id} not found.");

            //// Kiểm tra ràng buộc trước khi xóa: không thể xóa nếu có sinh viên hoặc lịch học liên quan
            //var hasStudents = await _context.StudentsInfors.AnyAsync(s => s.UserId == id);
            //if (hasStudents) throw new InvalidOperationException($"Cannot delete Class with ID {id} because there are students associated with it.");

            //var hasSchedules = await _context.Schedules.AnyAsync(s => s.ClassId == id);
            //if (hasSchedules) throw new InvalidOperationException($"Cannot delete Class with ID {id} because there are schedules associated with it.");
            var classToDelete = await _context.Classes.FindAsync(id);
            if (classToDelete == null) throw new KeyNotFoundException($"Class with ID {id} not found.");
            _context.Classes.Remove(classToDelete);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Class>> GetAllClassesAsync()
        {
            return await _context.Classes
                                 .Include(c => c.Subject)
                                 .ToListAsync();
        }

        public async Task<Class> GetClassByIdAsync(int id)
        {
           try
            {
                if (id <= 0) throw new ArgumentException("Class ID must be a positive integer.", nameof(id));

                var classData = await _context.Classes
                                                .Include(c => c.Subject)
                                                .FirstOrDefaultAsync(c => c.Id == id);
                if (classData == null) throw new KeyNotFoundException($"Class with ID {id} not found.");

                return classData;
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                throw new InvalidOperationException("An error occurred while retrieving the class.", ex);
            }
        }

        public async Task UpdateClassAsync(int id, Class updatedClass)
        {
            if (id <= 0) throw new ArgumentException("Class ID must be a positive integer.", nameof(id));
            if (updatedClass == null) throw new ArgumentNullException(nameof(updatedClass), "Updated class data cannot be null.");
            if (id != updatedClass.Id) throw new ArgumentException("Class ID in URL does not match ID in request body.");
            if (string.IsNullOrWhiteSpace(updatedClass.NameClass)) throw new ArgumentException("Class name cannot be empty.", nameof(updatedClass.NameClass));
            if (updatedClass.SubjectId <= 0) throw new ArgumentException("SubjectId must be a positive integer.", nameof(updatedClass.SubjectId));

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == updatedClass.SubjectId);
            if (!subjectExists) throw new InvalidOperationException($"Subject with ID {updatedClass.SubjectId} does not exist.");

            var existingClass = await _context.Classes.FindAsync(id);
            if (existingClass == null) throw new KeyNotFoundException($"Class with ID {id} not found for update.");

            // Kiểm tra trùng lặp với các lớp khác (không phải chính nó)
            var classExists = await _context.Classes
                                            .AnyAsync(c => c.NameClass == updatedClass.NameClass &&
                                                           c.Semester == updatedClass.Semester &&
                                                           c.YearSchool == updatedClass.YearSchool &&
                                                           c.Id != id);
            if (classExists) throw new InvalidOperationException($"Another class with name '{updatedClass.NameClass}' in semester '{updatedClass.Semester}' and year '{updatedClass.YearSchool}' already exists.");

            existingClass.NameClass = updatedClass.NameClass;
            existingClass.SubjectId = updatedClass.SubjectId;
            existingClass.Semester = updatedClass.Semester;
            existingClass.YearSchool = updatedClass.YearSchool;

            await _context.SaveChangesAsync();
        }
    }
}