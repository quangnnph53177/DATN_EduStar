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
    [Authorize] // Yêu cầu đăng nhập cho tất cả action
    public class AttendanceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _client;
        private readonly AduDbcontext _context;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            HttpClient client,
            AduDbcontext context,
            IHttpClientFactory httpClientFactory,
            ILogger<AttendanceController> logger)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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

        public async Task<IActionResult> IndexSession(
            int? classId,
            int? studyShiftid,
            int? roomid,
            int? subjectid,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // Validate page parameters
                if (page < 1) page = 1;
                if (pageSize < 5 || pageSize > 100) pageSize = 20;

                // Validate filter parameters
                if (classId.HasValue && classId.Value <= 0)
                {
                    ModelState.AddModelError("", "ClassId không hợp lệ");
                    classId = null;
                }

                if (studyShiftid.HasValue && studyShiftid.Value <= 0)
                {
                    ModelState.AddModelError("", "StudyShiftId không hợp lệ");
                    studyShiftid = null;
                }

                if (roomid.HasValue && roomid.Value <= 0)
                {
                    ModelState.AddModelError("", "RoomId không hợp lệ");
                    roomid = null;
                }

                if (subjectid.HasValue && subjectid.Value <= 0)
                {
                    ModelState.AddModelError("", "SubjectId không hợp lệ");
                    subjectid = null;
                }

                // Build query string
                var queryParams = new List<string>();
                if (classId.HasValue)
                    queryParams.Add($"classid={classId.Value}");
                if (studyShiftid.HasValue)
                    queryParams.Add($"studyShiftid={studyShiftid.Value}");
                if (roomid.HasValue)
                    queryParams.Add($"roomid={roomid.Value}");
                if (subjectid.HasValue)
                    queryParams.Add($"subjectid={subjectid.Value}");

                string query = queryParams.Count > 0
                    ? "api/attendance?" + string.Join("&", queryParams)
                    : "api/attendance";

                var response = await _client.GetAsync(query);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get attendance sessions. Status: {StatusCode}", response.StatusCode);
                    ViewBag.Error = "Không thể tải danh sách phiên điểm danh";
                    await LoadSelectItems();
                    return View(new List<IndexAttendanceViewModel>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<IndexAttendanceViewModel>>(json)
                    ?? new List<IndexAttendanceViewModel>();

                // Phân trang
                ViewBag.TotalItems = result.Count;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling(result.Count / (double)pageSize);

                result = result
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                await LoadSelectItems();
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IndexSession");
                ViewBag.Error = "Đã xảy ra lỗi khi tải danh sách";
                await LoadSelectItems();
                return View(new List<IndexAttendanceViewModel>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Detail(int id)
        {
            // Validate id
            if (id <= 0)
            {
                TempData["Error"] = "ID phiên điểm danh không hợp lệ";
                return RedirectToAction("IndexSession");
            }

            try
            {
                var response = await _client.GetAsync($"api/attendance/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Attendance session {Id} not found", id);
                    TempData["Error"] = "Không tìm thấy phiên điểm danh";
                    return RedirectToAction("IndexSession");
                }

                var data = await response.Content.ReadFromJsonAsync<IndexAttendanceViewModel>();

                if (data == null)
                {
                    TempData["Error"] = "Dữ liệu phiên điểm danh không hợp lệ";
                    return RedirectToAction("IndexSession");
                }

                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance detail for ID: {Id}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết phiên điểm danh";
                return RedirectToAction("IndexSession");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detail([FromForm] CheckInDto dto)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return await Detail(dto.AttendanceId);
            }

            try
            {
                var response = await _client.PostAsJsonAsync("api/attendance/checkin", dto);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "✅ Điểm danh thành công";
                    _logger.LogInformation("Successfully checked in student {StudentId} for attendance {AttendanceId}",
                        dto.StudentId, dto.AttendanceId);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to check in: {Error}", error);
                    TempData["Error"] = "❌ Điểm danh thất bại";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in student");
                TempData["Error"] = "❌ Lỗi khi điểm danh";
            }

            return await Detail(dto.AttendanceId);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> History()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Users");
            }

            try
            {
                var response = await client.GetAsync("api/Attendance/history");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get attendance history. Status: {StatusCode}", response.StatusCode);
                    ViewBag.Error = "Không thể tải lịch sử điểm danh";
                    return View(new List<StudentAttendanceHistory>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<StudentAttendanceHistory>>(json)
                    ?? new List<StudentAttendanceHistory>();

                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance history");
                ViewBag.Error = "Đã xảy ra lỗi khi tải lịch sử điểm danh";
                return View(new List<StudentAttendanceHistory>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadSchedules();
                return View(new CreateAttendanceViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["ErrorMessage"] = "Không thể tải form tạo phiên điểm danh";
                return RedirectToAction("IndexSession");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAttendanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSchedules();
                return View(model);
            }

            try
            {
                // Validate thêm
                if (string.IsNullOrWhiteSpace(model.SessionCode))
                {
                    ModelState.AddModelError("SessionCode", "Mã phiên không được trống");
                    await LoadSchedules();
                    return View(model);
                }

                // Chuẩn hóa mã phiên
                model.SessionCode = model.SessionCode.Trim().ToUpper();

                // Set default time if not provided
                if (!model.Starttime.HasValue)
                    model.Starttime = DateTime.Now;

                if (!model.Endtime.HasValue)
                    model.Endtime = DateTime.Now.AddHours(2);

                var response = await _client.PostAsJsonAsync("api/Attendance", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Tạo phiên điểm danh thành công!";
                    _logger.LogInformation("Created attendance session with code: {SessionCode}", model.SessionCode);
                    return RedirectToAction(nameof(IndexSession));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to create attendance session: {Error}", error);

                    // Parse error message if possible
                    try
                    {
                        dynamic errorObj = JsonConvert.DeserializeObject(error);
                        ModelState.AddModelError("", errorObj.message?.ToString() ?? "Tạo phiên thất bại");
                    }
                    catch
                    {
                        ModelState.AddModelError("", "Tạo phiên điểm danh thất bại");
                    }

                    await LoadSchedules();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance session");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo phiên điểm danh");
                await LoadSchedules();
                return View(model);
            }
        }

        private async Task LoadSchedules()
        {
            try
            {
                var schedules = await _context.Schedules
                    .Include(s => s.Subject)
                    .Include(s => s.StudyShift)
                    .OrderBy(s => s.ClassName)
                    .ToListAsync();

                ViewBag.Schedules = schedules.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.ClassName} - {s.Subject?.SubjectName ?? "N/A"} ({s.StudyShift?.StudyShiftName ?? "N/A"})"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedules");
                ViewBag.Schedules = new List<SelectListItem>();
            }
        }

        private async Task LoadSelectItems()
        {
            try
            {
                var classList = await _context.Schedules
                    .OrderBy(s => s.ClassName)
                    .ToListAsync();
                ViewBag.ClassList = classList.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ClassName ?? "N/A"
                }).ToList();

                var roomlist = await _context.Rooms
                    .OrderBy(r => r.RoomCode)
                    .ToListAsync();
                ViewBag.RoomList = roomlist.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoomCode ?? "N/A"
                }).ToList();

                var weekday = await _context.DayOfWeeks
                    .OrderBy(d => d.Id)
                    .ToListAsync();
                ViewBag.DayList = weekday.Select(w => new SelectListItem
                {
                    Value = w.Id.ToString(),
                    Text = w.Weekdays.ToString()
                }).ToList();

                var studyShift = await _context.StudyShifts
                    .OrderBy(s => s.StudyShiftName)
                    .ToListAsync();
                ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.StudyShiftName ?? "N/A"
                }).ToList();

                var subject = await _context.Subjects
                    .OrderBy(s => s.SubjectName)
                    .ToListAsync();
                ViewBag.SubjectList = subject.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SubjectName ?? "N/A"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading select items");
                // Set empty lists to prevent null reference
                ViewBag.ClassList = new List<SelectListItem>();
                ViewBag.RoomList = new List<SelectListItem>();
                ViewBag.DayList = new List<SelectListItem>();
                ViewBag.ShiftList = new List<SelectListItem>();
                ViewBag.SubjectList = new List<SelectListItem>();
            }
        }
    }
}