using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace Web.Controllers
{
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public UsersController(IHttpClientFactory httpClientFactory)
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
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }
            try
            {
                var client = _httpClientFactory.CreateClient("EdustarAPI");
                var content = new StringContent(JsonSerializer.Serialize(loginDto), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://localhost:7298/api/User/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    using var doc = JsonDocument.Parse(responseContent);

                    var token = doc.RootElement.GetProperty("token").GetString();
                    var roleId = doc.RootElement.GetProperty("roleId").GetInt32();
                    var userName = doc.RootElement.GetProperty("userName").GetString();

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userName ?? ""),
                        new(ClaimTypes.Role, roleId.ToString()),
                        new("JWToken", token ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    Response.Cookies.Append("JWToken", token ?? "", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    });

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    return RedirectToAction("Index", "Users");
                }
                TempData["ErrorMessage"] = "Đăng nhập không thành công. Vui lòng kiểm tra lại thông tin.";
                return View(loginDto);
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối đến server API: {ex.Message}";
                return View(loginDto);
            }
            catch (TaskCanceledException)
            {
                TempData["ErrorMessage"] = "Yêu cầu đăng nhập bị timeout. Vui lòng thử lại.";
                return View(loginDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(loginDto);
            }


        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("JWToken");
            return RedirectToAction("Login", "Users");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            try
            {
                var response = await client.GetAsync("https://localhost:7298/api/User/user");

                if (response.IsSuccessStatusCode)
                {
                    var usersJson = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserDTO>>(usersJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<UserDTO>();
                    return View(users);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập danh sách người dùng.";
                    return RedirectToAction("AccessDenied");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
                    return RedirectToAction("Login");
                }

                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lấy danh sách người dùng.";
                return View(new List<UserDTO>());
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối đến server API: {ex.Message}";
                return RedirectToAction("Login");
            }
            catch (TaskCanceledException)
            {
                TempData["ErrorMessage"] = "Yêu cầu bị timeout khi kết nối API.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View(new List<UserDTO>());
            }
        }
        [HttpGet]
        public IActionResult Register()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Admin" },
                new SelectListItem { Value = "2", Text = "Teacher" },
                new SelectListItem { Value = "3", Text = "Student" }
            };

            ViewBag.RoleList = roles;

            return View(new UserDTO());
        }
        [HttpPost]
        public async Task<IActionResult> Register(UserDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = GetClientWithToken();

            var response = await client.PostAsJsonAsync("https://localhost:7298/api/User/register", model);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index", "Users");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                return RedirectToAction("AccessDenied");
            ModelState.AddModelError("", "Đăng ký không thành công.");
            return View(model);
        }
        [HttpGet]
        public IActionResult Upload()
        {
            var client = GetClientWithToken();
            if (client == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn tệp để tải lên.");
                return View();
            }

            var client = GetClientWithToken();

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            content.Add(new StreamContent(fileStream), "file", file.FileName);

            var response = await client.PostAsync("https://localhost:7298/api/User/upload", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Users");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                return RedirectToAction("AccessDenied");
            ModelState.AddModelError("", "Tải lên không thành công.");
            return View();
        }
        public IActionResult Confirm()
        {
            return View(); // Trả về form nhập token
        }
        [HttpPost]
        public async Task<IActionResult> Confirm(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập token.";
                return RedirectToAction("Confirm");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("EdustarAPI");
                var response = await client.GetAsync($"https://localhost:7298/api/User/confirm?token={token}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xác nhận email thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Xác nhận thất bại. Token không hợp lệ hoặc đã hết hạn.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction("Confirm");
        }
        [HttpGet]
        public async Task<IActionResult> SearchUS(string? keyword)
        {
            try
            {
                var client = GetClientWithToken();
                if (client == null)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login");
                }

                string url = $"https://localhost:7298/api/User/searchuser";
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    url += $"?keyword={Uri.EscapeDataString(keyword)}";
                }

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var users = JsonSerializer.Deserialize<List<UserDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View("Index", users);
                }
                else
                {
                    // Hiển thị thông báo lỗi chi tiết từ API (nếu có)
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(content) ? content : "Không thể lấy danh sách người dùng.";
                    return View("Index", new List<UserDTO>());
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("SearchUS");
            }
        }
        [HttpGet]
        public async Task<IActionResult> IndexUser(string username)
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
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index");
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
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateUser(string username, UserDTO userDto)
        {
            try
            {
                var client = GetClientWithToken();

                // Serialize object
                var content = new StringContent(JsonSerializer.Serialize(userDto), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"https://localhost:7298/api/User/updateuser/{userDto.UserName}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công.";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(errorContent) ? errorContent : "Cập nhật thất bại.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> LockUser(string username)
        {
            try
            {
                var client = GetClientWithToken();
                if (client == null)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
                // Gọi API với phương thức POST (đã đổi trên API)
                var response = await client.PostAsync($"https://localhost:7298/api/User/lock/{username}", null);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(responseContent, "application/json"); // Trả nguyên JSON từ API
                }
                else
                {
                    return Json(new { success = false, message = !string.IsNullOrEmpty(responseContent) ? responseContent : "Thao tác thất bại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
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
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tải dữ liệu người dùng.";
                return RedirectToAction("Index");
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
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("EdustarAPI");
                var content = new StringContent(JsonSerializer.Serialize(email), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://localhost:7298/api/User/forgetpassword", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Vui lòng kiểm tra email để đặt lại mật khẩu.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(errorContent) ? errorContent : "Gửi email thất bại.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction("ForgetPassword");
        }
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            return View(model: token);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("EdustarAPI");

                var resetDto = new
                {
                    Token = token,
                    NewPassword = newPassword
                };

                var content = new StringContent(JsonSerializer.Serialize(resetDto), Encoding.UTF8, "application/json");

                var response = await client.PutAsync("https://localhost:7298/api/User/resetpassword", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = !string.IsNullOrEmpty(errorContent) ? errorContent : "Đặt lại mật khẩu thất bại.";
                    return RedirectToAction("ResetPassword", new { token });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("ResetPassword", new { token });
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
