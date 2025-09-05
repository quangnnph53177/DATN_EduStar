using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using static API.Models.Schedule;
using static API.Models.TeachingRegistration;

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

        //duyệt đăng ký
        public async Task<string> ApproveRegistration(int registrationId, ApprovedStatus approve, Guid adminId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reg = await _context.TeachingRegistrations
                    .Include(s => s.Schedule)
                    .FirstOrDefaultAsync(tr => tr.Id == registrationId);

                if (reg == null)
                {
                    return "Ko tìm thấy đơn đăng ký";
                }

                if (reg.IsApproved != ApprovedStatus.Pending)
                {
                    return "Đơn đã được xử lý";
                }

                if (approve == ApprovedStatus.Approved)
                {
                    var canApprove = await CanRegister(reg.TeacherId, reg.ScheduleID);
                    if (canApprove != "OK")
                    {
                        return $"Không thể duyệt: {canApprove}";
                    }

                    var schedule = reg.Schedule;
                    schedule.UsersId = reg.TeacherId;
                    schedule.Status = SchedulesStatus.Sapdienra;
                    _context.Schedules.Update(schedule);
                }

                reg.IsApproved = approve;
                reg.ApprovedBy = adminId;
                reg.ApprovedDate = DateTime.Now;
                _context.TeachingRegistrations.Update(reg);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return approve == ApprovedStatus.Approved ? "Duyệt đăng ký thành công" : "Đã từ chối đăng ký";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Lỗi khi duyệt đăng ký: {ex.Message}", ex);
            }
        }

        //từ chối 
        public async Task<string> CancelRegistration(int registrationId, Guid teacherId)
        {
            try
            {
                var reg = await _context.TeachingRegistrations
                    .Include(s => s.Schedule)
                    .FirstOrDefaultAsync(s => s.Id == registrationId && s.TeacherId == teacherId);

                if (reg == null)
                {
                    return "Không thấy đơn đăng ký";
                }

                if (reg.IsApproved == ApprovedStatus.Approved)
                {
                    return "Không thể từ chối đơn đã duyệt";
                }

                reg.Status = false;
                _context.TeachingRegistrations.Update(reg);
                await _context.SaveChangesAsync();

                return "Từ chối đơn đăng ký thành công";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi hủy đăng ký: {ex.Message}", ex);
            }
        }

        //check 
        public async Task<string> CanRegister(Guid teacherId, int schedulesID)
        {
            try
            {
                var schedule = await _context.Schedules
                    .Include(s => s.ScheduleDays)
                    .FirstOrDefaultAsync(s => s.Id == schedulesID);

                if (schedule == null)
                {
                    return "Lịch học không tồn tại";
                }

                // Sửa lỗi: Kiểm tra status đúng cho lịch có thể đăng ký
                if (schedule.Status != SchedulesStatus.Sapdienra || schedule.UsersId != null)
                {
                    return "Lịch học không còn có thể đăng ký";
                }

                // Kiểm tra đăng ký đã tồn tại
                var existingRegistration = await _context.TeachingRegistrations
                    .FirstOrDefaultAsync(tr => tr.TeacherId == teacherId
                                            && tr.ScheduleID == schedulesID
                                            && (tr.Status == true || tr.Status == null)); // Bao gồm cả Status null

                if (existingRegistration != null)
                {
                    if (existingRegistration.IsApproved == ApprovedStatus.Pending)
                    {
                        return "Bạn đã đăng ký lịch học này rồi, đang chờ duyệt";
                    }
                    if (existingRegistration.IsApproved == ApprovedStatus.Approved)
                    {
                        return "Bạn đã được phân vào lịch học này";
                    }
                }

                // Kiểm tra trung lịch dạy
                var approvedRegistrations = await _context.TeachingRegistrations
                    .Include(s => s.Schedule)
                        .ThenInclude(s => s.ScheduleDays)
                    .Where(tr => tr.TeacherId == teacherId &&
                               (tr.Status == true || tr.Status == null) &&
                               tr.IsApproved == ApprovedStatus.Approved)
                    .ToListAsync();

                foreach (var registration in approvedRegistrations)
                {
                    var regSchedule = registration.Schedule;

                    // Kiểm tra trung thời gian
                    if (schedule.StartDate < regSchedule.EndDate && schedule.EndDate > regSchedule.StartDate)
                    {
                        // Kiểm tra trung ca học
                        if (schedule.StudyShiftId == regSchedule.StudyShiftId)
                        {
                            var scheduleDays = schedule.ScheduleDays?.Select(s => s.DayOfWeekkId).ToList() ?? new List<int>();
                            var regDays = regSchedule.ScheduleDays?.Select(s => s.DayOfWeekkId).ToList() ?? new List<int>();

                            if (scheduleDays.Any(sd => regDays.Contains(sd)))
                            {
                                return $"Bạn có lịch dạy trùng với lớp {regSchedule.ClassName}";
                            }
                        }
                    }
                }

                return "OK";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kiểm tra đăng ký: {ex.Message}", ex);
            }
        }

        // danh sách đăng ký
        public async Task<List<AdminResgistration>> GetAllRegistration(string? Status = null)
        {
            try
            {
                var query = _context.TeachingRegistrations
                    .Include(s => s.Teacher)
                    .Include(s => s.Schedule).ThenInclude(s => s.Subject)
                    .Include(s => s.Schedule).ThenInclude(s => s.Room)
                    .Include(s => s.Schedule).ThenInclude(s => s.StudyShift)
                    .Include(s => s.Schedule).ThenInclude(s => s.ScheduleDays).ThenInclude(s => s.DayOfWeekk)
                    .Include(tr => tr.Approver)
                    .Where(tr => tr.Status == true || tr.Status == null) // Bao gồm cả null
                    .AsQueryable();

                if (!string.IsNullOrEmpty(Status))
                {
                    query = Status.ToLower() switch
                    {
                        "chờ duyệt" => query.Where(s => s.IsApproved == ApprovedStatus.Pending),
                        "đã duyệt" => query.Where(s => s.IsApproved == ApprovedStatus.Approved),
                        "hủy" => query.Where(s => s.IsApproved == ApprovedStatus.Rejected),
                        _ => query
                    };
                }

                var model = await query.Select(s => new AdminResgistration
                {
                    TeacherName = s.Teacher.UserName ?? "",
                    TeacherEmail = s.Teacher.Email ?? "",
                    ClassName = s.Schedule.ClassName ?? "",
                    SujectName = s.Schedule.Subject.SubjectName ?? "",
                    RoomName = s.Schedule.Room.RoomCode ?? "",
                    ShiftName = s.Schedule.StudyShift.StudyShiftName ?? "",
                    starttime = s.Schedule.StudyShift.StartTime,
                    endtime = s.Schedule.StudyShift.EndTime,
                    DayNames = s.Schedule.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList(),
                    CreateAt = s.CreateAt,
                    Status = s.IsApproved == ApprovedStatus.Pending ? "Chờ duyệt" :
                            s.IsApproved == ApprovedStatus.Approved ? "Đã duyệt" : "Từ chối",
                    StatusColor = s.IsApproved == ApprovedStatus.Pending ? "warning" :
                                 s.IsApproved == ApprovedStatus.Approved ? "success" : "danger",
                    ApprovedBy = s.Approver != null ? s.Approver.UserName : "",
                    ApprovedDate = s.ApprovedDate ?? DateTime.MinValue,
                })
                .OrderBy(s => s.Status == "Chờ duyệt" ? 0 : 1)
                .ThenByDescending(s => s.CreateAt)
                .ToListAsync();

                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách đăng ký: {ex.Message}", ex);
            }
        }

        //lịch có thể đăng ký
        public async Task<List<SchedulesViewModel>> GetAvailableSchedules(Guid? teacherID = null)
        {
            try
            {
                var schedules = _context.Schedules
                    .Include(s => s.Subject)
                    .Include(s => s.Room)
                    .Include(s => s.StudyShift)
                    .Include(s => s.ScheduleDays).ThenInclude(s => s.DayOfWeekk)
                    .Include(s => s.Semester)
                    .Where(s => s.Status == SchedulesStatus.Sapdienra && s.UsersId == null)
                    .AsQueryable();

                if (teacherID.HasValue)
                {
                    var registeredScheduleIds = await _context.TeachingRegistrations
                        .Where(tr => tr.TeacherId == teacherID &&
                                   (tr.Status == true || tr.Status == null) &&
                                   tr.IsApproved != ApprovedStatus.Rejected)
                        .Select(tr => tr.ScheduleID)
                        .ToListAsync();

                    schedules = schedules.Where(s => !registeredScheduleIds.Contains(s.Id));
                }

                var model = await schedules.Select(s => new SchedulesViewModel
                {
                    Id = s.Id,
                    ClassName = s.ClassName ?? "",
                    SubjectName = s.Subject.SubjectName ?? "",
                    RoomCode = s.Room.RoomCode ?? "",
                    StudyShift = s.StudyShift.StudyShiftName ?? "",
                    starttime = s.StudyShift.StartTime,
                    endtime = s.StudyShift.EndTime,
                    startdate = s.StartDate,
                    enddate = s.EndDate,
                    weekdays = s.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList(),
                    Status = s.Status.ToString(),
                })
                .OrderBy(s => s.startdate)
                .ToListAsync();

                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy lịch có thể đăng ký: {ex.Message}", ex);
            }
        }

        //giảng viên đăng ký cái gì rồi
        public async Task<List<TeachingRegistrationVM>> GetTeacherRegistrations(Guid teacherId)
        {
            try
            {
                var teacherRegistrations = await _context.TeachingRegistrations
                    .Include(s => s.Schedule).ThenInclude(s => s.Subject)
                    .Include(s => s.Schedule).ThenInclude(s => s.Room)
                    .Include(s => s.Schedule).ThenInclude(s => s.StudyShift)
                    .Include(s => s.Schedule).ThenInclude(s => s.ScheduleDays).ThenInclude(s => s.DayOfWeekk)
                    .Include(s => s.Approver)
                    .Where(s => s.TeacherId == teacherId && (s.Status == true || s.Status == null))
                    .Select(s => new TeachingRegistrationVM
                    {
                        regisID = s.Id,
                        SchedulesId = s.ScheduleID,
                        ClassName = s.Schedule.ClassName ?? "",
                        SujectName = s.Schedule.Subject.SubjectName ?? "",
                        RoomName = s.Schedule.Room.RoomCode ?? "",
                        ShiftName = s.Schedule.StudyShift.StudyShiftName ?? "",
                        starttime = s.Schedule.StudyShift.StartTime,
                        endtime = s.Schedule.StudyShift.EndTime,
                        DayNames = s.Schedule.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList(),
                        CreateAt = s.CreateAt,
                        Status = s.IsApproved == ApprovedStatus.Pending ? "Chờ duyệt" :
                                s.IsApproved == ApprovedStatus.Approved ? "Đã duyệt" : "Từ chối",
                        StatusColor = s.IsApproved == ApprovedStatus.Pending ? "warning" :
                                     s.IsApproved == ApprovedStatus.Approved ? "success" : "danger",
                        ApprovedBy = s.Approver != null ? s.Approver.UserName : "",
                        ApprovedDate = s.ApprovedDate ?? DateTime.MinValue,
                    })
                    .OrderByDescending(s => s.CreateAt)
                    .ToListAsync();

                return teacherRegistrations;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy đăng ký của giảng viên: {ex.Message}", ex);
            }
        }

        //đăng ký
        public async Task<string> TeacherRegistration(Guid teacherId, int schedulesID)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var check = await CanRegister(teacherId, schedulesID);
                if (check != "OK")
                {
                    return check;
                }

                var register = new TeachingRegistration()
                {
                    TeacherId = teacherId,
                    ScheduleID = schedulesID,
                    CreateAt = DateTime.Now,
                    Status = true, // Đặt Status = true thay vì null
                    IsApproved = ApprovedStatus.Pending,
                };

                _context.TeachingRegistrations.Add(register);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return "Đăng ký thành công";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Lỗi khi đăng ký: {ex.Message}", ex);
            }
        }
    }
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

        //public async Task<string> CanRegister(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end)
        //{
        //    // 1. Kiểm tra học kỳ
        //    var semester = await _context.Semesters.FindAsync(semesterId);
        //    if (semester == null)
        //        return $"Kỳ học không tồn tại. Id = {semesterId}";

        //    if (!semester.IsActive)
        //        return "Kỳ học này chưa được mở để đăng ký.";

        //    if (semester.StartDate == default || semester.EndDate == default)
        //        return $"Ngày bắt đầu/kết thúc của học kỳ không hợp lệ. Học kỳ: {semester.Name}";

        //    if (start > end)
        //        return "Ngày bắt đầu không được sau ngày kết thúc.";

        //    if (start < semester.StartDate || end > semester.EndDate)
        //        return "Thời gian đăng ký nằm ngoài phạm vi học kỳ hiện tại.";

        //    // 2. Kiểm tra lớp học tồn tại và hợp lệ
        //    var cls = await _context.Schedules.FindAsync(classId);
        //    if (cls == null || cls.Status == SchedulesStatus.Ketthuc)
        //        return "Lớp học không tồn tại hoặc đã bị khóa.";

        //    // 3. Kiểm tra giảng viên đã đăng ký trùng ca học chưa
        //    foreach (var dayId in dayIds)
        //    {
        //        // Trùng ca học trong cùng ngày
        //        var hasConflict = await _context.TeachingRegistrations.AnyAsync(x =>
        //            x.TeacherId == teacherId &&
        //            x.DayId == dayId &&
        //            x.StudyShiftId == shiftId &&
        //            x.Status == true);
        //        if (hasConflict)
        //            return $"Bạn đã đăng ký ca học Thứ {dayId} rồi. Hãy chọn ngày khác.";

        //        // Trùng lớp học ở ca khác
        //        var hasRegisteredSameClass = await _context.TeachingRegistrations.AnyAsync(x =>
        //            x.TeacherId == teacherId &&
        //            x.ClassId == classId &&
        //            x.DayId != dayId &&
        //            x.StudyShiftId != shiftId &&
        //            x.Status == true);
        //        if (hasRegisteredSameClass)
        //            return $"Bạn đã đăng ký lớp này ở ca khác rồi. Không thể đăng ký thêm.";
        //    }
        //    // 5. Kiểm tra giới hạn đăng ký (tổng số lớp và phòng)
        //    var totalRegistrations = await _context.TeachingRegistrations.CountAsync(x =>
        //        dayIds.Contains(x.DayId) &&
        //        x.StudyShiftId == shiftId &&
        //        x.Status == true);

        //    var totalClasses = await _context.Schedules.CountAsync();
        //    if (totalRegistrations >= totalClasses)
        //        return "Số lượng đăng ký cho ca học này đã đầy.";

        //    // 6. ❗ Giới hạn theo số phòng học
        //    var totalRooms = await _context.Rooms.CountAsync();
        //    if (totalRegistrations >= totalRooms)
        //        return "Số lượng đăng ký vượt quá số phòng học hiện có.";

        //    // 7. Giới hạn số ca giảng viên được đăng ký trong học kỳ (ví dụ: tối đa 10 ca)
        //    var registeredCount = await _context.TeachingRegistrations.CountAsync(x =>
        //        x.TeacherId == teacherId &&
        //        x.Status == true &&
        //        x.SemesterId == semesterId);

        //    if (registeredCount >= 8)
        //        return "Bạn đã đạt giới hạn số ca được đăng ký trong học kỳ này.";


        //    return "OK"; // ✅ Có thể đăng ký
        //}
        //public async Task<string> RegisterTeaching(Guid teacherId, int classId, int semesterId, List<int> dayIds, int shiftId, DateTime start, DateTime end)
        //{
        //    var classInfo = await _context.Schedules.FindAsync(classId);
        //    if (classInfo == null)
        //        return "Không tìm thấy thông tin lớp.";
        //    if (classInfo.UsersId != teacherId)
        //        return "Bạn không phải là giảng viên phụ trách lớp này.";

        //    foreach (var dayId in dayIds)
        //    {
        //        var canRegisterResult = await CanRegister(teacherId, classId, semesterId, dayIds, shiftId, start, end);
        //        if (canRegisterResult != "OK")
        //            return $"Không thể đăng ký cho thứ {dayId}: {canRegisterResult}";

        //        var reg = new TeachingRegistration
        //        {
        //            TeacherId = teacherId,
        //            ClassId = classId,
        //            SemesterId = semesterId,
        //            DayId = dayId,
        //            StudyShiftId = shiftId,
        //            StartDate = start,
        //            EndDate = end,
        //            Status = true,
        //            IsConfirmed = false
        //        };

        //        _context.TeachingRegistrations.Add(reg);
        //    }
        //    await _context.SaveChangesAsync();

        //    return "Đăng ký thành công.";
        //}

        //public async Task<List<TeachingRegistrationVMD>> GetTeacherRegister(string? userName, bool isAdmin)
        //{
        //    var query = _context.TeachingRegistrations
        //        .Include(r => r.Teacher)
        //        .Include(r => r.Schedule)
        //        .Include(r => r.Day)
        //        .Include(r => r.StudyShift)
        //        .Include(r => r.Semester)

        //        .AsQueryable();

        //    if (!isAdmin)
        //    {
        //        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        //        if (user == null) return new List<TeachingRegistrationVMD>();

        //        query = query.Where(x => x.TeacherId == user.Id && x.Status == true);
        //    }

        //    var list = await query
        //        .Select(r => new TeachingRegistrationVMD
        //        {
        //            Id = r.Id,
        //            TeacherName = r.Teacher.UserName,
        //            ClassName = r.Schedule.ClassName,
        //            DayName = r.Day.Weekdays.ToString(), // Fix: Convert Weekday enum to string using ToString()  
        //            ShiftName = r.StudyShift.StudyShiftName,
        //            SemesterName = r.Semester.Name,
        //            StartDate = r.StartDate ?? DateTime.MinValue,
        //            EndDate = r.EndDate ?? DateTime.MinValue,
        //            IsConfirmed = r.IsConfirmed ?? false
        //        })
        //        .OrderByDescending(r => r.IsConfirmed == false)
        //        .ToListAsync();

        //    return list;
        //}
        //public async Task<string> ConfirmTeachingRegistration(int registrationId)
        //{
        //    var registration = await _context.TeachingRegistrations.FindAsync(registrationId);
        //    if (registration == null)
        //        return "Không tìm thấy đơn đăng ký.";

        //    registration.IsConfirmed = true;
        //    await _context.SaveChangesAsync();
        //    // 🔁 Gọi logic RegisterSchedule ngay sau xác nhận
        //    var teacherId = registration.TeacherId;
        //    var classId = registration.ClassId;
        //    var semesterId = registration.SemesterId;
        //    var dayId = registration.DayId;
        //    var shiftId = registration.StudyShiftId;
        //    var start = registration.StartDate;
        //    var end = registration.EndDate;

        //    // Kiểm tra trùng lịch/phòng
        //    var classBusy = await _context.Schedules.AnyAsync(s =>
        //        s.ScheduleDays.FirstOrDefault().DayOfWeekk.Id == dayId && s.StudyShiftId == shiftId &&
        //        s.StartDate <= end && s.EndDate >= start);
        //    if (classBusy)
        //        return "Lớp đã có lịch dạy cho ca này.";

        //    var roomBusy = await _context.Schedules.Where(s =>
        //         s.ScheduleDays.FirstOrDefault().DayOfWeekk.Id == dayId && s.StudyShiftId == shiftId &&
        //        s.StartDate <= end && s.EndDate >= start)
        //        .Select(s => s.RoomId)
        //        .ToListAsync();
        //    var availableRooms = await _context.Rooms
        //        .Where(r => !roomBusy.Contains(r.Id))
        //        .Select(r => r.Id)
        //        .ToListAsync();

        //    if (!availableRooms.Any())
        //        return "Không còn phòng trống cho ca học này.";

        //    var randomRoom = new Random();
        //    var roomId = availableRooms[randomRoom.Next(availableRooms.Count)];

        //    var schedule = new Schedule
        //    {
        //       // ClassName =
        //       //// RoomId = roomId,
        //       // //SemesterId = semesterId,
        //       // DayId = dayId,
        //       // StudyShiftId = shiftId,
        //       // StartDate = start,
        //       // EndDate = end,
        //       // Status = SchedulesStatus.Sapdienra
        //    };

        //    _context.Schedules.Add(schedule);
        //    await _context.SaveChangesAsync();

        //    return $"✅ Đăng ký thành công! Đã xếp lớp vào phòng: {roomId} (ID: {roomId.ToString()})";
        //}
//    }
//}
