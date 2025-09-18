using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static API.Models.AttendanceDetail;

namespace API.Services.Repositories
{
    public class AttendanceRepos : IAttendance
    {
        private readonly AduDbcontext _context;

        public AttendanceRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task<bool> CheckInStudent(CheckInDto dto)
        {
            // Validation
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Dữ liệu điểm danh không được null");

            if (dto.AttendanceId <= 0)
                throw new ArgumentException("AttendanceId không hợp lệ");

            if (dto.StudentId == Guid.Empty)
                throw new ArgumentException("StudentId không hợp lệ");

            // Kiểm tra phiên điểm danh có tồn tại
            var attendanceExists = await _context.Attendances
                .AnyAsync(a => a.Id == dto.AttendanceId);

            if (!attendanceExists)
                throw new InvalidOperationException($"Không tìm thấy phiên điểm danh với ID: {dto.AttendanceId}");

            // Kiểm tra sinh viên có trong lớp không
            var studentInClass = await _context.ScheduleStudentsInfors
                .Include(ss => ss.Schedule)
                .AnyAsync(ss => ss.StudentsUserId == dto.StudentId &&
                               ss.Schedule.Attendances.Any(a => a.Id == dto.AttendanceId));

            if (!studentInClass)
                throw new InvalidOperationException("Sinh viên không thuộc lớp học này");

            // Kiểm tra phiên điểm danh còn trong thời gian cho phép
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.Id == dto.AttendanceId);

            if (attendance.Endtime.HasValue && DateTime.Now > attendance.Endtime.Value)
                throw new InvalidOperationException("Phiên điểm danh đã kết thúc");

            // Thực hiện điểm danh
            var detail = await _context.AttendanceDetails
                .FirstOrDefaultAsync(a => a.AttendanceId == dto.AttendanceId && a.StudentId == dto.StudentId);

            if (detail == null)
            {
                detail = new AttendanceDetail
                {
                    AttendanceId = dto.AttendanceId,
                    StudentId = dto.StudentId,
                    Status = dto.Status,
                    CheckinTime = DateTime.Now,
                    Description =  "Điểm danh thủ công bởi GV",
                };
                _context.AttendanceDetails.Add(detail);
            }
            else
            {
                detail.Status = dto.Status;
                detail.CheckinTime = DateTime.Now;
                detail.Description = "Cập nhật điểm danh thủ công";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateSession(CreateAttendanceViewModel model)
        {
            // Validation
            if (model == null)
                throw new ArgumentNullException(nameof(model), "Dữ liệu tạo phiên không được null");

            if (model.SchedulesId <= 0)
                throw new ArgumentException("SchedulesId không hợp lệ");

            if (string.IsNullOrWhiteSpace(model.SessionCode))
                throw new ArgumentException("Mã phiên điểm danh không được trống");

            // Kiểm tra mã phiên đã tồn tại chưa
            var sessionCodeExists = await _context.Attendances
                .AnyAsync(a => a.SessionCode == model.SessionCode);

            if (sessionCodeExists)
                throw new InvalidOperationException($"Mã phiên '{model.SessionCode}' đã tồn tại");

            // Lấy thông tin lịch học
            var schedule = await _context.Schedules
                .Include(s => s.StudyShift)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .FirstOrDefaultAsync(s => s.Id == model.SchedulesId);

            if (schedule == null)
                throw new InvalidOperationException("Không tìm thấy lịch học");

            if (schedule.StudyShift == null)
                throw new InvalidOperationException("Lịch học chưa được cấu hình ca học");

            // Kiểm tra hôm nay có phải là ngày học không
            var today = (Weekday)DateTime.Today.DayOfWeek;
            var validDays = schedule.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList();

            if (!validDays.Contains(today))
                throw new InvalidOperationException($"Hôm nay ({today}) không phải là ngày học của lớp. Lớp học vào: {string.Join(", ", validDays)}");

            // Kiểm tra đã có phiên điểm danh hôm nay chưa
            var todaySessionExists = await _context.Attendances
                .AnyAsync(a => a.SchedulesId == model.SchedulesId &&
                              a.CreateAt.HasValue &&
                              a.CreateAt.Value.Date == DateTime.Today);

            if (todaySessionExists)
                throw new InvalidOperationException("Đã tồn tại phiên điểm danh cho lớp này trong ngày hôm nay");

            // Kiểm tra thời gian trong ca học
            if (!schedule.StudyShift.StartTime.HasValue)
                throw new InvalidOperationException("Ca học chưa được cấu hình thời gian bắt đầu");

            var timeSpan = schedule.StudyShift.StartTime.Value.ToTimeSpan();
            var now = DateTime.Now;
            var startTime = DateTime.Today.Add(timeSpan);
            var endTime = startTime.AddMinutes(30);

            if (now < startTime.AddMinutes(-15)) // Cho phép tạo sớm 15 phút
                throw new InvalidOperationException($"Quá sớm để tạo phiên. Ca học bắt đầu lúc {startTime:HH:mm}");

            if (now > endTime)
                throw new InvalidOperationException($"Đã quá thời gian tạo phiên (trong 30 phút đầu ca học). Ca học bắt đầu lúc {startTime:HH:mm}");

            // Validate thời gian start/end nếu có
            if (model.Starttime.HasValue && model.Endtime.HasValue)
            {
                if (model.Endtime <= model.Starttime)
                    throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu");

                if ((model.Endtime.Value - model.Starttime.Value).TotalHours > 4)
                    throw new ArgumentException("Phiên điểm danh không được quá 4 giờ");
            }

            // Tạo phiên
            var session = new Attendance
            {
                SchedulesId = model.SchedulesId,
                SessionCode = model.SessionCode.ToUpper(), // Chuẩn hóa mã phiên
                CreateAt = DateTime.Now,
                Starttime = model.Starttime ?? DateTime.Now,
                Endtime = model.Endtime ?? DateTime.Now.AddHours(2), // Mặc định 2 giờ
            };

            _context.Attendances.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TeacherClassViewModel>> GetTeacherClasses(Guid teacherId)
        {
            // Validation
            if (teacherId == Guid.Empty)
                throw new ArgumentException("TeacherId không hợp lệ");

            // Kiểm tra giáo viên tồn tại
            var teacherExists = await _context.Users.AnyAsync(u => u.Id == teacherId);
            if (!teacherExists)
                throw new InvalidOperationException($"Không tìm thấy giáo viên với ID: {teacherId}");

            var today = (Weekday)DateTime.Today.DayOfWeek;

            var teacherClasses = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.StudyShift)
                .Include(s => s.Room)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Include(s => s.Attendances.Where(a => a.CreateAt.HasValue && a.CreateAt.Value.Date == DateTime.Today))
                .Where(s => s.UsersId == teacherId)
                .ToListAsync();

            var result = teacherClasses.Select(s => new TeacherClassViewModel
            {
                ScheduleId = s.Id,
                ClassName = s.ClassName ?? "N/A",
                SubjectName = s.Subject?.SubjectName ?? "N/A",
                StudyShiftName = s.StudyShift?.StudyShiftName ?? "N/A",
                WeekDays = s.ScheduleDays?.Select(sd => sd.DayOfWeekk?.Weekdays.ToString() ?? "N/A").ToList() ?? new List<string>(),
                RoomCode = s.Room?.RoomCode ?? "N/A",
                CanCreateSession = s.ScheduleDays.Any(sd => sd.DayOfWeekk.Weekdays == today)
                                 && !s.Attendances.Any(),
                HasActiveSession = s.Attendances.Any()
            }).ToList();

            return result;
        }

        public async Task<List<IndexAttendanceViewModel>> GetAllSession()
        {
            var attendance = await _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Subject)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Room)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Where(a => a.Schedules != null)
                .OrderByDescending(a => a.CreateAt) // Sắp xếp theo thời gian tạo
                .Select(a => new IndexAttendanceViewModel
                {
                    AttendanceId = a.Id,
                    SessionCode = a.SessionCode ?? "N/A",
                    SubjectName = a.Schedules.Subject != null ? a.Schedules.Subject.SubjectName : "N/A",
                    ClassName = a.Schedules.ClassName ?? "N/A",
                    ShiftStudy = a.Schedules.StudyShift != null ? a.Schedules.StudyShift.StudyShiftName : "N/A",
                    WeekDay = a.Schedules.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                    RoomCode = a.Schedules.Room != null ? a.Schedules.Room.RoomCode : "N/A",
                    StartTime = a.Starttime,
                    EndTime = a.Endtime,
                }).ToListAsync();

            return attendance;
        }

        public async Task<IndexAttendanceViewModel> GetByIndex(int attendanceId)
        {
            // Validation
            if (attendanceId <= 0)
                throw new ArgumentException("AttendanceId không hợp lệ");

            var attendance = await _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(a => a.ScheduleStudents).ThenInclude(a => a.Student)
                        .ThenInclude(si => si.User)
                            .ThenInclude(u => u.UserProfile)
                .Include(a => a.Schedules).ThenInclude(s => s.Subject)
                .Include(a => a.Schedules).ThenInclude(s => s.Room)
                .Include(a => a.Schedules).ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules).ThenInclude(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Include(a => a.AttendanceDetails)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
                return null;

            var schedule = attendance.Schedules;

            if (schedule == null)
                throw new InvalidOperationException("Phiên điểm danh không có lịch học liên kết");

            var result = new IndexAttendanceViewModel
            {
                AttendanceId = attendance.Id,
                SessionCode = attendance.SessionCode ?? "N/A",
                SubjectName = schedule.Subject?.SubjectName ?? "N/A",
                ClassName = schedule.ClassName ?? "N/A",
                ShiftStudy = schedule.StudyShift?.StudyShiftName ?? "N/A",
                WeekDay = schedule.ScheduleDays?.Select(d => d.DayOfWeekk.Weekdays).ToList() ?? new List<Weekday>(),
                RoomCode = schedule.Room?.RoomCode ?? "N/A",
                StartTime = attendance.Starttime,
                EndTime = attendance.Endtime,
                stinclass = schedule.ScheduleStudents?.Select(c =>
                {
                    var detail = attendance.AttendanceDetails
                        .FirstOrDefault(ad => ad.StudentId == c.Student.UserId);

                    return new StudentAttendanceViewModel
                    {
                        StudentId = c.Student.UserId,
                        StudentCode = c.Student.StudentsCode ?? "N/A",
                        FullName = c.Student.User?.UserProfile?.FullName ?? "N/A",
                        Email = c.Student.User?.Email ?? "N/A",
                        HasCheckedIn = detail?.CheckinTime != null,
                        Status = detail?.Status.ToString() ?? "Absent"
                    };
                }).ToList() ?? new List<StudentAttendanceViewModel>()
            };

            return result;
        }

        public async Task<List<StudentAttendanceHistory>> GetHistoryForStudent(Guid studentId)
        {
            // Validation
            if (studentId == Guid.Empty)
                throw new ArgumentException("StudentId không hợp lệ");

            // Kiểm tra sinh viên tồn tại
            var studentExists = await _context.StudentsInfors.AnyAsync(s => s.UserId == studentId);
            if (!studentExists)
                throw new InvalidOperationException($"Không tìm thấy sinh viên với ID: {studentId}");

            var result = await _context.AttendanceDetails
                .Include(d => d.Attendance)
                    .ThenInclude(a => a.Schedules).ThenInclude(c => c.Subject)
                .Include(d => d.Attendance.Schedules.StudyShift)
                .Include(d => d.Attendance.Schedules.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.CheckinTime) // Sắp xếp theo thời gian điểm danh mới nhất
                .Select(d => new StudentAttendanceHistory
                {
                    SubjectName = d.Attendance.Schedules.Subject != null ? d.Attendance.Schedules.Subject.SubjectName : "N/A",
                    ClassName = d.Attendance.Schedules.ClassName ?? "N/A",
                    Shift = d.Attendance.Schedules.StudyShift != null ? d.Attendance.Schedules.StudyShift.StudyShiftName : "N/A",
                    WeekDay = string.Join(", ", d.Attendance.Schedules.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays.ToString())),
                    CheckInTime = d.CheckinTime,
                    Status = d.Status == AttendanceStatus.Present ? "✅ Có mặt"
                            : d.Status == AttendanceStatus.Late ? "🕒 Đi trễ"
                            : "❌ Vắng",
                    Description = d.Description ?? "",
                    AttendanceDate = d.Attendance.Starttime.GetValueOrDefault(),
                }).ToListAsync();

            return result;
        }

        public async Task<List<IndexAttendanceViewModel>> Search(int? classId, int? studyShiftid, int? roomid, int? subjectid)
        {
            // Validation các parameter
            if (classId.HasValue && classId.Value <= 0)
                throw new ArgumentException("ClassId không hợp lệ");

            if (studyShiftid.HasValue && studyShiftid.Value <= 0)
                throw new ArgumentException("StudyShiftId không hợp lệ");

            if (roomid.HasValue && roomid.Value <= 0)
                throw new ArgumentException("RoomId không hợp lệ");

            if (subjectid.HasValue && subjectid.Value <= 0)
                throw new ArgumentException("SubjectId không hợp lệ");

            var query = _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Subject)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Room)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Where(a => a.Schedules != null) // Đảm bảo có lịch học
                .AsSplitQuery();

            // Apply filters
            if (classId.HasValue)
                query = query.Where(c => c.Schedules.Id == classId.Value);

            if (studyShiftid.HasValue)
                query = query.Where(ss => ss.Schedules.StudyShiftId == studyShiftid.Value);

            if (roomid.HasValue)
                query = query.Where(r => r.Schedules.RoomId == roomid.Value);

            if (subjectid.HasValue)
                query = query.Where(s => s.Schedules.SubjectId == subjectid.Value);

            // Order by created date descending
            query = query.OrderByDescending(a => a.CreateAt);

            var result = await query.Select(s => new IndexAttendanceViewModel
            {
                AttendanceId = s.Id,
                SessionCode = s.SessionCode ?? "N/A",
                ClassName = s.Schedules.ClassName ?? "N/A",
                SubjectName = s.Schedules.Subject != null ? s.Schedules.Subject.SubjectName : "N/A",
                ShiftStudy = s.Schedules.StudyShift != null ? s.Schedules.StudyShift.StudyShiftName : "N/A",
                RoomCode = s.Schedules.Room != null ? s.Schedules.Room.RoomCode : "N/A",
                WeekDay = s.Schedules.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                StartTime = s.Starttime,
                EndTime = s.Endtime,
            }).ToListAsync();

            return result;
        }
    }
}