using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;

namespace API.Services.Repositories
{
    public class Attendancerepos : IAttendance
    {
        private readonly AduDbcontext _context;
        private readonly IWebHostEnvironment _env;

        public Attendancerepos(AduDbcontext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task CreateSession(CreateAttendanceSessionViewModel model)
        {
            var session = new Attendance()
            {
                SchedulesId = model.SchedulesId,
                UserId = model.CreatedByUserId,
                Starttime = DateTime.Now,
                Endtime = DateTime.Now.AddMinutes(30),
                CreateAt = DateTime.Now,
                SessionCode = Guid.NewGuid().ToString().Substring(0, 6)
            };
            _context.Attendances.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudentAttendanceViewModel>> GetStudentsForAttendance(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Schedules)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(s => s.User)
                            .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (attendance == null)
                throw new Exception("Không tìm thấy phiên điểm danh");

            var checkedIns = await _context.AttendanceDetails
                .Where(x => x.AttendanceId == id)
                .Select(x => x.StudentId)
                .ToListAsync();

            return attendance.Schedules.Class.Students.Select(s => new StudentAttendanceViewModel
            {
                StudentId = s.UserId,
                StudentCode = s.StudentsCode,
                FullName = s.User?.UserName,
                Email = s.User?.Email,
                Avatar = s.User?.UserProfile?.Avatar,
                HasCheckedIn = checkedIns.Contains(s.UserId)
            }).ToList();
        }

        public async Task<(bool match, string message)> Recognize(string base64, int attendanceId)
        {
            var clean64 = base64.Replace("data:image/jpeg;base64,", "").Replace(" ", "+");
            var imageBytes = Convert.FromBase64String(clean64);
            var captured = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);

            if (captured.Empty()) return (false, "❌ Ảnh không hợp lệ");

            var faces = new List<Mat>();
            var labels = new List<int>();
            var labelMap = new Dictionary<int, Guid>();

            int label = 0;
            var users = _context.Users.Include(u => u.UserProfile).Where(u => u.UserProfile != null).ToList();

            foreach (var user in users)
            {
                var path = Path.Combine(_env.WebRootPath, user.UserProfile.Avatar.TrimStart('/'));
                if (!System.IO.File.Exists(path)) continue;

                var img = Cv2.ImRead(path, ImreadModes.Grayscale);
                if (img.Empty()) continue;

                Cv2.EqualizeHist(img, img);
                faces.Add(img);
                labels.Add(label);
                labelMap[label] = user.Id;
                label++;
            }

            if (faces.Count < 2) return (false, "❌ Dữ liệu mẫu không đủ để nhận diện.");

            var recognizer = OpenCvSharp.Face.LBPHFaceRecognizer.Create();
            recognizer.Train(faces, labels);
            recognizer.Predict(captured, out int predictLabel, out double confidence);

            if (confidence < 80 && labelMap.TryGetValue(predictLabel, out Guid userId))
            {
                var detail = new AttendanceDetail
                {
                    StudentId = userId,
                    AttendanceId = attendanceId,
                    Status = AttendanceDetail.AttendanceStatus.Present,
                    CheckinTime = DateTime.Now,
                    ImagePath = SaveImage(userId, imageBytes)
                };

                _context.AttendanceDetails.Add(detail);
                await _context.SaveChangesAsync();
                return (true, $"✅ Chào {userId}, điểm danh thành công ({confidence:0.00})");
            }

            return (false, "❌ Không nhận diện được.");
        }

        private string SaveImage(Guid userId, byte[] bytes)
        {
            var folder = Path.Combine(_env.WebRootPath, "evidence");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var file = $"{userId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var path = Path.Combine(folder, file);
            System.IO.File.WriteAllBytes(path, bytes);
            return $"/evidence/{file}";
        }

        public async Task<List<AttendancesessionViewModel>> GetAllSessions()
        {
            return await _context.Attendances
            .Include(a => a.Schedules)
                .ThenInclude(s => s.Class)
                .Select(a => new AttendancesessionViewModel
                {
                    
                    SessionCode = a.SessionCode,
                    NameClass = a.Schedules.Class.NameClass,
                    StartTime = a.Starttime.HasValue? TimeOnly.FromDateTime(a.Starttime.Value):null,
                    EndTime = a.Endtime.HasValue ? TimeOnly.FromDateTime(a.Endtime.Value) : null,
                }).ToListAsync();
        }

        public async Task<List<StudentCheckInSessionViewModel>> GetSessionsForStudent(Guid studentId)
        {
            var sessions = await _context.StudentsInfors
                .Where(sc => sc.UserId == studentId)
                .Include(sc => sc.Classes)
                .ThenInclude(c => c.Schedules)
                .ThenInclude(s => s.Attendances)
                .SelectMany(sc => sc.Classes.FirstOrDefault().Schedules.SelectMany(s => s.Attendances))
                .ToListAsync();

            return sessions.Select(s => new StudentCheckInSessionViewModel
            {
                AttendanceId = s.Id,
                ClassName = s.Schedules.Class.NameClass,
                StartTime = s.Starttime,
                EndTime = s.Endtime,
            }).ToList();
        }
    }
}
