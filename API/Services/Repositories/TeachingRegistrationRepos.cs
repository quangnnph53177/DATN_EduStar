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
        private readonly IEmailRepos _emailRepos;
        public TeachingRegistrationRepos(AduDbcontext context, IEmailRepos emailRepos)
        {
            _context = context;
            _emailRepos = emailRepos;
        }
        //duyệt đăng ký
        public async Task<string> ApproveRegistration(int registrationId, ApprovedStatus approve, Guid adminId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reg = await _context.TeachingRegistrations
                    .Include(s => s.Schedule)
                    .Include(s => s.Teacher) // join với giảng viên để lấy email
                    .FirstOrDefaultAsync(tr => tr.Id == registrationId);

                if (reg == null)
                    return "Ko tìm thấy đơn đăng ký";

                if (reg.IsApproved != ApprovedStatus.Pending)
                    return "Đơn đã được xử lý";

                if (approve == ApprovedStatus.Approved)
                {
                    var canApprove = await CanApproveRegistration(reg.TeacherId, reg.ScheduleID, registrationId);
                    if (canApprove != "OK")
                        return $"Không thể duyệt: {canApprove}";

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

                // ✅ Gửi mail thông báo
                //var teacherEmail = reg.Teacher.Email;
                //var subject = approve == ApprovedStatus.Approved
                //    ? "Đăng ký giảng dạy đã được duyệt"
                //    : "Đăng ký giảng dạy bị từ chối";

                //var body = approve == ApprovedStatus.Approved
                //    ? $"Xin chào {reg.Teacher.UserProfile.FullName},<br/>Đơn đăng ký giảng dạy của bạn cho lớp {reg.Schedule.Subject.SubjectName} đã được duyệt."
                //    : $"Xin chào {reg.Teacher.UserProfile.FullName},<br/>Đơn đăng ký giảng dạy của bạn cho lớp {reg.Schedule.Subject.SubjectName} đã bị từ chối.";

                //await _emailRepos.SendEmail(teacherEmail, subject, body);

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
        private async Task<string> CanApproveRegistration(Guid teacherId, int schedulesID, int currentRegistrationId)
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

                // Kiểm tra xem có registration khác đã được approve cho schedule này chưa
                var existingApprovedRegistration = await _context.TeachingRegistrations
                    .FirstOrDefaultAsync(tr => tr.ScheduleID == schedulesID
                                            && tr.Id != currentRegistrationId // Bỏ qua registration hiện tại
                                            && tr.IsApproved == ApprovedStatus.Approved
                                            && (tr.Status == true || tr.Status == null));

                if (existingApprovedRegistration != null)
                {
                    return "Lịch học này đã có giảng viên được duyệt rồi";
                }

                // Kiểm tra trung lịch dạy với teacher
                var approvedRegistrations = await _context.TeachingRegistrations
                    .Include(s => s.Schedule)
                        .ThenInclude(s => s.ScheduleDays)
                    .Where(tr => tr.TeacherId == teacherId &&
                               tr.Id != currentRegistrationId && // Bỏ qua registration hiện tại
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
                                return $"Giảng viên có lịch dạy trùng với lớp {regSchedule.ClassName}";
                            }
                        }
                    }
                }

                return "OK";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kiểm tra approve: {ex.Message}", ex);
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
                    regisID = s.Id,
                    TeacherName = s.Teacher.UserName ?? "",
                    TeacherEmail = s.Teacher.Email ?? "",
                    ClassName = s.Schedule.ClassName ?? "",
                    SubjectName = s.Schedule.Subject.SubjectName ?? "",
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
                        SubjectName = s.Schedule.Subject.SubjectName ?? "",
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

        