using API.Data;
using API.ViewModel;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Web.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AduDbcontext _context;
        public ComplaintController(IHttpClientFactory httpClientFactory, AduDbcontext aduDbcontext)
        {
            _httpClientFactory = httpClientFactory;
            _context = aduDbcontext;
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
            if (client == null) return RedirectToAction("Login", "Users");
            try
            {
                var response = await client.GetAsync("api/complaints/complaints");
                var complaintList = await response.Content.ReadFromJsonAsync<List<ComplaintDTO>>();
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = errorContent;
                }
                return View(complaintList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }
            return View(new List<ComplaintDTO>());
        }
        [HttpGet]
        public async Task<IActionResult> ClassChangeComplaint()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Users");

            try
            {
                await LoadClassSelectLists();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }
            return View(new ClassChangeComplaintDTO());
        }
        [HttpPost]
        public async Task<IActionResult> ClassChangeComplaint(ClassChangeComplaintDTO dto)
        {
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
                   
                }
                await LoadClassSelectLists();
                return View(dto);

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
                ComplaintId = id 
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ProcessClassComplaint(ProcessComplaintDTO dto)
        {
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
        [HttpGet]
        public async Task<IActionResult> ComplaintDetail(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Users");
            try
            {
                var response = await client.GetAsync($"api/Complaints/chiTietKhieuNai/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var complaintDetails = await response.Content.ReadFromJsonAsync<List<ComplaintDTO>>();
                    return View(complaintDetails);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = errorContent;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }
            return View(new List<ComplaintDTO>());
        }
        private async Task LoadClassSelectLists()
        {
            var client = GetClientWithToken();

            // Lấy lớp hiện tại của học sinh
            var classlstresponse = await client.GetAsync("api/Complaints/GetClassesOfStudent");
            if (classlstresponse.IsSuccessStatusCode)
            {
                var classlst = await classlstresponse.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<ClassViewModel>>(classlst, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                ViewBag.ClassList = result.Select(c => new SelectListItem
                {
                    Value = c.schedulesId.ToString(),
                    Text = $"{c.ClassName} - {c.SubjectName}"
                }).ToList();
            }

            // Lớp muốn chuyển đến (toàn bộ lớp)
            var getclass = await _context.Schedules.Include(c => c.Subject).ToListAsync();
            ViewBag.RequestedClassList = getclass.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.ClassName} - {c.Subject.SubjectName}"
            }).ToList();
        }
    }
}
