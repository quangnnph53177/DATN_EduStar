
using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class SchedulessController : Controller
    {
        private readonly HttpClient _client;
        private readonly AduDbcontext _context;
        public SchedulessController(HttpClient client, AduDbcontext context)
        {
            _context = context;
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
        }
        public async Task<IActionResult> Index1()
        {
            var response = await _client.GetStringAsync("api/Schedules");
            var result = JsonConvert.DeserializeObject<List<SchedulesViewModel>>(response);
            return View(result);
        }
        public async Task<IActionResult> Index()
        {
            var response = await _client.GetStringAsync("api/Schedules/codinh");
            var result = JsonConvert.DeserializeObject<List<Lichcodinh>>(response);
            return View(result);

        }
        public async Task<IActionResult> Auto(Schedule sc)
        {
            var response = await _client.PostAsJsonAsync("api/Schedules/arrangeschedules", sc);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int Id)
        {
            var response = await _client.GetStringAsync($"api/Schedules/{Id}");
            var result = JsonConvert.DeserializeObject<SchedulesViewModel>(response);
            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSelectitem();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(SchedulesDTO dto)
        {
            var response = await _client.PostAsJsonAsync("api/Schedules/create", dto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, error); // <== đây sẽ đẩy lỗi vào ValidationSummary

                await LoadSelectitem();
                return View(dto);
            }

            return RedirectToAction("Index1");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var response = await _client.GetStringAsync($"api/schedules/{Id}");
            var schedules = JsonConvert.DeserializeObject<SchedulesDTO>(response);
            await LoadSelectitemEdit(schedules);
            return View(schedules);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(SchedulesDTO model)
        {
            var response = await _client.PutAsJsonAsync($"api/schedules/{model.Id}", model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index1");
            }
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            await LoadSelectitemEdit(model);
            return View(model);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var response = await _client.DeleteAsync($"api/Schedules/{Id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Xóa thành công!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Xóa thất bại!";
            }

            return RedirectToAction(nameof(Index1));
        }
        public async Task LoadSelectitem()
        {
            //var classList = await _context.Schedules.ToListAsync();
            //ViewBag.ClassList = classList.Select(c => new SelectListItem
            //{
            //    Value = c.Id.ToString(),
            //    Text = c.ClassName,
            //}).ToList();
            var roomlist = await _context.Rooms.ToListAsync();
            ViewBag.RoomList = roomlist.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoomCode
            }).ToList();
            var weekday = await _context.DayOfWeeks.ToListAsync();
            ViewBag.DayList = weekday.Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = w.Weekdays.ToString()
            }).ToList();
            var studyShift = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.StudyShiftName
            }).ToList();
            var subjectList = await _context.Subjects.ToListAsync();
            ViewBag.SubjectList = subjectList.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.SubjectName
            }).ToList();
            var TeacherList = await _context.Users.Where(r => r.Roles.Any(c => c.Id == 2)).ToListAsync();
            ViewBag.TeacherList = TeacherList.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.UserName,
            });

        }
        public async Task LoadSelectitemEdit(SchedulesDTO model)
        {
            var roomlist = await _context.Rooms.ToListAsync();
            ViewBag.RoomList = roomlist.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoomCode,
                Selected = model.RoomId == r.Id
            }).ToList();

            var weekday = await _context.DayOfWeeks.ToListAsync();
            ViewBag.DayList = weekday.Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = w.Weekdays.ToString(),
                Selected = model.WeekDayId != null && model.WeekDayId.Contains(w.Id)
            }).ToList();

            var studyShift = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.StudyShiftName,
                Selected = model.StudyShiftId == s.Id
            }).ToList();

            var subjectList = await _context.Subjects.ToListAsync();
            ViewBag.SubjectList = subjectList.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.SubjectName,
                Selected = model.SubjectId == s.Id
            }).ToList();

            var teacherList = await _context.Users
                .Where(r => r.Roles.Any(c => c.Id == 2))
                .ToListAsync();

            ViewBag.TeacherList = teacherList.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.UserName,
                Selected = model.TeacherId == t.Id
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> AssignStudent(int Id)
        {
            await LoadStudentListForClass(Id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudent(AssignStudentsRequest request)
        {
            Console.WriteLine(">> [MVC] Nhận classId: " + request.SchedulesId);
            Console.WriteLine(">> [MVC] Số sinh viên chọn: " + (request.StudentIds?.Count ?? 0));

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                ViewBag.ErrorMessage = "Bạn chưa chọn sinh viên nào.";
                await LoadStudentListForClass(request.SchedulesId);
                return View();
            }

            var response = await _client.PostAsJsonAsync($"api/schedules/{request.SchedulesId}/assignStudent/{request.StudentIds}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gán sinh viên thành công!";
                return RedirectToAction("Details", new { id = request.SchedulesId });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ViewBag.ErrorMessage = $"Lỗi khi gọi API: {errorContent}";
            await LoadStudentListForClass(request.SchedulesId);
            return View();
        }

        private async Task LoadStudentListForClass(int classId)
        {
            var cls = await _context.Schedules.FindAsync(classId);

            var allStudents = await _context.StudentsInfors
                .Select(s => new StudentDTO
                {
                    id = s.UserId,
                    FullName = s.User.UserProfile.FullName,
                    Email = s.User.Email
                })
                .ToListAsync();

            var assignedStudentIds = await _context.ScheduleStudentsInfors
                .Where(sc => sc.SchedulesId == classId)
                .Select(sc => sc.StudentsUserId)
                .ToListAsync();

            var unassignedStudents = allStudents
                .Where(s => !assignedStudentIds.Contains(s.id))
                .ToList();

            ViewBag.ClassId = classId;
            ViewBag.NameClass = cls?.ClassName;
            ViewBag.StudentList = unassignedStudents;

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int SchedulesId, [FromForm] Guid studentId)
        {
            try
            {
                var response = await _client.DeleteAsync($"api/Schedules/{SchedulesId}/removeStudent/{studentId}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Sinh viên đã được xóa khỏi lớp thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Error removing student: {response.ReasonPhrase}. Details: {errorContent}";
                }
            }
            catch (HttpRequestException e)
            {
                TempData["ErrorMessage"] = "Request error: " + e.Message;
            }
            return RedirectToAction("Details", new { id = SchedulesId });
        }
    }
}
