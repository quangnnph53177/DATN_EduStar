using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

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
            var classes = _context.Classes
                .Include(c => c.Schedules)
                .ToList();

            var classnoschedules = classes
                .Where(c => c.Schedules == null || !c.Schedules.Any())
                .ToList();

            var days = _context.DayOfWeeks.ToList();
            var rooms = _context.Rooms.ToList();
            var shifts = _context.StudyShifts.ToList();

            List<Schedule> newSchedules = new List<Schedule>();

            foreach (var cls in classnoschedules)
            {
                bool scheduled = false;

                foreach (var room in rooms)
                {
                    foreach (var shift in shifts)
                    {
                        // Danh sách các DayId đã bị chiếm (từ DB hoặc trong danh sách đang chuẩn bị lưu)
                        var usedDayIds = _context.Schedules
                            .Where(s => s.RoomId == room.Id && s.StudyShiftId == shift.Id)
                            .Select(s => s.DayId)
                            .ToList();

                        // Cộng thêm các lịch mới đang chờ lưu (tránh trùng với lớp khác cùng lúc)
                        usedDayIds.AddRange(newSchedules
                            .Where(s => s.RoomId == room.Id && s.StudyShiftId == shift.Id)
                            .Select(s => s.DayId));

                        var availableDays = days
                            .Where(d => !usedDayIds.Contains(d.Id))
                            .Take(3)
                            .ToList();

                        if (availableDays.Count == 3)
                        {
                            foreach (var day in availableDays)
                            {
                                newSchedules.Add(new Schedule
                                {
                                    ClassId = cls.Id,
                                    RoomId = room.Id,
                                    DayId = day.Id,
                                    StudyShiftId = shift.Id
                                });
                            }

                            scheduled = true;
                            break; // break shift
                        }
                    }

                    if (scheduled) break; // break room
                }

                if (!scheduled)
                {
                    Console.WriteLine($"Không thể xếp đủ 3 buổi cho lớp {cls.NameClass}");
                }
            }

            if (newSchedules.Any())
            {
                _context.Schedules.AddRange(newSchedules);
                await _context.SaveChangesAsync();
            }
        }
        public async Task CreateSchedules(SchedulesViewModel model)
        {
              

        }
        
        public async Task<List<Schedule>> GetAll()
        {
            var schedule =await _context.Schedules
                .Include(c=>c.Class)
                .Include(r=>r.Room)
                .Include(d=>d.Day)
                .Include(s=> s.StudyShift)
                .AsSplitQuery().ToListAsync();
            return schedule;
        }

        public async Task<List<SchedulesViewModel>> GetByStudent(Guid Id)
        {
            //var students = await _context.StudentsInfors
            //     .Include(s => s.Classes)
            //         .ThenInclude(c => c.Schedules)
            //             .ThenInclude(sc => sc.Room)
            //     .Include(s => s.Classes)
            //         .ThenInclude(c => c.Schedules)
            //             .ThenInclude(sc => sc.Day)
            //     .Include(s => s.Classes)
            //         .ThenInclude(c => c.Schedules)
            //             .ThenInclude(sc => sc.StudyShift)
            //     .FirstOrDefaultAsync(st => st.UserId == Id);
            //var model = new SchedulesViewModel()
            //{
            //    Id = students.Classes.FirstOrDefault().Schedules.FirstOrDefault().Id,
            //    ClassName = students.Classes.FirstOrDefault().NameClass,
            //    StudyShift =students.Classes.FirstOrDefault().Schedules.FirstOrDefault().StudyShift.StudyShiftName,
            //    RoomCode = students.Classes.FirstOrDefault().Schedules.FirstOrDefault().Room.RoomCode,
            //    WeekDay = students.Classes.FirstOrDefault().Schedules.FirstOrDefault().Day.Weekdays
            //};

            //return model;
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
                    WeekDay = sc.Day.Weekdays,
                    StudyShift = sc.StudyShift.StudyShiftName,
                    RoomCode = sc.Room.RoomCode,
                })
                .ToListAsync();

            return schedule;
        }
    }
}
