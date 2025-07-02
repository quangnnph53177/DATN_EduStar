using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    public class RolesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public RolesController(IHttpClientFactory httpClientFactory)
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
        public IActionResult Index()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Users");
            var response = client.GetAsync("https://localhost:7298/api/Role/getroles").Result;
            if (response.IsSuccessStatusCode)
            {
                var roles = response.Content.ReadAsAsync<List<Role>>().Result;
                return View(roles);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể lấy danh sách vai trò.";
                return RedirectToAction("Index", "User");
            }
        }
        [HttpGet]
        public IActionResult IndexPermission()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Login", "Users");
            var response = client.GetAsync("https://localhost:7298/api/Permissions/all").Result;
            if (response.IsSuccessStatusCode)
            {
                var roles = response.Content.ReadAsAsync<List<Permission>>().Result;
                return View(roles);
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể lấy danh sách quyền.";
                return RedirectToAction("Index", "Roles");
            }
        }
        //[HttpPost("assign-permissions")]
        //public async Task<IActionResult> AssignPermissions(int roleId, List<int> permissionIds)
        //{
        //    var client = GetClientWithToken();
        //    if (client == null) return RedirectToAction("Login", "Account");
        //    var dto = new
        //    {
        //        RoleId = roleId,
        //        PermissionIds = permissionIds
        //    };
        //    var response = await client.PostAsJsonAsync("https://localhost:7298/api/Role/assign-permissions", dto);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        TempData["SuccessMessage"] = "Gán quyền thành công.";
        //        return RedirectToAction("Index", "Roles");
        //    }
        //    else
        //    {
        //        var errorMessage = await response.Content.ReadAsStringAsync();
        //        TempData["ErrorMessage"] = $"Lỗi: {errorMessage}";
        //        return RedirectToAction("Index", "Roles");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> AssignPermissions()
        {
            var client = GetClientWithToken();

            var rolesResponse = await client.GetAsync("https://localhost:7298/api/Role/getroles");
            var permissionsResponse = await client.GetAsync("https://localhost:7298/api/Permissions/all");

            if (!rolesResponse.IsSuccessStatusCode || !permissionsResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không thể tải vai trò hoặc quyền.";
                return View(new AssignPermissionDTO());
            }

            var rolesContent = await rolesResponse.Content.ReadAsStringAsync();
            var permissionsContent = await permissionsResponse.Content.ReadAsStringAsync();

            var roles = JsonSerializer.Deserialize<List<Role>>(rolesContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var permissions = JsonSerializer.Deserialize<List<Permission>>(permissionsContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.Roles = roles?.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.RoleName }).ToList();
            ViewBag.Permissions = permissions;

            return View(new AssignPermissionDTO());
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermissions(AssignPermissionDTO model)
        {
            var client = GetClientWithToken();

            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://localhost:7298/api/Role/assign-permissions", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gán quyền thành công.";
                return RedirectToAction("Index","Roles");
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = $"Lỗi khi gán quyền: {error}";
            return RedirectToAction("AssignPermissions");
        }

    }
}
