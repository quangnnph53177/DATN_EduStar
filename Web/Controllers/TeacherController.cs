using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Web.Controllers
{
    public class TeacherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public TeacherController(IHttpClientFactory httpClientFactory)
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
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {

            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Users");
            }

            var response = await client.GetAsync("api/User/teacher");

            if (response.IsSuccessStatusCode)
            {
                var usersJson = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDTO>>(usersJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<UserDTO>();
                var totalCount = users.Count;
                var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var result = new PagedResult<UserDTO>
                {
                    Items = pagedUsers,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageIndex = page
                };

                return View(result);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Login");
            }
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lấy danh sách người dùng.";
            return View(new PagedResult<UserDTO>());
        }
        [HttpGet]
        public async Task<IActionResult> SearchUS(string? keyword, int page = 1, int pageSize = 10)
        {
            var client = GetClientWithToken();

            string url = "api/User/searchuser";
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                url += $"?keyword={Uri.EscapeDataString(keyword)}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var users = JsonSerializer.Deserialize<List<UserDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                // 🔍 Chỉ giữ lại các user có RoleIds chứa 1 (admin)
                var filteredUsers = users
                    .Where(u => u.RoleIds != null && u.RoleIds.Contains(2))
                    .ToList();

                var totalCount = filteredUsers.Count;
                var pagedUsers = filteredUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var result = new PagedResult<UserDTO>
                {
                    Items = pagedUsers,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageIndex = page
                };
                ViewBag.Keyword = keyword; // để giữ lại từ khoá khi hiển thị view
                return View("Index", result); // dùng lại view Index
            }
            else
            {
                // Hiển thị thông báo lỗi chi tiết từ API (nếu có)
                TempData["ErrorMessage"] = !string.IsNullOrEmpty(content) ? content : "Không thể lấy danh sách người dùng.";
                return View("Index", new PagedResult<UserDTO>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> IndexUser(string? username)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            if (string.IsNullOrEmpty(username))
            {
                username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    TempData["ErrorMessage"] = "Không thể xác định người dùng";
                    return RedirectToAction("Index", "Teacher");
                }
            }
            // Gọi API để lấy dữ liệu user cũ
            var response = await client.GetAsync($"https://localhost:7298/api/User/searchuser?keyword={username}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDTO>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var user = users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index", "Teacher");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index", "Teacher");
            }
        }
        [HttpGet]
        public async Task<IActionResult> UpdateUser(string username)
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            // Gọi API để lấy dữ liệu user cũ
            var response = await client.GetAsync($"https://localhost:7298/api/User/searchuser?keyword={username}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDTO>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var user = users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index", "Teacher");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index", "Teacher");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateUser(string username, UserDTO userDto, IFormFile imgFile)
        {
            try
            {
                var client = GetClientWithToken();

                // Serialize object
                var formData = new MultipartFormDataContent
               {
                   { new StringContent(userDto.UserName ?? ""), "UserName" },
                   { new StringContent(userDto.FullName ?? ""), "FullName" },
                   { new StringContent(userDto.UserCode ?? ""), "UserCode" },
                   { new StringContent(userDto.Email ?? ""), "Email" },
                   { new StringContent(userDto.PhoneNumber ?? ""), "PhoneNumber" },
                   { new StringContent(string.Join(",", userDto.RoleIds ?? new List<int>())), "RoleId" },
                   { new StringContent(userDto.Address ?? ""), "Address" },
                   { new StringContent(userDto.Gender.HasValue ? userDto.Gender.Value.ToString() : ""), "Gender" }
               };

                // Fix for CS1503: Convert DateTime? to string using ToString with a format
                if (userDto.Dob.HasValue)
                {
                    formData.Add(new StringContent(userDto.Dob.Value.ToString("yyyy-MM-dd")), "Dob");
                }

                if (imgFile != null && imgFile.Length > 0)
                {
                    var streamContent = new StreamContent(imgFile.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(imgFile.ContentType);
                    formData.Add(streamContent, "imgFile", imgFile.FileName);
                }

                var response = await client.PutAsync($"https://localhost:7298/api/User/updateuser/{userDto.UserName}", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công.";
                    return RedirectToAction("IndexUser", new { username = userDto.UserName });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = !string.IsNullOrEmpty(errorContent)
                    ? errorContent
                    : "Cập nhật thất bại.";
                return RedirectToAction("IndexUser", new { username = userDto.UserName });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("IndexUser", new { username = userDto.UserName });
            }
        }
        [HttpPost]
        public async Task<IActionResult> LockUser(string username)
        {
            try
            {
                var client = GetClientWithToken();
                // Gọi API với phương thức POST (đã đổi trên API)
                var response = await client.PostAsync($"https://localhost:7298/api/User/lock/{username}", null);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật trạng thái người dùng thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(content) ? content : "Thao tác thất bại.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction("Index","Teacher");
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
            var response = await client.GetAsync($"https://localhost:7298/api/User/searchuser?keyword={username}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDTO>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var user = users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index", "Teacher");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index", "Teacher");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string username, int newRoleId)
        {
            try
            {
                var client = GetClientWithToken();

                var response = await client.PutAsync($"https://localhost:7298/api/User/changerole/{username}?newRoleId={newRoleId}", null);

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
                return RedirectToAction("Index", "Teacher");
            }
        }
    }
}
