﻿// ScheduleRepos.cs

using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OpenCvSharp.Features2D;

namespace API.Services.Repositories
{
    public class ScheduleRepos : IShedulesRepos
    {
        private readonly AduDbcontext _context;
        public ScheduleRepos(AduDbcontext context)
        {
            _context = context;
        }

        // Tự động tạo lịch học cố định cho các lớp chưa có lịch
        public async Task AutogenerateSchedule()
        {
            var classes = await _context.Classes
                .Include(c => c.Schedules)
                .Include(c => c.Subject)
                .ToListAsync();

            var days = await _context.DayOfWeeks.ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();
            var shifts = await _context.StudyShifts.ToListAsync();

            List<Schedule> newSchedules = new();

            foreach (var cls in classes)
            {
                if (cls.Schedules != null && cls.Schedules.Any()) continue;

                var startDate = cls.StartTime ?? DateTime.Today;
                int sessionsPerWeek = 3;

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
                            .Take(sessionsPerWeek)
                            .ToList();

                        if (availableDays.Count == sessionsPerWeek)
                        {
                            foreach (var day in availableDays)
                            {
                                newSchedules.Add(new Schedule
                                {
                                    ClassId = cls.Id,
                                    RoomId = room.Id,
                                    DayId = day.Id,
                                    StudyShiftId = shift.Id,
                                    StartDate = startDate,
                                    EndDate = startDate.AddMonths(1) // ví dụ học 1 tháng
                                });
                            }
                            scheduled = true;
                            break;
                        }
                    }
                    if (scheduled) break;
                }
            }
            if (newSchedules.Any())
            {
                await _context.Schedules.AddRangeAsync(newSchedules);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Schedule> CreateSchedules(SchedulesDTO model)
        {
                if (model.startdate == null)
                    throw new Exception("Ngày bắt đầu không được để trống.");

                var startDate = model.startdate.Value.Date;

                var conflict = await _context.Schedules
                    .FirstOrDefaultAsync(s =>
                        s.RoomId == model.RoomId &&
                        s.StudyShiftId == model.StudyShiftId &&
                        s.DayId == model.WeekDayId &&
                        s.StartDate == startDate);

                if (conflict != null)
                {
                    List<string> conflicts = new();

                    if (conflict.RoomId == model.RoomId)
                        conflicts.Add("phòng");
                    if (conflict.StudyShiftId == model.StudyShiftId)
                        conflicts.Add("ca học");
                    if (conflict.DayId == model.WeekDayId)
                        conflicts.Add("thứ");
                    if (conflict.StartDate.Value.Date == startDate)
                        conflicts.Add("ngày");

                    string message = "Lịch bị trùng: " + string.Join(", ", conflicts);
                    throw new Exception(message);
                }

                var sc = new Schedule
                {
                    ClassId = model.ClassId,
                    RoomId = model.RoomId,
                    DayId = model.WeekDayId,
                    StudyShiftId = model.StudyShiftId,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(30)
                };

                _context.Schedules.Add(sc);
                await _context.SaveChangesAsync();

                return sc;

        }



        public async Task DeleteSchedules(int Id)
        {
            var delete = await _context.Schedules.FirstOrDefaultAsync(c => c.Id == Id);
            _context.Schedules.Remove(delete);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Schedule>> GetAll()
        {
            return await _context.Schedules
                .Include(c => c.Class).ThenInclude(s => s.Subject)
                .Include(r => r.Room)
                .Include(d => d.Day)
                .Include(s => s.StudyShift)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<SchedulesViewModel> GetById(int id)
        {
            var schedule = await _context.Schedules
                .Include(c => c.Class).ThenInclude(s => s.Subject)
                .Include(r => r.Room)
                .Include(d => d.Day)
                .Include(s => s.StudyShift)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            return new SchedulesViewModel
            {
                Id = schedule.Id,
                ClassName = schedule.Class.NameClass,
                SubjectName = schedule.Class.Subject.SubjectName,
                RoomCode = schedule.Room.RoomCode,
                WeekDay = schedule.Day.Weekdays.ToString(),
                StudyShift = schedule.StudyShift.StudyShiftName,
                starttime = schedule.StudyShift.StartTime,
                endtime = schedule.StudyShift.EndTime,
                StudentCount = schedule.Class.StudentCount,
                UserId = schedule.Class.UsersId
            };
        }

        public async Task<List<SchedulesViewModel>> GetByStudent(Guid Id)
        {
            var student = await _context.StudentsInfors
                .Include(s => s.Classes)
                .FirstOrDefaultAsync(s => s.UserId == Id);

            if (student == null || student.Classes == null)
                return new List<SchedulesViewModel>();

            var classId = student.Classes.FirstOrDefault().Id;

            return await _context.Schedules
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
        }

        public async Task<List<Lichcodinh>> GetAllCoDinh()
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Class.Subject)
                .Include(s => s.Room)
                .Include(s => s.Day)
                .Include(s => s.StudyShift)
                .GroupBy(s => new {
                    s.Class.Id,
                    s.Class.NameClass,
                    s.Class.Subject.SubjectName,
                    s.Room.RoomCode,
                    s.StudyShift.StudyShiftName
                })
                .Select(g => new Lichcodinh
                {
                    ClassId=g.Key.Id,
                    ClassName = g.Key.NameClass,
                    SubjectName = g.Key.SubjectName,
                    RoomCode = g.Key.RoomCode,
                    StudyShift = g.Key.StudyShiftName,
                    weekdays = g.Select(x => x.Day.Weekdays).Distinct().ToList()
                })
                .ToListAsync();
        }

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
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
        public async Task UpdateSchedules(SchedulesDTO model)
        {

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (schedule == null)
                throw new Exception("Không tìm thấy lịch học cần cập nhật.");
            bool istrung = _context.Schedules.Any(
                i => i.Id != model.Id
                && i.ClassId == model.ClassId
                && i.StudyShiftId == model.StudyShiftId
                && i.RoomId == model.RoomId
                && i.DayId == model.WeekDayId);
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
    
