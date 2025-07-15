using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly HttpClient _client;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        public StudentController(HttpClient client, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
            _env = env;
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
        public async Task<IActionResult> IndexST(string? StudentCode, string? fullName, string? username, string? email, bool? gender, bool? status, int classId = 0, int page = 1, int pageSize = 10)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(StudentCode)) queryParams.Add($"studentCode={StudentCode}");
            if (!string.IsNullOrWhiteSpace(fullName)) queryParams.Add($"fullName={fullName}");
            if (!string.IsNullOrWhiteSpace(username)) queryParams.Add($"username={username}");
            if (!string.IsNullOrWhiteSpace(email)) queryParams.Add($"email={email}");
            if (gender.HasValue) queryParams.Add($"gender={gender.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
            var url = $"api/students/student{queryString}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
                    return RedirectToAction("Login");
                }

                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lấy danh sách người dùng.";
                return View(new List<UserDTO>());
            }

            var usersJson = await response.Content.ReadAsStringAsync();

            // Nếu là giảng viên thì deserialize theo dictionary
            var usersDict = JsonConvert.DeserializeObject<Dictionary<string, List<UserDTO>>>(usersJson);
            var classViewModels = usersDict.Select((kv, index) => new ClassWithStudentsViewModel
            {
                ClassId = index + 1,
                ClassName = kv.Key,
                StudentsInfor = kv.Value
            }).ToList();

            foreach (var cls in classViewModels)
            {
                int currentPage = (cls.ClassId == classId) ? page : 1;
                var total = cls.StudentsInfor.Count;
                var paged = cls.StudentsInfor
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                cls.StudentsInfor = paged;
                ViewData[$"TotalPages_{cls.ClassId}"] = (int)Math.Ceiling((double)total / pageSize);
                ViewData[$"CurrentPage_{cls.ClassId}"] = currentPage;
                ViewData[$"PageSize_{cls.ClassId}"] = pageSize;
            }

            return View(classViewModels);
        }

        public async Task<IActionResult> Index(string? StudentCode, string? fullName, string? username, string? email, bool? gender, bool? status)
        {             
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(StudentCode)) queryParams.Add($"studentCode={StudentCode}");
            if (!string.IsNullOrWhiteSpace(fullName)) queryParams.Add($"fullName={fullName}");
            if (!string.IsNullOrWhiteSpace(username)) queryParams.Add($"username={username}");
            if (!string.IsNullOrWhiteSpace(email)) queryParams.Add($"email={email}");
            if (gender.HasValue) queryParams.Add($"gender={gender.Value}");
            if(status.HasValue) queryParams.Add($"status={status.Value}" );

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
        public async Task<IActionResult> EditBoss(Guid id, StudentViewModels model, IFormFile avatarFile)
        {
            var oldresponse = await _client.GetStringAsync($"api/students/{id}");
            var oldata = JsonConvert.DeserializeObject<StudentViewModels>(oldresponse);
            if (avatarFile != null && avatarFile.Length > 0)
            {
                
                var fileName = Path.GetFileNameWithoutExtension(avatarFile.FileName);
                var extension = Path.GetExtension(avatarFile.FileName);
                var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

               
                var filePath = Path.Combine(uploadsFolder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
                model.Avatar = $"/uploads/{newFileName}";
            }
            else
            {
                model.Avatar = oldata.Avatar;
            }

            var response = await _client.PutAsJsonAsync($"api/Students/boss/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Cập nhật thất bại: {response.StatusCode} - {error}");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditBeast(Guid id)
        {
            var response = await _client.GetStringAsync($"api/Students/{id}");
            var result = JsonConvert.DeserializeObject<StudentViewModels>(response);
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> EditBeast(Guid id, StudentViewModels model, IFormFile avatarFile)
        {
            var oldresponse = await _client.GetStringAsync($"api/students/{id}");
            var oldata = JsonConvert.DeserializeObject<StudentViewModels>(oldresponse);
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Tạo tên file duy nhất
                var fileName = Path.GetFileNameWithoutExtension(avatarFile.FileName);
                var extension = Path.GetExtension(avatarFile.FileName);
                var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                // Đường dẫn thư mục wwwroot/uploads
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Lưu file vào wwwroot/uploads
                var filePath = Path.Combine(uploadsFolder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Gán đường dẫn tương đối (hoặc URL nếu muốn)
                model.Avatar = $"/uploads/{newFileName}";
            }
            else
            {
                model.Avatar = oldata.Avatar;
            }
            var response = await _client.PutAsJsonAsync($"api/Students/Beast/{id}", model);
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
            var response = await _client.GetAsync("api/Students/auditlog");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API Error: " + error);
                return StatusCode((int)response.StatusCode, "Không thể lấy audit log");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<AuditLogViewModel>>(json);
            return View(result);
        }
        // thêm cái gửi cho tất cả sinh viên về thông tin lớp học
       // gửi thông báo chơi chơi
        [HttpPost]
        public async Task<IActionResult> SendEmail(int classId, string subject)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("classId", classId.ToString()),
                new KeyValuePair<string, string>("subject", subject)
             });

            var response = await _client.PostAsync("api/Students/SendEmail", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Gửi email thành công!";
            }
            else
            {
                TempData["Error"] = $"Thất bại: {response.StatusCode}";
            }

            return RedirectToAction("Index"); // hoặc trang bạn muốn quay lại
        }

    }
}
