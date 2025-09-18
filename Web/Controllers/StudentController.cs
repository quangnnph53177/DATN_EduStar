using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.Json;

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

            // 📌 Build query string
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(StudentCode)) queryParams.Add($"studentCode={StudentCode}");
            if (!string.IsNullOrWhiteSpace(fullName)) queryParams.Add($"fullName={fullName}");
            if (!string.IsNullOrWhiteSpace(username)) queryParams.Add($"username={username}");
            if (!string.IsNullOrWhiteSpace(email)) queryParams.Add($"email={email}");
            if (gender.HasValue) queryParams.Add($"gender={gender.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
            var url = $"api/students/student{queryString}";

            // 📡 Gửi yêu cầu
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
                    return RedirectToAction("Login");
                }

                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lấy danh sách người dùng.";
                return View(new List<ClassWithStudentsViewModel>());
            }

            var usersJson = await response.Content.ReadAsStringAsync();

            List<ClassWithStudentsViewModel> classViewModels;

            try
            {
                // 👑 Giảng viên (trả về dictionary: lớp -> sinh viên)
                var usersDict = JsonConvert.DeserializeObject<Dictionary<string, List<UserDTO>>>(usersJson);
                classViewModels = usersDict.Select((kv, index) => new ClassWithStudentsViewModel
                {
                    ClassId = index + 1,
                    ClassName = kv.Key,
                    StudentsInfor = kv.Value
                }).ToList();
            }
            catch
            {
                try
                {
                    // 👨‍🎓 Sinh viên hoặc Admin (trả về danh sách đơn lẻ)
                    var usersList = JsonConvert.DeserializeObject<List<UserDTO>>(usersJson);
                    classViewModels = new List<ClassWithStudentsViewModel>
                    {
                        new ClassWithStudentsViewModel
                        {
                            ClassId = 1,
                            ClassName = "Lớp của bạn",
                            StudentsInfor = usersList
                        }
                    };
                }
                catch
                {
                    TempData["ErrorMessage"] = "Lỗi xử lý dữ liệu người dùng.";
                    return View(new List<ClassWithStudentsViewModel>());
                }
            }

            // 📌 Phân trang từng lớp
            foreach (var cls in classViewModels)
            {
                int currentPage = (cls.ClassId == classId) ? page : 1;
                int total = cls.StudentsInfor.Count;

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
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            try
            {
                var response = await client.GetAsync("api/Students/profile");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập.";
                    return RedirectToAction("Login", "Users");
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin cá nhân.";
                    return View(new StudentViewModels());
                }

                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonConvert.DeserializeObject<StudentViewModels>(json);

                return View(profile);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(new StudentViewModels());
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditMyProfile()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            try
            {
                var response = await client.GetAsync("api/Students/profile");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin để chỉnh sửa.";
                    return RedirectToAction("MyProfile");
                }

                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonConvert.DeserializeObject<StudentViewModels>(json);

                return View(profile);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("MyProfile");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditMyProfile(StudentViewModels model, IFormFile avatarFile)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var content = new MultipartFormDataContent();

                // Thêm các thông tin cơ bản
                content.Add(new StringContent(model.id.ToString()), "id");
                content.Add(new StringContent(model.Email ?? ""), "Email");
                content.Add(new StringContent(model.PhoneNumber ?? ""), "PhoneNumber");
                content.Add(new StringContent(model.FullName ?? ""), "FullName");
                content.Add(new StringContent(model.Address ?? ""), "Address");

                if (model.Dob.HasValue)
                    content.Add(new StringContent(model.Dob.Value.ToString("yyyy-MM-dd")), "Dob");

                if (model.Gender.HasValue)
                    content.Add(new StringContent(model.Gender.Value.ToString()), "Gender");

                // Thêm file avatar nếu có
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var streamContent = new StreamContent(avatarFile.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(avatarFile.ContentType);
                    content.Add(streamContent, "imgFile", avatarFile.FileName);
                }

                var response = await client.PutAsync("api/Students/profile", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Cập nhật thất bại: {error}";
                    return View(model);
                }

                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction("MyProfile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(model);
            }
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
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            try
            {
                // Tạo multipart content để gửi cả data và file
                using var content = new MultipartFormDataContent();

                // Thêm các thông tin cơ bản
                content.Add(new StringContent(model.id.ToString()), "id");
                content.Add(new StringContent(model.UserName ?? ""), "UserName");
                content.Add(new StringContent(model.Email ?? ""), "Email");
                content.Add(new StringContent(model.PhoneNumber ?? ""), "PhoneNumber");
                content.Add(new StringContent(model.FullName ?? ""), "FullName");
                content.Add(new StringContent(model.Address ?? ""), "Address");

                if (model.Dob.HasValue)
                    content.Add(new StringContent(model.Dob.Value.ToString("yyyy-MM-dd")), "Dob");

                if (model.Gender.HasValue)
                    content.Add(new StringContent(model.Gender.Value.ToString()), "Gender");

                // Thêm file avatar nếu có
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var streamContent = new StreamContent(avatarFile.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(avatarFile.ContentType);
                    content.Add(streamContent, "imgFile", avatarFile.FileName);
                }

                var response = await client.PutAsync($"api/Students/Beast/{id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Cập nhật thất bại: {error}";
                    return View(model);
                }

                TempData["SuccessMessage"] = "Cập nhật sinh viên thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(model);
            }
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
        [HttpGet]
        public async Task<IActionResult> ChangeRole(string username)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            // Gọi API để lấy dữ liệu user cũ
            var response = await client.GetAsync($"api/User/searchuser?keyword={username}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var users =System.Text.Json.JsonSerializer.Deserialize<List<UserDTO>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var user = users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index", "Student");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index", "Student");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string username, int newRoleId)
        {
            try
            {
                var client = GetClientWithToken();

                var response = await client.PutAsync($"api/User/changerole/{username}?newRoleId={newRoleId}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Thay đổi vai trò thành công.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(errorContent) ? errorContent : "Thay đổi vai trò thất bại.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("Index", "Student");
            }
        }
        public async Task<IActionResult> CreateSV(RegisterViewModel model, IFormFile? imgFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = GetClientWithToken();
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(model.User.UserName ?? ""), "UserName");
            content.Add(new StringContent(model.User.PassWordHash ?? ""), "PassWordHash");
            content.Add(new StringContent(model.User.Email ?? ""), "Email");
            content.Add(new StringContent(model.User.PhoneNumber ?? ""), "PhoneNumber");
            content.Add(new StringContent(model.User.FullName ?? ""), "FullName");
            content.Add(new StringContent(model.User.Address ?? ""), "Address");
            content.Add(new StringContent(model.User.Statuss.ToString()), "Statuss");

            if (model.User.Dob.HasValue)
                content.Add(new StringContent(model.User.Dob.Value.ToString("yyyy-MM-dd")), "Dob");
            content.Add(new StringContent(model.User.Gender.HasValue ? model.User.Gender.Value.ToString() : ""), "Gender");

            //if (model.User.RoleIds != null && model.User.RoleIds.Any())
            //{
            //    foreach (var roleId in model.User.RoleIds)
            //    {
            //        content.Add(new StringContent(roleId.ToString()), "RoleIds");
            //    }
            //}

            if (imgFile != null && imgFile.Length > 0)
            {
                var streamContent = new StreamContent(imgFile.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(imgFile.ContentType);
                content.Add(streamContent, "imgFile", imgFile.FileName);
            }
            var response = await client.PostAsync("api/User/createSv", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo tài khoản thành công.";
                return RedirectToAction("Index", "Student");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(); // 🔥 Đọc lỗi chi tiết từ API
                TempData["ErrorMessage"] = $"Đăng ký không thành công: {errorContent}";
                return View(model);
            }
        }
    }
}
