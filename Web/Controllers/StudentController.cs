using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly HttpClient _client;
        public StudentController(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
        }
        public async Task<IActionResult> Index(string? StudentCode, string? fullName, string? username, string? email, bool? gender)
        {             
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(StudentCode)) queryParams.Add($"studentCode={StudentCode}");
            if (!string.IsNullOrWhiteSpace(fullName)) queryParams.Add($"fullName={fullName}");
            if (!string.IsNullOrWhiteSpace(username)) queryParams.Add($"username={username}");
            if (!string.IsNullOrWhiteSpace(email)) queryParams.Add($"email={email}");
            if (gender.HasValue) queryParams.Add($"gender={gender.Value}");

            string query = queryParams.Count > 0 ? "api/students?" + string.Join("&", queryParams) : "api/students";
            var response = await _client.GetAsync(query);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không tìm thấy sinh viên hoặc có lỗi xảy ra.";
                return View(new List<StudentViewModels>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<StudentViewModels>>(json);
            return View(result);
        }
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _client.GetStringAsync($"api/students/{id}");
            var result = JsonConvert.DeserializeObject<StudentViewModels>(response);
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> Lock(Guid id)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/students/{id}/toggle-active");
            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Message"] = "❌ Không thể cập nhật trạng thái sinh viên.";
                return RedirectToAction("Index"); 
            }

            TempData["Message"] = "✅ Cập nhật trạng thái thành công.";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Export()
        {
            var response = await _client.GetAsync("api/students/excel");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Message"] = "❌ Không xuất được file.";
                return RedirectToAction("Index");
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            var fileName = $"DanhSach_SinhVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }



        [HttpGet]
        public async Task<IActionResult> EditBoss(Guid id)
        {
            var response = await _client.GetStringAsync($"api/Students/{id}");
            var result = JsonConvert.DeserializeObject<StudentViewModels>(response);
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> EditBoss(Guid id,StudentViewModels model)
        {
            var response = await _client.PutAsJsonAsync($"api/Students/boss/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Cập nhật thất bại: {response.StatusCode} - {error}");
                return View(model);
            }
            return RedirectToAction("Index");
        }
   
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _client.DeleteAsync($"api/students/{id}");

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index"); // về trang danh sách sinh viên
            }

            return StatusCode((int)response.StatusCode, "Lỗi khi xóa sinh viên");
        }

        public async Task<IActionResult> Auditlog()
        {
            var response = await _client.GetStringAsync("api/Students/auditlog");
            var result = JsonConvert.DeserializeObject<List<AuditLogViewModel>>(response);
            return View(result);
        }
       
    }
}
