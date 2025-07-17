using API.Data;
using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class SubjectWController : Controller
    {
        private readonly HttpClient _Client;
        private readonly AduDbcontext _context; 
        public SubjectWController(HttpClient client, AduDbcontext aduDbcontext)
        {
            _Client = client;
            _Client.BaseAddress = new Uri("https://localhost:7298/");
            _context = aduDbcontext;
        }
        public async Task<IActionResult> Index(string? subjectName, int? numberofCredit, string? subcode, bool? status,int? semesterId)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(subjectName)) queryParams.Add($"subjectName={subjectName}");
            if (!string.IsNullOrWhiteSpace(subcode)) queryParams.Add($"subjectCode={subcode}");
            if (numberofCredit.HasValue) queryParams.Add($"numberofCredit={numberofCredit.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");
            if (semesterId.HasValue) queryParams.Add($"semesterId={semesterId.Value}");

            string query = queryParams.Count > 0 ? "api/subject?" + string.Join("&", queryParams) : "api/subject";
            var response = await _Client.GetAsync(query);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không tìm thấy môn học hoặc có lỗi xảy ra.";
                return View(new List<SubjectViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<SubjectViewModel>>(json);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var response = await _Client.GetStringAsync($"api/Subject/{id}");
            var result = JsonConvert.DeserializeObject<SubjectViewModel>(response);
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> Lock(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/Subject/{id}/Lock");
            var response = await _Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Message"] = "❌ Không thể cập nhật trạng thái sinh viên.";
                return RedirectToAction("Index");
            }

            TempData["Message"] = "✅ Cập nhật trạng thái thành công.";
            return RedirectToAction();
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var semesters = await _context.Semesters.ToListAsync();
            ViewBag.SemesterList = semesters.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(SubjectViewModel model)
        {
            var response = await _Client.PostAsJsonAsync("api/subject", model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            // Đọc nội dung lỗi trả về từ API
            var errorContent = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, $"Lỗi khi tạo môn học: {errorContent}");
            return View(model);

        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _Client.GetStringAsync($"api/Subject/{id}");
            var result = JsonConvert.DeserializeObject<SubjectViewModel>(response);
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, SubjectViewModel model)
        {
            var response = await _Client.PutAsJsonAsync($"api/subject/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Cập nhật thất bại: {response.StatusCode} - {error}");
                return View(model);
            }
            return RedirectToAction("Index");
        }
   
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _Client.DeleteAsync($"api/subject/{id}" );
            var json =await response.Content.ReadAsStringAsync();
            return RedirectToAction(nameof(Index));  
        }
    }
}
