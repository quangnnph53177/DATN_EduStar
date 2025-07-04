using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly HttpClient _client;
        public AttendanceController(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
        }
        public async Task<IActionResult> IndexSession()
        {
            var response = await _client.GetStringAsync("api/attendance");

            var data = JsonConvert.DeserializeObject<List<IndexAttendanceViewModel>>(response);

            return View(data);
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
        [HttpGet]
        public async Task<IActionResult> History()
        {
            // Lấy StudentId từ session hoặc JWT token
            var studentId = new Guid(User.FindFirst("Id").Value); // Giả sử bạn lưu Id sinh viên trong token

            var response = await _client.GetAsync($"api/attendance/history/{studentId}");
            if (!response.IsSuccessStatusCode) return View(new List<StudentAttendanceHistory>());  

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<StudentAttendanceHistory>>(json);

            return View(data);
        }
    }
}
