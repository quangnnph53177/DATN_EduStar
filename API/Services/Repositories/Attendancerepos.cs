using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static API.Models.AttendanceDetail;

namespace API.Services.Repositories
{
    public class AttendanceRepos : IAttendance
    {
        private readonly AduDbcontext _context;
        private readonly FaceRecognitionService _faceService;
        //private readonly IHttpContextAccessor _httpContextAccessor;

        public AttendanceRepos(AduDbcontext context, FaceRecognitionService faceService)
        {
            _context = context;
            _faceService = faceService;
        }

        public async Task<bool> CheckInStudent(CheckInDto dto)
        {
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
                    Description = "Điểm danh thủ công bởi GV",
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
            var schedule = await _context.Schedules
                .Include(s => s.StudyShift)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .FirstOrDefaultAsync(s => s.Id == model.SchedulesId);

            if (schedule == null || schedule.StudyShift == null)
                throw new Exception("Không tìm thấy lịch học hoặc ca học.");
            // Kiểm tra hôm nay có phải là ngày học không
            //var today = (Weekday)DateTime.Today.DayOfWeek;
            //var validDays = schedule.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList();



            //if (!validDays.Contains(today))
            //    throw new Exception($"Hôm nay không phải là ngày học của lớp (Hôm nay là: {today})");

            ////  Kiểm tra thời gian trong ca học
            //if (!schedule.StudyShift.StartTime.HasValue)
            //    throw new Exception("Ca học chưa được cấu hình thời gian bắt đầu.");

            //var timeSpan = schedule.StudyShift.StartTime.Value.ToTimeSpan();
            //var now = DateTime.Now;
            //var startTime = DateTime.Today.Add(timeSpan);
            //var endTime = startTime.AddMinutes(30);

            //if (now < startTime || now > endTime)
            //    throw new Exception("Chỉ được tạo phiên điểm danh trong 30 phút đầu của ca học.");

            //  Tạo phiên
            var session = new Attendance
            {
                SchedulesId = model.SchedulesId,
                SessionCode = model.SessionCode,
                CreateAt = DateTime.Now,
                Starttime = model.Starttime,
                Endtime = model.Endtime,
            };

            _context.Attendances.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TeacherClassViewModel>> GetTeacherClasses(Guid teacherId)
        {
            var today = (Weekday)DateTime.Today.DayOfWeek;
            var teacherClasses = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.StudyShift)
                .Include(s => s.Room)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Include(s => s.Attendances.Where(a => a.CreateAt.Value == DateTime.Today))
                .Where(s => s.UsersId == teacherId) 
                .ToListAsync();

            var result = teacherClasses.Select(s => new TeacherClassViewModel
            {
                ScheduleId = s.Id,
                ClassName = s.ClassName,
                SubjectName = s.Subject.SubjectName,
                StudyShiftName = s.StudyShift.StudyShiftName,
                WeekDays = s.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays.ToString()).ToList(),
                RoomCode = s.Room.RoomCode,

                // Kiểm tra có phải ngày học hôm nay không
                CanCreateSession = s.ScheduleDays.Any(sd => sd.DayOfWeekk.Weekdays == today) && !s.Attendances.Any(a => a.CreateAt.Value == DateTime.Today),

                // Kiểm tra đã có phiên điểm danh hôm nay chưa
                HasActiveSession = s.Attendances.Any(a => a.CreateAt.Value == DateTime.Today)
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

                .Select(a => new IndexAttendanceViewModel
                {
                    AttendanceId = a.Id,
                    SessionCode = a.SessionCode,
                    SubjectName = a.Schedules.Subject.SubjectName,
                    ClassName = a.Schedules.ClassName,
                    ShiftStudy = a.Schedules.StudyShift.StudyShiftName,
                    WeekDay = a.Schedules.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                    RoomCode = a.Schedules.Room.RoomCode,
                    StartTime = a.Starttime,
                    EndTime = a.Endtime,
                }).ToListAsync();

            return attendance;
        }
        public async Task<IndexAttendanceViewModel> GetByIndex(int attendanceId)
        {
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
            var result = new IndexAttendanceViewModel
            {
                AttendanceId = attendance.Id,
                SessionCode = attendance.SessionCode,
                SubjectName = schedule.Subject.SubjectName,
                ClassName = schedule.ClassName,
                ShiftStudy = schedule.StudyShift.StudyShiftName,
                WeekDay = schedule.ScheduleDays?.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                RoomCode = schedule.Room.RoomCode,
                StartTime = attendance.Starttime,
                EndTime = attendance.Endtime,
                stinclass = schedule.ScheduleStudents.Select(c =>
                {
                    var detail = attendance.AttendanceDetails
                        .FirstOrDefault(ad => ad.StudentId == c.Student.UserId);

                    return new StudentAttendanceViewModel
                    {
                        StudentId = c.Student.UserId,
                        StudentCode = c.Student.StudentsCode,
                        FullName = c.Student.User.UserProfile.FullName,
                        Email = c.Student.User.Email,
                        HasCheckedIn = detail?.CheckinTime != null,
                        Status = detail?.Status.ToString()
                    };
                }).ToList()
            };

            return result;
        }

        public async Task<List<StudentAttendanceHistory>> GetHistoryForStudent(Guid studentId)
        {
            var result = await _context.AttendanceDetails
                .Include(d => d.Attendance)
                    .ThenInclude(a => a.Schedules).ThenInclude(c => c.Subject)
                .Include(d => d.Attendance.Schedules.StudyShift)
                .Include(d => d.Attendance.Schedules.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Where(d => d.StudentId == studentId)

                .Select(d => new StudentAttendanceHistory
                {
                    SubjectName = d.Attendance.Schedules.Subject.SubjectName,
                    ClassName = d.Attendance.Schedules.ClassName,
                    Shift = d.Attendance.Schedules.StudyShift.StudyShiftName,
                    // WeekDay = string.Join(", ", d.Attendance.Schedules.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays.ToString())),
                    CheckInTime = d.CheckinTime,
                    Status = d.Status == AttendanceStatus.Present ? "✅ Có mặt"
                            : d.Status == AttendanceStatus.Late ? "🕒 Đi trễ"
                            : "❌ Vắng",
                    Description = d.Description,
                    AttendanceDate = d.Attendance.Starttime.GetValueOrDefault(),
                }).ToListAsync();

            return result;
        }
        public async Task<List<IndexAttendanceViewModel>> Search(int? classId, int? studyShiftid, int? roomid, int? subjectid)
        {
            var query = _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Subject)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Room)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)

                .AsSplitQuery();


            if (classId.HasValue)
                query = query.Where(c => c.Schedules.Id == classId.Value);

            if (studyShiftid.HasValue)
                query = query.Where(ss => ss.Schedules.StudyShiftId == studyShiftid.Value);

            if (roomid.HasValue)
                query = query.Where(r => r.Schedules.RoomId == roomid.Value);

            if (subjectid.HasValue)
                query = query.Where(s => s.Schedules.SubjectId == subjectid.Value);

            var result = await query.Select(s => new IndexAttendanceViewModel
            {
                AttendanceId = s.Id,
                SessionCode = s.SessionCode,
                ClassName = s.Schedules.ClassName,
                SubjectName = s.Schedules.Subject.SubjectName,
                ShiftStudy = s.Schedules.StudyShift.StudyShiftName,
                RoomCode = s.Schedules.Room.RoomCode,
                WeekDay = s.Schedules.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                StartTime = s.Starttime,
                EndTime = s.Endtime,
            }).ToListAsync();

            return result;
        }

        public async Task<bool> CheckInByFace(int attendanceId, Guid studentId, byte[] faceImageBytes)
        {
            var attendanceExists = await _context.Attendances
                .AnyAsync(a => a.Id == attendanceId);

            if (!attendanceExists)
                throw new InvalidOperationException($"Không tìm thấy phiên điểm danh với ID: {attendanceId}");

            var studentInClass = await _context.ScheduleStudentsInfors
                .AnyAsync(ss => ss.StudentsUserId == studentId &&
                                ss.Schedule.Attendances.Any(a => a.Id == attendanceId));

            if (!studentInClass)
                throw new InvalidOperationException("Sinh viên không thuộc lớp học này");

            var student = await _context.StudentsInfors
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (student?.FaceFeatures == null || student.FaceFeatures.Length == 0)
            {
                throw new InvalidOperationException("Chưa có dữ liệu khuôn mặt cho sinh viên này.");
            }

            var detectedFaces = _faceService.DetectFaces(faceImageBytes);

            if (detectedFaces.Length == 0)
            {
                throw new InvalidOperationException("Không tìm thấy khuôn mặt trong ảnh.");
            }

            var uploadedFaceFeatures = _faceService.ExtractFaceFeatures(detectedFaces[0]);

            var storedFeatures = new float[student.FaceFeatures.Length / sizeof(float)];
            System.Runtime.InteropServices.Marshal.Copy(student.FaceFeatures, 0, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(storedFeatures, 0), storedFeatures.Length);

            if (!_faceService.CompareFaces(uploadedFaceFeatures, storedFeatures))
            {
                throw new InvalidOperationException("Khuôn mặt không khớp.");
            }

            var detail = await _context.AttendanceDetails
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId && a.StudentId == studentId);

            if (detail == null)
            {
                detail = new AttendanceDetail
                {
                    AttendanceId = attendanceId,
                    StudentId = studentId,
                    Status = AttendanceDetail.AttendanceStatus.Present,
                    CheckinTime = DateTime.Now,
                    Description = "Điểm danh bằng khuôn mặt"
                };
                _context.AttendanceDetails.Add(detail);
            }
            else
            {
                detail.Status = AttendanceDetail.AttendanceStatus.Present;
                detail.CheckinTime = DateTime.Now;
                detail.Description = "Cập nhật điểm danh bằng khuôn mặt";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<IndexAttendanceViewModel>> GetActiveSessionsForStudent(Guid studentId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var activeSessions = await _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Subject)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Room)
                .Where(a => a.Schedules != null &&
                            a.CreateAt.HasValue &&
                            a.CreateAt.Value.Date == today &&
                            a.Endtime.HasValue &&
                            now < a.Endtime.Value)
                .Where(a => a.Schedules.ScheduleStudents.Any(ss => ss.StudentsUserId == studentId) &&
                            !a.AttendanceDetails.Any(ad => ad.StudentId == studentId && ad.Status == AttendanceStatus.Present))
                .Select(a => new IndexAttendanceViewModel
                {
                    AttendanceId = a.Id,
                    SessionCode = a.SessionCode,
                    SubjectName = a.Schedules.Subject.SubjectName,
                    ClassName = a.Schedules.ClassName,
                    ShiftStudy = a.Schedules.StudyShift.StudyShiftName,
                    RoomCode = a.Schedules.Room.RoomCode
                }).ToListAsync();

            return activeSessions;
        }
    }
}