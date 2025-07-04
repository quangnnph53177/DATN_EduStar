using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Web.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ComplaintController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Hàm helper lấy HttpClient có set Authorization header từ cookie token
        private HttpClient? GetClientWithToken()
        {
            var client = _httpClientFactory.CreateClient("EdustarAPI");
            if (!Request.Cookies.TryGetValue("JWToken", out var token) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return null;
            }
            Console.WriteLine("Token từ Cookie: " + token);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Account");
            try
            {
                var response = await client.GetAsync("api/complaints/complaints");

                if (response.IsSuccessStatusCode)
                {
                    var complaints = await response.Content.ReadFromJsonAsync<IEnumerable<ComplaintDTO>>();
                    return View(complaints);
                }
                else
                {
                    // Đọc nội dung lỗi từ server (nếu có)
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var statusCode = (int)response.StatusCode;

                    TempData["ErrorMessage"] = $"Lỗi API: {(int)response.StatusCode} - {response.ReasonPhrase}";
                    TempData["ApiErrorDetail"] = errorContent;

                    return View(new List<ComplaintDTO>());
                }
            }
            catch (HttpRequestException httpEx)
            {
                TempData["ErrorMessage"] = "Không thể kết nối tới API. Kiểm tra mạng hoặc server.";
                TempData["ApiErrorDetail"] = httpEx.Message;
                return View(new List<ComplaintDTO>());
            }
            catch (TaskCanceledException)
            {
                TempData["ErrorMessage"] = "Yêu cầu tới API bị timeout.";
                return View(new List<ComplaintDTO>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi không xác định.";
                TempData["ApiErrorDetail"] = ex.Message;
                return View(new List<ComplaintDTO>());
            }
        }
        [HttpGet]
        public IActionResult ClassChangeComplaint()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ClassChangeComplaint(ClassChangeComplaintDTO dto)
        {
            if (dto == null || dto.CurrentClassId == 0 || dto.RequestedClassId == 0 || string.IsNullOrWhiteSpace(dto.Reason))
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return View(dto);
            }
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Account");
            try
            {
                var response = await client.PostAsJsonAsync("api/Complaints/class-change-complaint", dto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đăng ký khiếu nại thành công.";
                    return RedirectToAction("Index");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi đăng ký khiếu nại: {error}";
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(dto);
            }
        }
        [HttpGet]
        public IActionResult ProcessClassComplaint(int id)
        {
            var model = new ProcessComplaintDTO
            {
                ComplaintId = id // 👈 Gán đúng ID vào model
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ProcessClassComplaint(ProcessComplaintDTO dto)
        {
            Console.WriteLine($"Gửi xử lý complaintId = {dto.ComplaintId}");
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("ComplaintDetail", new { id = dto.ComplaintId });
            }
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Account");
            try
            {
                var response = await client.PutAsJsonAsync($"https://localhost:7298/api/Complaints/process/{dto.ComplaintId}", dto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xử lý khiếu nại thành công.";
                    return RedirectToAction("Index"); 
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi xử lý: {error}";
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xử lý khiếu nại: {ex.Message}";
            }
            return View(dto);
        }
    }
}
