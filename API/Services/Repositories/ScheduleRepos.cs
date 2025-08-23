// ScheduleRepos.cs

using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace API.Services.Repositories
{
    public class ScheduleRepos : IShedulesRepos
    {
        private readonly AduDbcontext _context;
        public ScheduleRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task CreateSchedules(SchedulesDTO model)
        {
            if (model.startdate == null)
                throw new Exception("Ngày bắt đầu không được để trống.");

            var startDate = model.startdate.Value.Date;
            var endDate = startDate.AddDays(30);

            // Kiểm tra trùng lịch theo từng thứ được chọn
            foreach (var dayId in model.WeekDayId)
            {
                var conflict = await _context.SchedulesInDays
                    .Include(sd => sd.Schedule)
                    .Where(sd => sd.DayOfWeekkId == dayId
                        && sd.Schedule.RoomId == model.RoomId
                        && sd.Schedule.StudyShiftId == model.StudyShiftId
                        && sd.Schedule.StartDate == startDate)
                    .FirstOrDefaultAsync();

                if (conflict != null)
                    throw new Exception("Lịch bị trùng vào thứ đã chọn.");
            }

            var schedule = new Schedule
            {
                ClassName = model.ClassName,
                SubjectId = model.SubjectId,
                RoomId = model.RoomId,
                StudyShiftId = model.StudyShiftId,
                StartDate = startDate,
                EndDate = endDate,
                UsersId = model.TeacherId
            };

            setstatus(schedule);
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Gắn các thứ học vào bảng trung gian ScheduleDay
            foreach (var dayId in model.WeekDayId)
            {
                var sd = new SchedulesInDay
                {
                    ScheduleId = schedule.Id,
                    DayOfWeekkId = dayId
                };
                _context.SchedulesInDays.Add(sd);
            }

            await _context.SaveChangesAsync();
        }

        private void setstatus(Schedule sc)
        {
            if (sc.StartDate > DateTime.Now.Date)
                sc.Status = Schedule.SchedulesStatus.Sapdienra;
            else if (sc.StartDate <= DateTime.Now.Date && sc.EndDate >= DateTime.Now.Date)
                sc.Status = Schedule.SchedulesStatus.Dangdienra;
            else if (sc.EndDate < DateTime.Now.Date)
                sc.Status = Schedule.SchedulesStatus.Ketthuc;
        }

        public async Task<List<Schedule>> GetAll()
        {
            return await _context.Schedules
                .Include(s => s.Subject)
                .Include(r => r.Room)
                .Include(s => s.StudyShift)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<SchedulesViewModel> GetById(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.StudyShift)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Include(s => s.ScheduleStudents).ThenInclude(ss => ss.Student).ThenInclude(st => st.User).ThenInclude(u => u.UserProfile)
                .Include(s => s.ScheduleStudents).ThenInclude(ss => ss.Student).ThenInclude(st => st.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (schedule == null)
                throw new Exception("Không tìm thấy lịch học");

            var model = new SchedulesViewModel
            {
                Id = schedule.Id,
                ClassName = schedule.ClassName,
                SubjectName = schedule.Subject?.SubjectName ?? "N/A",
                RoomCode = schedule.Room?.RoomCode ?? "N/A",
                weekdays = schedule.ScheduleDays?.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                StudyShift = schedule.StudyShift?.StudyShiftName ?? "N/A",
                starttime = schedule.StudyShift?.StartTime,
                endtime = schedule.StudyShift?.EndTime,
                startdate = schedule.StartDate,
                enddate = schedule.EndDate,
                Status = schedule.Status.ToString(),
                UserId = schedule.UsersId,
                Students = schedule.ScheduleStudents?.Select(s => new StudentViewModels
                {
                    id = s.StudentsUserId,
                    UserName = s.Student?.User?.UserName ?? "N/A",
                    StudentCode = s.Student?.StudentsCode ?? "N/A",
                    FullName = s.Student?.User?.UserProfile?.FullName ?? "N/A",
                    PhoneNumber = s.Student?.User?.PhoneNumber ?? "N/A",
                    Email = s.Student?.User?.Email ?? "N/A",
                    Gender = s.Student?.User?.UserProfile?.Gender,
                    Address = s.Student?.User?.UserProfile?.Address ?? "N/A",
                }).ToList()
            };

            return model;
        }



        public async Task<List<Lichcodinh>> GetAllCoDinh()
        {
            var result = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.StudyShift)
                .Include(s => s.User).ThenInclude(u => u.UserProfile)
                .Include(s => s.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .ToListAsync();

            return result.GroupBy(s => new
            {
                s.ClassName,
                s.Subject.SubjectName,
                s.Room.RoomCode,
                s.StudyShift.StudyShiftName,
                s.StudyShift.StartTime,
                s.StudyShift.EndTime,
                s.UsersId,
                FullName = s.User.UserProfile.FullName
            }).Select(g => new Lichcodinh
            {
                ClassName = g.Key.ClassName,
                SubjectName = g.Key.SubjectName,
                RoomCode = g.Key.RoomCode,
                StudyShift = g.Key.StudyShiftName,
                StartTime = g.Key.StartTime,
                Endtime = g.Key.EndTime,
                TeacherName = g.Key.FullName,
                weekdays = g.SelectMany(x => x.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays)).Distinct().ToList()
            }).ToList();
        }

        public async Task UpdateSchedules(SchedulesDTO model)
        {
            var schedule = await _context.Schedules
                .Include(s => s.ScheduleDays)
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (schedule == null)
                throw new Exception("Không tìm thấy lịch học cần cập nhật.");

            // Kiểm tra trùng lịch
            var isDuplicated = await _context.Schedules
                .Include(s => s.ScheduleDays)
                .AnyAsync(s =>
                    s.Id != model.Id &&
                    s.RoomId == model.RoomId &&
                    s.StudyShiftId == model.StudyShiftId &&
                    s.ScheduleDays.Any(d => model.WeekDayId.Contains(d.DayOfWeekkId))
                );

            if (isDuplicated)
                throw new Exception("⚠️ Trùng lịch học: cùng ca học, phòng học và thứ học.");

            // Cập nhật thông tin
            schedule.ClassName = model.ClassName;
            schedule.SubjectId = model.SubjectId;
            schedule.StudyShiftId = model.StudyShiftId;
            schedule.RoomId = model.RoomId;
            schedule.UsersId = model.TeacherId.Value;

            // Cập nhật lại các ngày học
            _context.SchedulesInDays.RemoveRange(schedule.ScheduleDays);
            foreach (var dayId in model.WeekDayId)
            {
                _context.SchedulesInDays.Add(new SchedulesInDay
                {
                    ScheduleId = schedule.Id,
                    DayOfWeekkId = dayId
                });
            }

            await _context.SaveChangesAsync();
        }



        public async Task DeleteSchedules(int Id)
        {
            var delete = await _context.Schedules
                .Include(s => s.ScheduleDays)
                .FirstOrDefaultAsync(c => c.Id == Id);

            _context.SchedulesInDays.RemoveRange(delete.ScheduleDays);
            _context.Schedules.Remove(delete);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SchedulesViewModel>> GetByStudent(Guid Id)
        {
            var student = await _context.StudentsInfors
                .Include(s => s.ScheduleStudents).ThenInclude(c => c.Schedule)
                .FirstOrDefaultAsync(s => s.UserId == Id);

            if (student == null || student.ScheduleStudents.FirstOrDefault().Schedule == null)
                return new List<SchedulesViewModel>();

            var classId = student.ScheduleStudents.FirstOrDefault().Schedule.Id;

            var schedules = await _context.Schedules
                .Where(sc => sc.Id == classId)
                .Include(sc => sc.ScheduleDays).ThenInclude(sd => sd.DayOfWeekk)
                .Include(sc => sc.Room)
                .Include(sc => sc.StudyShift)
                .ToListAsync();

            return schedules.Select(sc => new SchedulesViewModel
            {
                Id = sc.Id,
                ClassName = sc.ClassName,
                weekdays = sc.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                StudyShift = sc.StudyShift.StudyShiftName,
                RoomCode = sc.Room.RoomCode,
            }).ToList();
        }

        public async Task<byte[]> ExportSchedules(List<SchedulesViewModel> model)
        {
            using var package = new ExcelPackage();
            var worksheets = package.Workbook.Worksheets.Add("Schedules");
            worksheets.Cells[1, 1].Value = "STT";
            worksheets.Cells[1, 2].Value = "Tên lớp";
            worksheets.Cells[1, 3].Value = "Phòng";
            worksheets.Cells[1, 4].Value = "Ngày";
            worksheets.Cells[1, 5].Value = "Ca";
            worksheets.Cells[1, 6].Value = "Môn";
            worksheets.Cells[1, 7].Value = "Giáo viên";

            int row = 2;
            int stt = 1;
            foreach (var item in model)
            {
                worksheets.Cells[row, 1].Value = stt++;
                worksheets.Cells[row, 2].Value = item.ClassName;
                worksheets.Cells[row, 3].Value = item.RoomCode;
                worksheets.Cells[row, 4].Value = item.weekdays;
                worksheets.Cells[row, 5].Value = item.StudyShift;
                worksheets.Cells[row, 6].Value = item.SubjectName;
                worksheets.Cells[row, 7].Value = item.UserId;
                row++;
            }

            return package.GetAsByteArray();
        }

        public Task AutogenerateSchedule()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AssignStudentToClassAsync(AssignStudentsRequest request)
        {
            if (request.StudentIds == null || !request.StudentIds.Any())
                return false;

            var classEntity = await _context.Schedules.FindAsync(request.SchedulesId);
            if (classEntity == null)
                return false;

            // Lấy danh sách sinh viên hợp lệ
            var validStudentIds = await _context.StudentsInfors
                .Where(s => request.StudentIds.Contains(s.UserId))
                .Select(s => s.UserId)
                .ToListAsync();

            // Lấy danh sách sinh viên đã tồn tại trong lớp
            var existingStudentIds = await _context.ScheduleStudentsInfors
                .Where(sc => sc.SchedulesId == request.SchedulesId && validStudentIds.Contains(sc.StudentsUserId))
                .Select(sc => sc.StudentsUserId)
                .ToListAsync();

            // Lọc ra danh sách sinh viên mới cần thêm
            var newStudentIds = validStudentIds.Except(existingStudentIds).ToList();

            // Tạo danh sách bản ghi mới
            var newEntries = newStudentIds.Select(studentId => new ScheduleStudentsInfor
            {
                SchedulesId = request.SchedulesId,
                StudentsUserId = studentId,

            }).ToList();

            // Thêm vào context
            _context.ScheduleStudentsInfors.AddRange(newEntries);

            // Cập nhật số lượng sinh viên

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> RemoveStudentFromClassAsync(int SchedulesId, Guid studentId)
        {
            var entryToRemove = await _context.ScheduleStudentsInfors
                                              .FirstOrDefaultAsync(sc => sc.SchedulesId == SchedulesId && sc.StudentsUserId == studentId);

            if (entryToRemove == null)
            {
                return false;
            }

            _context.ScheduleStudentsInfors.Remove(entryToRemove);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
