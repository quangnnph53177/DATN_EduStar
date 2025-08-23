using API.Data;
using API.ViewModel;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static API.Models.TeachingRegistration;

namespace Web.Controllers
{
    public class TeachingRegistrationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AduDbcontext _context;
        public TeachingRegistrationsController(IHttpClientFactory httpClientFactory, AduDbcontext aduDbcontext)
        {
            _httpClientFactory = httpClientFactory;
            _context = aduDbcontext;
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

        // Lịch có thể đăng ký (cho giảng viên)
        public async Task<IActionResult> AllSchedules()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var response = await client.GetAsync("api/TeachingRegistration/allschedules");
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<SchedulesViewModel>>>(json);
            var result = apiResponse?.Data ?? new List<SchedulesViewModel>();
            return View(result);
        }

        // Đăng ký lịch dạy
        [HttpPost]
        public async Task<IActionResult> RegisterSchedule(int schedulesID)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
            }

            var response = await client.PostAsync($"api/TeachingRegistration/register?schedulesID={schedulesID}", null);
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<string>>(json);

            if (response.IsSuccessStatusCode && apiResponse?.Success == true)
            {
                return Json(new { success = true, message = apiResponse.Message ?? "Đăng ký thành công!" });
            }
            else
            {
                return Json(new { success = false, message = apiResponse?.Message ?? "Đăng ký thất bại" });
            }
        }

        // Lịch giảng viên đã đăng ký
        public async Task<IActionResult> TeacherRegistrations()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await client.GetAsync("api/TeachingRegistration/my-registrations");
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<TeachingRegistrationVM>>>(json);
            var result = apiResponse?.Data ?? new List<TeachingRegistrationVM>();

            return View(result);
        }

        // Tất cả đăng ký (cho admin)
        public async Task<IActionResult> AllRegistrations(string? status = null)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var url = "api/TeachingRegistration/admin/all-registrations";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"?status={Uri.EscapeDataString(status)}";
            }

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<AdminResgistration>>>(json);
            var result = apiResponse?.Data ?? new List<AdminResgistration>();

            ViewBag.CurrentStatus = status;
            return View(result);
        }

        // Duyệt đăng ký
        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int registrationId, ApprovedStatus approve)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
            }

            var payload = new
            {
                RegistrationId = registrationId,
                Approve = approve
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("api/TeachingRegistration/admin/approve", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<string>>(responseJson);

            if (response.IsSuccessStatusCode && apiResponse?.Success == true)
            {
                return Json(new { success = true, message = apiResponse.Message ?? "Xử lý thành công!" });
            }
            else
            {
                return Json(new { success = false, message = apiResponse?.Message ?? "Xử lý thất bại" });
            }
        }
    }



    //[HttpGet]
    //public async Task<IActionResult> Index()
    //{
    //    var client = GetClientWithToken();
    //    if (client == null)
    //    {
    //        TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    //        return RedirectToAction("Login", "Account");
    //    }

    //    var response = await client.GetAsync("api/TeachingRegistration/registrations");

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        TempData["ErrorMessage"] = "Lỗi khi tải dữ liệu. Vui lòng thử lại sau.";
    //        return View(new List<TeachingRegistrationVMD>());
    //    }

    //    var json = await response.Content.ReadAsStringAsync();
    //    var registrations = System.Text.Json.JsonSerializer.Deserialize<List<TeachingRegistrationVMD>>(json, new JsonSerializerOptions
    //    {
    //        PropertyNameCaseInsensitive = true
    //    }) ?? new List<TeachingRegistrationVMD>();

    //    return View(registrations);
    //}
    //[HttpGet]
    //public async Task<IActionResult> TeacherRegister()
    //{
    //    var client = GetClientWithToken();
    //    if (client == null) return RedirectToAction("Login", "Account");
    //    await LoadDropdownData();

    //    return View();
    //}
    //[HttpPost]
    //public async Task<IActionResult> TeacherRegister(TeachingRegistrationViewModel model, string dayGroup)
    //{
    //    var client = GetClientWithToken(); // Hàm này trả về HttpClient có kèm JWT token
    //    if (client == null)
    //        return RedirectToAction("Login", "Account");
    //    if (!ModelState.IsValid)
    //    {
    //        await LoadDropdownData();
    //        return View(model);
    //    }
    //    try
    //    {
    //        var json = JsonConvert.SerializeObject(model);
    //        var content = new StringContent(json, Encoding.UTF8, "application/json");
    //        var response = await client.PostAsync($"api/TeachingRegistration/register?dayGroup={dayGroup}", content);

    //        var result = await response.Content.ReadAsStringAsync();

    //        // Nếu là BadRequest từ API (400)
    //        if (response.IsSuccessStatusCode)
    //        {
    //            TempData["SuccessMessage"] = result;
    //        }
    //        else
    //        {
    //            TempData["ErrorMessage"] = $"Lỗi đăng ký: {result}";
    //        }

    //    }
    //    catch (Exception ex)
    //    {
    //        TempData["ErrorMessage"] = "Lỗi hệ thống khi gọi API: " + ex.Message;
    //    }
    //    Console.WriteLine($"SemesterId gửi lên: {model.SemesterId}");
    //    await LoadDropdownData();
    //    return RedirectToAction("Index");
    //}
    //private async Task LoadDropdownData()
    //{
    //    var client = GetClientWithToken();
    //    if (client == null) return;

    //    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
    //    if (user == null)
    //    {
    //        TempData["ErrorMessage"] = "Chưa đăng nhập. không thể tìm thấy lớp của bạn.";
    //        RedirectToAction("Login", "Account");
    //        return;
    //    }

    //    var classList = await _context.Schedules.Where(c => c.UsersId == user.Id).ToListAsync();
    //    ViewBag.ClassList = classList.Select(c => new SelectListItem
    //    {
    //        Value = c.Id.ToString(),
    //        Text = c.ClassName,
    //    }).ToList();

    //    var dayList = await _context.DayOfWeeks.ToListAsync();
    //    ViewBag.DayList = dayList.Select(d => new SelectListItem
    //    {
    //        Value = d.Id.ToString(),
    //        Text = d.Weekdays.ToString()
    //    }).ToList();

    //    var shiftList = await _context.StudyShifts.ToListAsync();
    //    ViewBag.ShiftList = shiftList.Select(s => new SelectListItem
    //    {
    //        Value = s.Id.ToString(),
    //        Text = $"{s.StudyShiftName} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"
    //    }).ToList();

    //    var semesterList = await _context.Semesters.Where(x => x.IsActive == true).ToListAsync();
    //    ViewBag.SemesterList = semesterList.Select(s => new SelectListItem
    //    {
    //        Value = s.Id.ToString(),
    //        Text = $"{s.Name} ({s.StartDate:dd/MM/yyyy} - {s.EndDate:dd/MM/yyyy})"
    //    }).ToList();
    //}
    //[HttpPost]
    //public async Task<IActionResult> ConfirmRegistration(int registrationId)
    //{
    //    var client = GetClientWithToken();
    //    if (client == null)
    //    {
    //        TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    //        return RedirectToAction("Login", "Account");
    //    }
    //    var response = await client.PutAsync($"api/TeachingRegistration/confirm/{registrationId}", null);
    //    var result = await response.Content.ReadAsStringAsync();
    //    TempData["Message"] = result;
    //    if (response.IsSuccessStatusCode)
    //    {
    //        TempData["SuccessMessage"] = "Xác nhận đăng ký thành công!";
    //    }
    //    else
    //    {
    //        TempData["ErrorMessage"] = "Lỗi khi xác nhận đăng ký. Vui lòng thử lại sau.";
    //    }
    //    return RedirectToAction("Index");
    //}
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }


}

