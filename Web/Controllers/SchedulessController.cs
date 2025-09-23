using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Web.Controllers
{
    public class SchedulessController : Controller
    {
        private readonly HttpClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AduDbcontext _context;
        public SchedulessController(HttpClient client, AduDbcontext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _client = client;
            _httpClientFactory = httpClientFactory;
            _client.BaseAddress = new Uri("https://localhost:7298/");
        }

        private HttpClient? GetClientWithToken()
        {
            var client = _httpClientFactory.CreateClient("EdustarAPI");
            if (!Request.Cookies.TryGetValue("JWToken", out var token) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return null;
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index1()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            
            var response = await client.GetAsync("api/Schedules");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không lấy được danh sách lịch học.";
                return View(new List<SchedulesViewModel>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<SchedulesViewModel>>(json);
            return View(result);
        }

        public async Task<IActionResult> Index()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            var response = await _client.GetAsync("api/Schedules/codinh");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không lấy được danh sách lịch học cố định.";
                return View(new List<SchedulesViewModel>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<Lichcodinh>>(json);
            //var result = JsonConvert.DeserializeObject<List<Lichcodinh>>(response);
            return View(result);
        }

        public async Task<IActionResult> ByStudent()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var response = await client.GetAsync($"api/schedules/Id");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không lấy được lịch học";
                return View(new List<SchedulesViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var schedules = JsonConvert.DeserializeObject<List<SchedulesViewModel>>(json);
            return View(schedules);
        }

        public async Task<IActionResult> ByTeacher()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var response = await client.GetAsync($"api/schedules/Teacher");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không lấy được lịch học";
                return View(new List<SchedulesViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var schedules = JsonConvert.DeserializeObject<List<SchedulesViewModel>>(json);
            return View(schedules);
        }

        public async Task<IActionResult> Auto(Schedule sc)
        {
            var response = await _client.PostAsJsonAsync("api/Schedules/arrangeschedules", sc);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int Id)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            var response = await client.GetAsync($"api/Schedules/{Id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không lấy được chi tiết lịch học.";
                return RedirectToAction("Index1");
            }
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SchedulesViewModel>(json);
            //var result = JsonConvert.DeserializeObject<SchedulesViewModel>(response);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            await LoadSelectitem();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SchedulesDTO dto)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            if (!ModelState.IsValid)
            {
                await LoadSelectitem();
                return View(dto);
            }
            var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/Schedules/create", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, error);
                await LoadSelectitem();
                return View(dto);
            }

            return RedirectToAction("Index1");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            try
            {
                var client = GetClientWithToken();
                if (client == null)
                {
                    return RedirectToAction("Login", "Users");
                }
                var response = await client.GetAsync($"api/Schedules/{Id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không lấy được chi tiết lịch học.";
                    return RedirectToAction("Index1");
                }
                var json = await response.Content.ReadAsStringAsync();
                var schedules = JsonConvert.DeserializeObject<SchedulesDTO>(json);
                //var schedules = JsonConvert.DeserializeObject<SchedulesDTO>(response);
                await LoadSelectitemEdit(schedules);
                return View(schedules);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi lấy dữ liệu: {ex.Message}";
                return RedirectToAction("Index1");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SchedulesDTO model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectitemEdit(model);
                return View(model);
            }

            try
            {
                var client = GetClientWithToken();
                if (client == null)
                {
                    return RedirectToAction("Login", "Users");
                }
                Console.WriteLine($"[MVC] Updating schedule ID: {model.Id}");
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                // Đảm bảo URL đúng với API Controller
                var response = await client.PutAsync($"api/Schedules/{model.Id}", content);

                Console.WriteLine($"[MVC] API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật lịch học thành công!";
                    return RedirectToAction("Index1");
                }

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[MVC] API Error: {error}");

                // Parse JSON error response nếu có
                try
                {
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(error);
                    ModelState.AddModelError("", errorObj?.message?.ToString() ?? error);
                }
                catch
                {
                    ModelState.AddModelError("", error);
                }

                await LoadSelectitemEdit(model);
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MVC] Exception: {ex}");
                ModelState.AddModelError("", $"Lỗi kết nối: {ex.Message}");
                await LoadSelectitemEdit(model);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int Id)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            try
            {
                var response = await _client.PutAsync($"api/Schedules/toggle-status/{Id}", null);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    string message = result?.message?.ToString() ?? "Thay đổi trạng thái thành công";
                    bool isActive = result?.isActive ?? false;

                    TempData["SuccessMessage"] = message;
                    TempData["ScheduleStatus"] = isActive ? "Hoạt động" : "Vô hiệu hóa";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Thay đổi trạng thái thất bại: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}";
            }

            return RedirectToAction(nameof(Index1));
        }
        [HttpGet]
        public async Task<IActionResult> CreateQuickSession()
        {
            return View();
        }

        private string GenerateSessionCode()
        {
            return DateTime.Now.ToString("yyyyMMddHHmm") + new Random().Next(1000, 9999);
        }
        // Giữ nguyên action POST hiện tại của bạn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuickSession(int scheduleId, string sessionCode)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
            }

            try
            {
                var data = new { scheduleId, sessionCode };
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/TeacherAttendance/CreateQuickSession", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    TempData["SuccessMessage"] = "Tạo phiên điểm danh thành công!";
                    return RedirectToAction("ByTeacher"); // về danh sách lịch dạy hoặc view bạn muốn
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo phiên điểm danh";
                    return RedirectToAction("ByTeacher");
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task LoadSelectitem()
        {
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

            // ✅ Ca học kèm giờ bắt đầu - giờ kết thúc
            var studyShift = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.StudyShiftName} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"
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

            // ✅ Ca học kèm giờ bắt đầu - giờ kết thúc
            var studyShift = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.StudyShiftName} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})",
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
            var client = GetClientWithToken();
            await LoadStudentListForClass(Id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudent(AssignStudentsRequest request)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            Console.WriteLine(">> [MVC] Nhận classId: " + request.SchedulesId);
            Console.WriteLine(">> [MVC] Số sinh viên chọn: " + (request.StudentIds?.Count ?? 0));

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                ViewBag.ErrorMessage = "Bạn chưa chọn sinh viên nào.";
                await LoadStudentListForClass(request.SchedulesId);
                return View();
            }
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"api/schedules/{request.SchedulesId}/assignStudent/{request.StudentIds}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gán sinh viên thành công!";
                return RedirectToAction("Details", new { id = request.SchedulesId });
            }

            var errorContent = await response.Content.ReadAsStringAsync();

            // Parse JSON error để lấy message cụ thể
            try
            {
                var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
                string errorMessage = errorObj?.message?.ToString() ?? errorContent;

                // Kiểm tra lỗi về lịch học bị vô hiệu hóa
                if (errorMessage.Contains("vô hiệu hóa"))
                {
                    ViewBag.ErrorMessage = "Không thể thêm sinh viên: " + errorMessage;
                }
                else
                {
                    ViewBag.ErrorMessage = $"Lỗi khi gọi API: {errorMessage}";
                }
            }
            catch
            {
                ViewBag.ErrorMessage = $"Lỗi khi gọi API: {errorContent}";
            }

            await LoadStudentListForClass(request.SchedulesId);
            return View();
        }
        private async Task LoadStudentListForClass(int classId)
        {
            var cls = await _context.Schedules.FindAsync(classId);

            // Kiểm tra lịch học có đang hoạt động không
            if (cls != null && !cls.IsActive)
            {
                ViewBag.WarningMessage = "Lịch học này đã bị vô hiệu hóa. Không thể thêm sinh viên mới.";
            }

            var allStudents = await _context.StudentsInfors
                .Select(s => new StudentDTO
                {
                    id = s.UserId,
                    StudentCode = s.StudentsCode,
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
            ViewBag.IsScheduleActive = cls?.IsActive ?? false;
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