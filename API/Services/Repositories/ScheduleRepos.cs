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

        public async Task AutogenerateSchedule()
        {
            var classes = await _context.Classes
                .Include(c => c.Schedules) 
                .Include(c => c.Subject)
                .ToListAsync();

            var days = await _context.DayOfWeeks.ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();
            var shifts = await _context.StudyShifts.ToListAsync();

            List<Schedule> newSchedules = new List<Schedule>();

            foreach (var cls in classes)
            {
                if (cls.Schedules != null && cls.Schedules.Any()) continue;

                var startDate = cls.StartTime ?? DateTime.Today;
                int durationWeeks = 4; 

                bool scheduled = false;

                foreach (var room in rooms)
                {
                    foreach (var shift in shifts)
                    {
                        var usedDayIds = _context.Schedules
                            .Where(s => s.RoomId == room.Id && s.StudyShiftId == shift.Id)
                            .Select(s => s.DayId)
                            .ToList();

                        usedDayIds.AddRange(newSchedules
                            .Where(s => s.RoomId == room.Id && s.StudyShiftId == shift.Id)
                            .Select(s => s.DayId));

                        var availableDays = days
                            .Where(d => !usedDayIds.Contains(d.Id))
                            .Take(3)
                            .ToList();

                        if (availableDays.Count == 3)
                        {
                            for (int i = 0; i < durationWeeks; i++)
                            {
                                foreach (var day in availableDays)
                                {
                                    var studyDate = GetNextWeekday(startDate.AddDays(i * 7), (DayOfWeek)day.Weekdays);

                                    newSchedules.Add(new Schedule
                                    {
                                        ClassId = cls.Id,
                                        RoomId = room.Id,
                                        DayId = day.Id,
                                        StudyShiftId = shift.Id,
                                        StartDate = studyDate.Date,
                                        EndDate = studyDate.Date.AddHours(2)
                                    });
                                }
                            }

                            scheduled = true;
                            break;
                        }
                    }

                    if (scheduled) break;
                }

                if (!scheduled)
                {
                    Console.WriteLine($"Không thể xếp đủ 3 buổi cho lớp {cls.NameClass}");
                }
            }

            if (newSchedules.Any())
            {
                await _context.Schedules.AddRangeAsync(newSchedules);
                await _context.SaveChangesAsync();
            }
        }

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public async Task<Schedule> CreateSchedules(SchedulesDTO model)
        {

            bool istrung = _context.Schedules.Any(
                i => i.Id != model.Id
                && i.ClassId == model.ClassId
                && i.StudyShiftId == model.StudyShiftId
                && i.RoomId == model.RoomId
                && i.DayId == model.WeekDayId);
            if (istrung)
                throw new Exception("Có lịch trùng");
            
            var sc = new Schedule()
            {
                Id = model.Id,
                ClassId = model.ClassId,
                RoomId = model.RoomId,
                DayId = model.WeekDayId,
                StudyShiftId = model.StudyShiftId,
            };
            _context.Schedules.Add(sc);
            await _context.SaveChangesAsync();
            return sc;
        }

        public async Task DeleteSchedules(int Id)
        {
            var delete =await _context.Schedules.FirstOrDefaultAsync(c => c.Id == Id);
            _context.Schedules.Remove(delete);
            await _context.SaveChangesAsync();
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
            int row = 2;
            int stt = 1;
            foreach (var item in model)
            {
                worksheets.Cells[row, 1].Value = stt++;
                worksheets.Cells[row, 2].Value = item.ClassName;
                worksheets.Cells[row, 3].Value = item.RoomCode;
                worksheets.Cells[row, 4].Value = item.WeekDay;
                worksheets.Cells[row, 5].Value = item.StudyShift;
                row++;
            }
            return package.GetAsByteArray();
        }

        public async Task<List<Schedule>> GetAll()
        {
            var schedule =await _context.Schedules
                .Include(c=>c.Class)
                .ThenInclude(s=>s.Subject)
                .Include(r=>r.Room)
                .Include(d=>d.Day)
                .Include(s=> s.StudyShift)
                .AsSplitQuery().ToListAsync();
            return schedule;
        }

        public async Task<SchedulesViewModel> GetById(int id)
        {
            var schedule = await _context.Schedules
                .Include(c=>c.Class)
                .ThenInclude(s=>s.Subject)
                .Include (r=>r.Room)
                .Include(d=>d.Day)
                .Include(s=> s.StudyShift)
                .AsSplitQuery().FirstOrDefaultAsync(e=> e.Id ==id);
            var model = new SchedulesViewModel()
            {
                Id = schedule.Id,
                ClassName = schedule.Class.NameClass,
                SubjectName = schedule.Class.Subject.SubjectName,
                RoomCode = schedule.Room.RoomCode,
                WeekDay = schedule.Day.Weekdays.ToString(),
                StudyShift = schedule.StudyShift.StudyShiftName,
                starttime =schedule.StudyShift.StartTime,
                endtime =schedule.StudyShift.EndTime,
                
            };
            return model;
        }

        public async Task<List<SchedulesViewModel>> GetByStudent(Guid Id)
        {
           
            var student = await _context.StudentsInfors
            .Include(s => s.Classes)
            .FirstOrDefaultAsync(s => s.UserId == Id);

            if (student == null || student.Classes == null)
            {
                Console.WriteLine("Không tìm thấy sinh viên hoặc lớp học.");
                return new List<SchedulesViewModel>();
            }

            var classId = student.Classes.FirstOrDefault().Id;

            var schedule = await _context.Schedules
                .Where(sc => sc.ClassId == classId)
                .Include(sc => sc.Day)
                .Include(sc => sc.Room)
                .Include(sc => sc.StudyShift)
                .Include(sc => sc.Class)
                .Select(sc => new SchedulesViewModel
                {
                    Id = sc.Id,
                    ClassName = sc.Class.NameClass,
                    WeekDay = sc.Day.Weekdays.ToString(),
                    StudyShift = sc.StudyShift.StudyShiftName,
                    RoomCode = sc.Room.RoomCode,
                })
                .ToListAsync();

            return schedule;
        }

        public async Task UpdateSchedules(SchedulesDTO model)
        {

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (schedule == null)
                throw new Exception("Không tìm thấy lịch học cần cập nhật.");
            bool istrung = _context.Schedules.Any(
                i => i.Id != model.Id
                && i.ClassId==model.ClassId
                && i.StudyShiftId==model.StudyShiftId
                && i.RoomId==model.RoomId
                && i.DayId ==model.WeekDayId);
            if (istrung)
                throw new Exception("Có lịch trùng"); 
            schedule.ClassId = model.ClassId;
            schedule.StudyShiftId = model.StudyShiftId;
            schedule.DayId = model.WeekDayId;
            schedule.RoomId = model.RoomId;

            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();
        }         
    }
}
