using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Azure;
using API.Models;

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
                    //var userId = doc.RootElement.GetProperty("userId").GetInt32();
                    var userName = doc.RootElement.GetProperty("userName").GetString();

                    var claims = new List<Claim>
                     {
                         new(ClaimTypes.Name, userName ?? ""),
                         //new(ClaimTypes.NameIdentifier, userId.ToString()),
                         new("JWToken", token ?? "")
                     };
                    if (doc.RootElement.TryGetProperty("roleId", out var roleArray) && roleArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roleArray.EnumerateArray())
                        {
                            var roleValue = role.GetInt32().ToString();
                            claims.Add(new Claim(ClaimTypes.Role, roleValue));
                        }
                    }
                    if (doc.RootElement.TryGetProperty("permission", out var permissionElement) && permissionElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var perm in permissionElement.EnumerateArray())
                        {
                            var permission = perm.GetString();
                            if (!string.IsNullOrWhiteSpace(permission))
                            {
                                claims.Add(new Claim("Permission", permission));
                            }
                        }
                    }
                    var claimsIdentity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        ClaimTypes.Name,
                        ClaimTypes.Role // 👈 RẤT QUAN TRỌNG
                    );
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
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
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
                return View(new PagedResult<UserDTO>());
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
        public async Task<IActionResult> Register(UserDTO model, IFormFile imgFile)
        {
            try
                {
                if (!ModelState.IsValid)
                    return View(model);

                var client = GetClientWithToken();
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(model.UserName ?? ""), "UserName");
                content.Add(new StringContent(model.PassWordHash ?? ""), "PassWordHash");
                content.Add(new StringContent(model.Email ?? ""), "Email");
                content.Add(new StringContent(model.PhoneNumber ?? ""), "PhoneNumber");
                content.Add(new StringContent(model.FullName ?? ""), "FullName");
                content.Add(new StringContent(model.Address ?? ""), "Address");
                content.Add(new StringContent(model.Statuss.ToString()), "Statuss");

                if (model.Dob.HasValue)
                    content.Add(new StringContent(model.Dob.Value.ToString("yyyy-MM-dd")), "Dob");
                content.Add(new StringContent(model.Gender.HasValue ? model.Gender.Value.ToString() : ""), "Gender");

                if (model.RoleIds != null && model.RoleIds.Any())
                {
                    foreach (var roleId in model.RoleIds)
                    {
                        content.Add(new StringContent(roleId.ToString()), "RoleIds");
                    }
                }

                if (imgFile != null && imgFile.Length > 0)
                {
                    var streamContent = new StreamContent(imgFile.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(imgFile.ContentType);
                    content.Add(streamContent, "imgFile", imgFile.FileName);
                }
                var response = await client.PostAsync("https://localhost:7298/api/User/register", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản thành công.";
                    return RedirectToAction("Index", "Users");
                }
               TempData["ErrorMessage"] = "Đăng ký không thành công.";
                return View(model);

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
                return View(model);
            }
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
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn tệp để tải lên.";
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
                TempData["ErrorMessage"] = "Tải lên không thành công.";
                return View();
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
                return View();
            }
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
        public async Task<IActionResult> SearchUS(string? keyword,int page = 1, int pageSize = 12)
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
                    var totalCount = users.Count;
                    var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction("Index");
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
                    return RedirectToAction("Index");
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

            return RedirectToAction("Index");
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
        [HttpGet]
        public async Task<IActionResult> IndexLog()
        {
            try
            {
                var client = GetClientWithToken();
                var response = await client.GetAsync("https://localhost:7298/api/User/log");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var logs = JsonSerializer.Deserialize<List<AuditLogViewModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return View(logs);
                }
                TempData["ErrorMessage"] = "Không thể tải nhật ký hoạt động.";
                return View(new List<AuditLogViewModel>());

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
                return View();
            }
        }
    }
}
