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
            if (model.StartDate == null)
                throw new Exception("Ngày bắt đầu không được để trống.");

            var startDate = model.StartDate.Value.Date;
            var endDate =model.EndDate.Value.Date;

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
                .AsSplitQuery().OrderByDescending(s => s.Id)
                .ToListAsync();
        }

        public async Task<SchedulesViewModel> GetById(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.StudyShift)
                .Include(s => s.User).ThenInclude(u => u.UserProfile)
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
                Teachers = schedule.User != null ? new List<string> { schedule.User.UserProfile.FullName } : new List<string>(),
                SubjectId = schedule.SubjectId,
                RoomId = schedule.RoomId,
                StudyShiftId = schedule.StudyShiftId,
                WeekDayIds = schedule.ScheduleDays?.Select(d => d.DayOfWeekkId).ToList(),
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
            Console.WriteLine($"[Repo] Starting update for schedule ID: {model.Id}");

            var schedule = await _context.Schedules
                .Include(s => s.ScheduleDays)
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (schedule == null)
            {
                Console.WriteLine($"[Repo] Schedule not found with ID: {model.Id}");
                throw new Exception("Không tìm thấy lịch học cần cập nhật.");
            }

            Console.WriteLine($"[Repo] Found schedule: {schedule.ClassName}");

            // Kiểm tra trùng lịch một cách cụ thể hơn
            var conflictSchedules = await _context.Schedules
                .Include(s => s.ScheduleDays)
                .Where(s => s.Id != model.Id // Loại trừ chính schedule đang update
                    && s.RoomId == model.RoomId
                    && s.StudyShiftId == model.StudyShiftId)
                .ToListAsync();

            foreach (var conflictSchedule in conflictSchedules)
            {
                var conflictDays = conflictSchedule.ScheduleDays.Select(sd => sd.DayOfWeekkId).ToList();
                var newDays = model.WeekDayId ?? new List<int>();

                // Kiểm tra xem có ngày nào trùng không
                var hasConflict = newDays.Any(day => conflictDays.Contains(day));

                if (hasConflict)
                {
                    var conflictDayNames = conflictDays.Intersect(newDays)
                        .Select(dayId => GetDayName(dayId))
                        .ToList();

                    Console.WriteLine($"[Repo] Schedule conflict detected with {conflictSchedule.ClassName}");
                    throw new Exception($"Trùng lịch với lớp '{conflictSchedule.ClassName}' vào các ngày: {string.Join(", ", conflictDayNames)}");
                }
            }

            Console.WriteLine($"[Repo] No conflicts found, proceeding with update");

            try
            {
                // Bắt đầu transaction để đảm bảo tính nhất quán
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Cập nhật thông tin cơ bản
                schedule.ClassName = model.ClassName;
                schedule.SubjectId = model.SubjectId;
                schedule.StudyShiftId = model.StudyShiftId;
                schedule.RoomId = model.RoomId;

                if (model.TeacherId.HasValue)
                    schedule.UsersId = model.TeacherId.Value;

                Console.WriteLine($"[Repo] Updated basic info");

                // Xóa tất cả các ngày học cũ
                var oldDays = schedule.ScheduleDays.ToList();
                _context.SchedulesInDays.RemoveRange(oldDays);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Repo] Removed {oldDays.Count} old schedule days");

                // Thêm các ngày học mới
                if (model.WeekDayId != null && model.WeekDayId.Any())
                {
                    foreach (var dayId in model.WeekDayId)
                    {
                        var newScheduleDay = new SchedulesInDay
                        {
                            ScheduleId = schedule.Id,
                            DayOfWeekkId = dayId
                        };
                        _context.SchedulesInDays.Add(newScheduleDay);
                    }

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[Repo] Added {model.WeekDayId.Count} new schedule days");
                }

                // Cập nhật trạng thái lịch học
                setstatus(schedule);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"[Repo] Update completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Repo] Error during update: {ex}");
                throw new Exception($"Lỗi khi cập nhật lịch học: {ex.Message}");
            }
        }

        // Helper method để lấy tên ngày
        private string GetDayName(int dayId)
        {
            var dayNames = new Dictionary<int, string>
            {
                { 1, "Thứ 2" },
                { 2, "Thứ 3" },
                { 3, "Thứ 4" },
                { 4, "Thứ 5" },
                { 5, "Thứ 6" },
                { 6, "Thứ 7" },
                { 7, "Chủ nhật" }
            };

            return dayNames.TryGetValue(dayId, out var name) ? name : $"Ngày {dayId}";
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
                .Include(s => s.ScheduleStudents)
                    .ThenInclude(ss => ss.Schedule)
                        .ThenInclude(sc => sc.ScheduleDays)
                            .ThenInclude(sd => sd.DayOfWeekk)
                .Include(s => s.ScheduleStudents)
                    .ThenInclude(ss => ss.Schedule)
                        .ThenInclude(sc => sc.Room)
                .Include(s => s.ScheduleStudents)
                    .ThenInclude(ss => ss.Schedule)
                        .ThenInclude(sc => sc.StudyShift)
                .Include(s => s.ScheduleStudents)
                    .ThenInclude(ss => ss.Schedule)
                        .ThenInclude(sc => sc.Subject)
                .FirstOrDefaultAsync(s => s.UserId == Id);

            if (student == null || !student.ScheduleStudents.Any())
                return new List<SchedulesViewModel>();

            var result = new List<SchedulesViewModel>();

            foreach (var scheduleStudent in student.ScheduleStudents.Where(ss => ss.Schedule != null))
            {
                var schedule = scheduleStudent.Schedule;

                // Lấy ngày bắt đầu và kết thúc của lịch học
                var startDate = schedule.StartDate ?? DateTime.Now.Date;
                var endDate = schedule.EndDate ?? DateTime.Now.AddMonths(3).Date;

                foreach (var scheduleDay in schedule.ScheduleDays)
                {
                    // Tìm tất cả các ngày trong khoảng thời gian học thuộc thứ này
                    var datesForThisWeekday = GetDatesForWeekday(startDate, endDate, scheduleDay.DayOfWeekk.Weekdays);

                    foreach (var date in datesForThisWeekday)
                    {
                        result.Add(new SchedulesViewModel
                        {
                            Id = schedule.Id,
                            ClassName = schedule.ClassName ?? "",
                            RoomCode = schedule.Room?.RoomCode ?? "",
                            SubjectName = schedule.Subject?.SubjectName ?? "",
                            weekdays = new List<Weekday> { scheduleDay.DayOfWeekk.Weekdays },
                            StudyShift = schedule.StudyShift?.StudyShiftName ?? "",
                            starttime = schedule.StudyShift?.StartTime, // Giả sử StudyShift có StartTime
                            endtime = schedule.StudyShift?.EndTime,     // Giả sử StudyShift có EndTime
                            startdate = date,
                            enddate = date,
                            Status = schedule.Status?.ToString() ?? "",
                            UserId = Id
                        });
                    }
                }
            }

            // Sắp xếp theo ngày và ca học
            return result.OrderBy(r => r.startdate).ThenBy(r => r.starttime).ToList();
        }
        public async Task<List<SchedulesViewModel>> GetByTeacher(Guid Id)
        {
            // Kiểm tra user có phải là giảng viên không (RoleId = 2)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id && u.Roles.FirstOrDefault().Id == 2);
            if (user == null)
                return new List<SchedulesViewModel>();

            // Lấy thông tin giảng viên và các lịch dạy
            var teacher = await _context.Users
                .Include(c => c.Schedules) // Lịch dạy của giảng viên
                .ThenInclude(s => s.ScheduleDays)
                .ThenInclude(sd => sd.DayOfWeekk)
                .Include(c => c.Schedules)
                .ThenInclude(s => s.Room)
                .Include(c => c.Schedules)
                .ThenInclude(s => s.StudyShift)
                .Include(c => c.Schedules)
                .ThenInclude(s => s.Subject)
                .FirstOrDefaultAsync(t => t.Id == Id);

            if (teacher == null || !teacher.Schedules.Any())
                return new List<SchedulesViewModel>();

            var result = new List<SchedulesViewModel>();

            foreach (var schedule in teacher.Schedules.Where(s => s != null))
            {
                // Lấy ngày bắt đầu và kết thúc của lịch dạy
                var startDate = schedule.StartDate ?? DateTime.Now.Date;
                var endDate = schedule.EndDate ?? DateTime.Now.AddMonths(3).Date;

                foreach (var scheduleDay in schedule.ScheduleDays)
                {
                    // Tìm tất cả các ngày trong khoảng thời gian dạy thuộc thứ này
                    var datesForThisWeekday = GetDatesForWeekday(startDate, endDate, scheduleDay.DayOfWeekk.Weekdays);

                    foreach (var date in datesForThisWeekday)
                    {
                        var existingAttendance = await _context.Attendances
                                .FirstOrDefaultAsync(a => a.SchedulesId == schedule.Id &&
                              a.CreateAt.HasValue &&
                              a.CreateAt.Value.Date == date.Date);

                        // Kiểm tra có thể tạo phiên điểm danh không
                        var canCreate = CanCreateAttendanceForSchedule(schedule, date);
                        result.Add(new SchedulesViewModel
                        {
                            Id = schedule.Id,
                            ClassName = schedule.ClassName ?? "",
                            RoomCode = schedule.Room?.RoomCode ?? "",
                            SubjectName = schedule.Subject?.SubjectName ?? "",
                            weekdays = new List<Weekday> { scheduleDay.DayOfWeekk.Weekdays },
                            StudyShift = schedule.StudyShift?.StudyShiftName ?? "",
                            starttime = schedule.StudyShift?.StartTime,
                            endtime = schedule.StudyShift?.EndTime,
                            startdate = date,
                            enddate = date,
                            Status = schedule.Status?.ToString() ?? "",
                            UserId = Id,
                            CanCreateAttendance = canCreate && existingAttendance == null,
                            HasActiveAttendance = existingAttendance != null,
                            ActiveAttendanceId = existingAttendance?.Id,
                            SessionCode = existingAttendance?.SessionCode 
                        });
                    }
                }
            }

            // Sắp xếp theo ngày và ca dạy
            return result.OrderBy(r => r.startdate).ThenBy(r => r.starttime).ToList();
        }
        private bool CanCreateAttendanceForSchedule(Schedule schedule, DateTime date)
        {
            // Chỉ cho phép tạo điểm danh cho ngày hôm nay
            if (date.Date != DateTime.Today)
                return false;

            // Kiểm tra hôm nay có phải ngày học không
            var today = (Weekday)DateTime.Today.DayOfWeek;
            var validDays = schedule.ScheduleDays.Select(sd => sd.DayOfWeekk.Weekdays).ToList();

            if (!validDays.Contains(today))
                return false;

            // Kiểm tra thời gian (trong 30 phút đầu ca học)
            if (!schedule.StudyShift?.StartTime.HasValue == true)
                return false;

            var timeSpan = schedule.StudyShift.StartTime.Value.ToTimeSpan();
            var now = DateTime.Now;
            var startTime = DateTime.Today.Add(timeSpan);
            var endTime = startTime.AddMinutes(30);

            return now >= startTime && now <= endTime;
        }
        // Helper method để lấy tất cả các ngày thuộc thứ cụ thể trong khoảng thời gian
        private List<DateTime> GetDatesForWeekday(DateTime startDate, DateTime endDate, Weekday weekday)
        {
            var dates = new List<DateTime>();

            // Chuyển đổi Weekday enum sang DayOfWeek enum
            var targetDayOfWeek = ConvertWeekdayToDayOfWeek(weekday);

            var current = startDate;

            // Tìm ngày đầu tiên thuộc thứ cần tìm
            while (current.DayOfWeek != targetDayOfWeek && current <= endDate)
            {
                current = current.AddDays(1);
            }

            // Thêm tất cả các ngày thuộc thứ này trong khoảng thời gian
            while (current <= endDate)
            {
                dates.Add(current);
                current = current.AddDays(7); // Thêm 1 tuần
            }

            return dates;
        }

        // Helper method để chuyển đổi Weekday enum sang System.DayOfWeek
        private DayOfWeek ConvertWeekdayToDayOfWeek(Weekday weekday)
        {
            return weekday switch
            {
                Weekday.Monday => DayOfWeek.Monday,
                Weekday.Tuesday => DayOfWeek.Tuesday,
                Weekday.Wednesday => DayOfWeek.Wednesday,
                Weekday.Thursday => DayOfWeek.Thursday,
                Weekday.Friday => DayOfWeek.Friday,
                Weekday.Saturday => DayOfWeek.Saturday,
                Weekday.Sunday => DayOfWeek.Sunday,
                _ => DayOfWeek.Monday
            };
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

            // Lấy danh sách sinh viên hợp lệ VÀ đang hoạt động
            var validStudentIds = await _context.StudentsInfors
                .Where(s => request.StudentIds.Contains(s.UserId) && s.User.Statuss == true) // Thêm điều kiện IsActive
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
