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
                return RedirectToAction("Login", "Users");
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
                return RedirectToAction("Login", "Users");
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
                return RedirectToAction("Login", "Users");
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

            var response = await client.PutAsync(
                $"api/TeachingRegistration/admin/approve?RegistrationId={registrationId}&Approve={approve}",
                null); 

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
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }


}

