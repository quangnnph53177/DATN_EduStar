using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace API.Services.Repositories
{
    public class TeachingRegistrationRepos : ITeachingRegistrationRepos
    {
        private readonly AduDbcontext _context;
        private readonly ISemesterRepos _semesterRepos;
        public TeachingRegistrationRepos(AduDbcontext context, ISemesterRepos semesterRepos)
        {
            _context = context;
            _semesterRepos = semesterRepos;
        }

            //public async Task<List<Class>> GetClasses(string userName)
            //{
            //    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            //    if (user == null) return new List<Class>();

            //    return await _context.Classes
            //        .Where(c => c.UsersId == user.Id)
            //        .ToListAsync();
            //}

            //public async Task<List<DayOfWeekk>> GetDays() => await _context.DayOfWeeks.ToListAsync();
            //public async Task<List<StudyShift>> GetStudyShifts() => await _context.StudyShifts.ToListAsync();

            //public async Task<List<Room>> GetRooms(int dayId, int shiftId, DateTime start, DateTime end)
            //{
            //    var busyRoomIds = await _context.Schedules
            //        .Where(s => s.DayId == dayId && s.StudyShiftId == shiftId && s.StartDate <= end && s.EndDate >= start)
            //        .Select(s => s.RoomId)
            //        .ToListAsync();

            //    return await _context.Rooms.Where(r => !busyRoomIds.Contains(r.Id)).ToListAsync();
            //}

        public async Task<string> CanRegister(Guid teacherId, int classId, int dayId, int shiftId, int semesterId, DateTime start, DateTime end)
        {
            // 1. Kiểm tra học kỳ
            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
                return $"Kỳ học không tồn tại. Id = {semesterId}";

            if (!semester.IsActive)
                return "Kỳ học này chưa được mở để đăng ký.";

            if (semester.StartDate == default || semester.EndDate == default)
                return $"Ngày bắt đầu/kết thúc của học kỳ không hợp lệ. Học kỳ: {semester.Name}";

            if (start > end)
                return "Ngày bắt đầu không được sau ngày kết thúc.";

            if (start < semester.StartDate || end > semester.EndDate)
                return "Thời gian đăng ký nằm ngoài phạm vi học kỳ hiện tại.";

            // 2. Kiểm tra lớp học tồn tại và hợp lệ
            var cls = await _context.Classes.FindAsync(classId);
            if (cls == null || cls.Status==false)
                return "Lớp học không tồn tại hoặc đã bị khóa.";

            // 3. Kiểm tra giảng viên đã đăng ký trùng ca học chưa
            var hasConflict = await _context.TeachingRegistrations.AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.DayId == dayId &&
                x.StudyShiftId == shiftId &&
                x.Status == true);
            if (hasConflict)
                return "Bạn đã đăng ký ca học này. Hãy chọn ca học khác vào ngày khác nhé";

            // 4. ❗ Kiểm tra xem giảng viên này đã đăng ký lớp đó ở ca khác chưa
            var hasRegisteredSameClass = await _context.TeachingRegistrations.AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.ClassId == classId &&
                x.StudyShiftId != shiftId &&  x.DayId != dayId && 
                x.Status == true);

            if (hasRegisteredSameClass)
                return "Bạn đã đăng ký lớp này ở ca học khác.";

            // 5. Kiểm tra giới hạn đăng ký (tổng số lớp và phòng)
            var totalRegistrations = await _context.TeachingRegistrations.CountAsync(x =>
                x.DayId == dayId &&
                x.StudyShiftId == shiftId &&
                x.Status == true);

            var totalClasses = await _context.Classes.CountAsync();
            if (totalRegistrations >= totalClasses)
                return "Số lượng đăng ký cho ca học này đã đầy.";

            // 6. ❗ Giới hạn theo số phòng học
            var totalRooms = await _context.Rooms.CountAsync();
            if (totalRegistrations >= totalRooms)
                return "Số lượng đăng ký vượt quá số phòng học hiện có.";

            // 7. Giới hạn số ca giảng viên được đăng ký trong học kỳ (ví dụ: tối đa 10 ca)
            var registeredCount = await _context.TeachingRegistrations.CountAsync(x =>
                x.TeacherId == teacherId &&
                x.Status == true &&
                x.SemesterId == semesterId);

            if (registeredCount >= 8)
                return "Bạn đã đạt giới hạn số ca được đăng ký trong học kỳ này.";


            return "OK"; // ✅ Có thể đăng ký
        }
        public async Task<string> RegisterTeaching(Guid teacherId, int classId, int semesterId, int dayId, int shiftId, DateTime start, DateTime end)
        {
            var canRegisterResult = await CanRegister(teacherId, classId, dayId, shiftId,semesterId,start,end);
            if (canRegisterResult != "OK")
                return canRegisterResult; // Trả về lý do không thể đăng ký
            var classInfo = await _context.Classes.FindAsync(classId);
            if (classInfo == null)
                return "Không tìm thấy thông tin lớp.";
            if (classInfo.UsersId != teacherId)
                return "Bạn không phải là giảng viên phụ trách lớp này.";           

            var reg = new TeachingRegistration
            {
                TeacherId = teacherId,
                ClassId = classId,
                DayId = dayId,
                StudyShiftId = shiftId,
                SemesterId = semesterId,
                StartDate = start,
                EndDate = end,
                Status = true,
                IsConfirmed = false
            };

            _context.TeachingRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            return "Đăng ký thành công.";
        }

        public async Task<List<TeachingRegistrationVMD>> GetTeacherRegister(string? userName, bool isAdmin)
        {
            var query = _context.TeachingRegistrations
                .Include(r => r.Teacher)
                .Include(r => r.Class)
                .Include(r => r.Day)
                .Include(r => r.StudyShift)
                .Include(r => r.Semester)
                .AsQueryable();

            if (!isAdmin)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null) return new List<TeachingRegistrationVMD>();

                query = query.Where(x => x.TeacherId == user.Id && x.Status == true);
            }

            var list = await query
                .Select(r => new TeachingRegistrationVMD
                {
                    Id = r.Id,
                    TeacherName = r.Teacher.UserName,
                    ClassName = r.Class.NameClass,
                    DayName = r.Day.Weekdays.ToString(), // Fix: Convert Weekday enum to string using ToString()  
                    ShiftName = r.StudyShift.StudyShiftName,
                    SemesterName = r.Semester.Name,
                    StartDate = r.StartDate ?? DateTime.MinValue,
                    EndDate = r.EndDate ?? DateTime.MinValue,
                    IsConfirmed = r.IsConfirmed ?? false
                })
                .ToListAsync();

            return list;
        }
        public async Task<string> ConfirmTeachingRegistration(int registrationId)
        {
            var registration = await _context.TeachingRegistrations.FindAsync(registrationId);
            if (registration == null)
                return "Không tìm thấy đơn đăng ký.";

            registration.IsConfirmed = true;
            await _context.SaveChangesAsync();
            // 🔁 Gọi logic RegisterSchedule ngay sau xác nhận
            var classId = registration.ClassId;
            var teacherId = registration.TeacherId;
            var dayId = registration.DayId;
            var shiftId = registration.StudyShiftId;
            var start = registration.StartDate;
            var end = registration.EndDate;

            // Kiểm tra trùng lịch/phòng
            var classBusy = await _context.Schedules.AnyAsync(s =>
                s.ClassId == classId && s.DayId == dayId && s.StudyShiftId == shiftId &&
                s.StartDate <= end && s.EndDate >= start);
            if (classBusy)
                return "Lớp đã có lịch dạy cho ca này.";

            var roomBusy = await _context.Schedules.Where(s =>
                s.DayId == dayId && s.StudyShiftId == shiftId &&
                s.StartDate <= end && s.EndDate >= start)
                .Select(s => s.RoomId)
                .ToListAsync();
            var availableRooms = await _context.Rooms
                .Where(r => !roomBusy.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync();

            if (!availableRooms.Any())
                return "Không còn phòng trống cho ca học này.";

            var randomRoom = new Random();
            var roomId = availableRooms[randomRoom.Next(availableRooms.Count)];

            var schedule = new Schedule
            {
                ClassId = classId,
                RoomId = roomId,
                DayId = dayId,
                StudyShiftId = shiftId,
                StartDate = start,
                EndDate = end,
                Status = true
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return $"✅ Đăng ký thành công! Đã xếp lớp vào phòng: {roomId} (ID: {roomId.ToString()})";
        }
    }
}
