using API.Data;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _client;
        private readonly AduDbcontext _context;
        public AttendanceController(HttpClient client, AduDbcontext context, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
            _context = context;
            _httpClientFactory = httpClientFactory;
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
        public async Task<IActionResult> IndexSession(int? classId, int? studyShiftid, int? roomid, int? subjectid)
        {
            var queryParams = new List<string>();
            if (classId.HasValue)
            {
                queryParams.Add($"classid={classId.Value}");
            }
            if (studyShiftid.HasValue)
            {
                queryParams.Add($"studyShiftid={studyShiftid.Value}");
            }
            if (roomid.HasValue)
            {
                queryParams.Add($"roomid={roomid.Value}");
            }
            if (subjectid.HasValue)
            {
                queryParams.Add($"subject={subjectid.Value}");
            }

            string query = queryParams.Count > 0 ? "api/attendance?" + string.Join("&", queryParams) : "api/attendance";
            var response = await _client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thấy ";
                return View(new List<IndexAttendanceViewModel>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<IndexAttendanceViewModel>>(json);
            await LoadSelectitem();
            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var response = await _client.GetAsync($"api/attendance/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không tìm thấy phiên điểm danh.";
                return RedirectToAction("IndexSession");
            }

            var data = await response.Content.ReadFromJsonAsync<IndexAttendanceViewModel>();
            return View(data);
        }


        [HttpPost]
        public async Task<IActionResult> Detail(CheckInDto dto)
        {
            var response = await _client.PostAsJsonAsync("api/attendance/checkin", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "✅ Điểm danh thành công";
            }
            else
            {
                TempData["Error"] = "❌ Điểm danh thất bại";
            }


            var viewResponse = await _client.GetAsync($"api/attendance/{dto.AttendanceId}");
            if (!viewResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("IndexSession");
            }

            var viewData = await viewResponse.Content.ReadFromJsonAsync<IndexAttendanceViewModel>();
            return View(viewData);
        }

        public async Task<IActionResult> History()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }
            // Gọi API lấy lịch học
            var response = await client.GetAsync("api/Attendance/history");
            if (!response.IsSuccessStatusCode)
            {

                return View(new List<StudentAttendanceHistory>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<StudentAttendanceHistory>>(json);

            return View(data);

        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var sche = await _context.Schedules.ToListAsync();
            ViewBag.Schedules = sche.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.ClassName.ToString()
            }).ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateAttendanceViewModel model)
        {
            var response = await _client.PostAsJsonAsync("api/Attendance", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo phiên điểm danh thành công!";
                return RedirectToAction(nameof(IndexSession));
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Tạo phiên điểm danh thất bại. Lỗi: {error}";
                return RedirectToAction(nameof(IndexSession));
            }
        }

        public async Task LoadSelectitem()
        {
            var classList = await _context.Schedules.ToListAsync();
            ViewBag.ClassList = classList.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.ClassName
            }).ToList();
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
            var subject = await _context.Subjects.ToListAsync();
            ViewBag.SubjectList = subject.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.SubjectName
            }).ToList();
        }
    }
}