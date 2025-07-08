using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
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
            var session = new Attendance
            {
                SchedulesId = model.SchedulesId,
                SessionCode = model.SessionCode,
                CreateAt = DateTime.Now,
                Endtime = DateTime.Now.AddMinutes(30),
                Starttime = DateTime.Now,
            };
            _context.Attendances.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<IndexAttendanceViewModel>> GetAllSession()
        {
            var attendance =await _context.Schedules
                .Include(s => s.Class)
                .ThenInclude(s => s.Subject)
                .Include(s => s.StudyShift)
                .Include(s => s.Room)
                .Include(s => s.Day)
                .Include(s => s.Attendances)
                .Where(a=> a.Attendances.Any())
            .Select(u => new IndexAttendanceViewModel
            {
                AttendanceId = u.Attendances.FirstOrDefault().Id,
                SessionCode = u.Attendances.FirstOrDefault().SessionCode,
                SubjectName = u.Class.Subject.SubjectName,
                ClassName = u.Class.NameClass,
                ShiftStudy = u.StudyShift.StudyShiftName,
                WeekDay = u.Day.Weekdays.ToString(),
                RoomCode = u.Room.RoomCode,
                StartTime = u.Attendances.FirstOrDefault().Starttime,
                EndTime = u.Attendances.FirstOrDefault().Endtime,
            }).ToListAsync();
            return attendance;
        }

        public async Task<IndexAttendanceViewModel> GetByIndex(int attendanceId)
        {
            // Lấy thông tin phiên + thông tin lớp học liên quan
            var attendance = await _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(si => si.User) 
                                .ThenInclude(u => u.UserProfile)
                .Include(a => a.Schedules).ThenInclude(s => s.Class).ThenInclude(c => c.Subject)
                .Include(a => a.Schedules).ThenInclude(s => s.Room)
                .Include(a => a.Schedules).ThenInclude(s => s.StudyShift)
                .Include(a => a.Schedules).ThenInclude(s => s.Day)
                .Include(a => a.AttendanceDetails)    
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
                return null;

            var schedule = attendance.Schedules;
            var @class = schedule.Class;

            var result = new IndexAttendanceViewModel
            {
                AttendanceId = attendance.Id,
                SessionCode = attendance.SessionCode,
                SubjectName = @class.Subject.SubjectName,
                ClassName = @class.NameClass,
                ShiftStudy = schedule.StudyShift.StudyShiftName,
                WeekDay = schedule.Day.Weekdays.ToString(),
                RoomCode = schedule.Room.RoomCode,
                StartTime = attendance.Starttime,
                EndTime = attendance.Endtime,
                stinclass = @class.Students.Select(c => {
                    var detail = attendance.AttendanceDetails
                        .FirstOrDefault(ad => ad.StudentId == c.UserId);

                    return new StudentAttendanceViewModel
                    {
                        StudentId = c.UserId,
                        StudentCode = c.StudentsCode,
                        FullName = c.User.UserProfile.FullName,
                        Email = c.User.Email,
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
                .ThenInclude(a => a.Schedules)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Subject)
            .Include(d => d.Attendance.Schedules.StudyShift)
            .Include(d => d.Attendance.Schedules.Day)
            .Where(d => d.StudentId == studentId)
            .Select(d => new StudentAttendanceHistory
            {
                SubjectName = d.Attendance.Schedules.Class.Subject.SubjectName,
                ClassName = d.Attendance.Schedules.Class.NameClass,
                Shift = d.Attendance.Schedules.StudyShift.StudyShiftName,
                WeekDay = d.Attendance.Schedules.Day.Weekdays.ToString(),
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
                .Include(s => s.Schedules)
                .ThenInclude(c => c.Class)
                .ThenInclude(t => t.Subject)
                .Include(s => s.Schedules)
                .ThenInclude(s => s.StudyShift)
                .Include(s => s.Schedules)
                .ThenInclude(s => s.Room).AsSplitQuery();
            if (classId.HasValue)
            {
                query = query.Where(c => c.Schedules.Class.Id == classId.Value);
            }
            if (studyShiftid.HasValue) 
            {
                query = query.Where(ss => ss.Schedules.StudyShift.Id == studyShiftid.Value);
            }
            if (roomid.HasValue) 
            {
                query = query.Where(r => r.Schedules.Room.Id == roomid.Value);
            }
            if (subjectid.HasValue)
            {
                query = query.Where(s => s.Schedules.Class.Subject.Id == subjectid.Value);
            }
            var result =await query.OrderBy(s => s.User.Id).Select(s => new IndexAttendanceViewModel
            {
                AttendanceId = s.Id,
                SessionCode = s.SessionCode,
                ClassName = s.Schedules.Class.NameClass,
                SubjectName =s.Schedules.Class.Subject.SubjectName,
                ShiftStudy = s.Schedules.StudyShift.StudyShiftName,
                RoomCode=s.Schedules.Room.RoomCode,
                WeekDay=s.Schedules.Day.Weekdays.ToString(),
                StartTime=s.Starttime,
                EndTime=s.Endtime,

            }).ToListAsync();
            return result;
        }
    }
}
