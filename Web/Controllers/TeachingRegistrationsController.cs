using API.Data;
using API.Models;
using API.ViewModel;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
            Console.WriteLine("Token từ Cookie: " + token);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var response = await client.GetAsync("api/TeachingRegistration/registrations");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải dữ liệu. Vui lòng thử lại sau.";
                return View(new List<TeachingRegistrationVMD>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var registrations = System.Text.Json.JsonSerializer.Deserialize<List<TeachingRegistrationVMD>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TeachingRegistrationVMD>();

            return View(registrations);
        }
        [HttpGet]
        public async Task<IActionResult> TeacherRegister()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Account");
            await LoadDropdownData();

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> TeacherRegister(TeachingRegistrationViewModel model, string dayGroup)
        {
            var client = GetClientWithToken(); // Hàm này trả về HttpClient có kèm JWT token
            if (client == null)
                return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
            {
                await LoadDropdownData();
                return View(model);
            }
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"api/TeachingRegistration/register?dayGroup={dayGroup}", content);

                var result = await response.Content.ReadAsStringAsync();

                // Nếu là BadRequest từ API (400)
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = result;
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi đăng ký: {result}";
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi gọi API: " + ex.Message;
            }
            Console.WriteLine($"SemesterId gửi lên: {model.SemesterId}");
            await LoadDropdownData();
            return RedirectToAction("Index");
        }
        private async Task LoadDropdownData()
        {
            var client = GetClientWithToken();
            if (client == null) return;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Chưa đăng nhập. không thể tìm thấy lớp của bạn.";
                RedirectToAction("Login", "Account"); 
                return; 
            }

            var classList = await _context.Classes.Where(c => c.UsersId == user.Id).ToListAsync();
            ViewBag.ClassList = classList.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameClass
            }).ToList();

            var dayList = await _context.DayOfWeeks.ToListAsync();
            ViewBag.DayList = dayList.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Weekdays.ToString()
            }).ToList();

            var shiftList = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = shiftList.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.StudyShiftName} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"
            }).ToList();

            var semesterList = await _context.Semesters.Where(x=>x.IsActive==true).ToListAsync();
            ViewBag.SemesterList = semesterList.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Name} ({s.StartDate:dd/MM/yyyy} - {s.EndDate:dd/MM/yyyy})"
            }).ToList();
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmRegistration(int registrationId)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }
            var response = await client.PutAsync($"api/TeachingRegistration/confirm/{registrationId}", null);
            var result = await response.Content.ReadAsStringAsync();
            TempData["Message"] = result;
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xác nhận đăng ký thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi xác nhận đăng ký. Vui lòng thử lại sau.";
            }
            return RedirectToAction("Index");
        }
    }

}
