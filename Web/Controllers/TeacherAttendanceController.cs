using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Web.Controllers
{
    public class TeacherAttendanceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TeacherAttendanceController(IHttpClientFactory httpClientFactory)
        {
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

        // GET: Lấy danh sách lớp giảng viên
        public async Task<IActionResult> MyClasses()
        {
            var client = GetClientWithToken();
            if (client == null)
                return RedirectToAction("Login", "Users");

            var response = await client.GetAsync("api/TeacherAttendance/my-classes");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return View(new List<TeacherClassViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<TeacherClassViewModel>>(json);
            return View(data);
        }

        // POST: Tạo phiên điểm danh
        [HttpPost]
        public async Task<IActionResult> CreateSession(CreateAttendanceViewModel model)
        {
            var client = GetClientWithToken();
            if (client == null)
                return RedirectToAction("Login", "Users");

            var jsonContent = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/TeacherAttendance/create-session", content);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("MyClasses");
            }

            TempData["Success"] = "Tạo phiên điểm danh thành công";
            return RedirectToAction("MyClasses");
        }

        // GET: Xem chi tiết 1 phiên điểm danh
        public async Task<IActionResult> SessionDetail(int id)
        {
            var client = GetClientWithToken();
            if (client == null)
                return RedirectToAction("Login", "Users");

            var response = await client.GetAsync($"api/TeacherAttendance/session/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("MyClasses");
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<IndexAttendanceViewModel>(json);
            return View(data);
        }
    }
}
