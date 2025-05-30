using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepos _userRepos;
        public UserController(IUserRepos userRepos)
        {
            _userRepos = userRepos;
        }
        [Authorize(Policy = "CreateUS")]//Nếu chưa có tài khoản thì commit cái này lại để tạo tài khoản để đăng nhập
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDto)
        {
            if (userDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ");

            try
            {
                var user = await _userRepos.Register(userDto);
                return Ok(new { message = "Đăng ký thành công, vui lòng kiểm tra email để xác nhận.", userId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        //[HttpGet("confirm")]
        //public async Task<IActionResult> Confirm(string token)
        //{
        //    bool result = await _userRepos.ConfirmEmail(token);
        //    return result ? Ok("Xác nhận thành công.") : BadRequest("Xác nhận thất bại.");
        //}
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (loginDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu đăng nhập không hợp lệ.");

            try
            {
                var token = await _userRepos.Login(loginDto.UserName, loginDto.Password);
                return Ok(new { message = "Đăng nhập thành công", token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "CreateUS")]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được tải lên.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null)
                    return BadRequest("File Excel không có worksheet nào.");

                int rowCount = worksheet.Dimension.Rows;
                var usersCreated = new List<string>();
                var usersFailed = new List<object>();

                for (int row = 2; row <= rowCount; row++) // Bắt đầu từ row 2 vì row 1 là header
                {
                    try
                    {
                        // Đọc từng cột 
                        var fullname = worksheet.Cells[row, 2].Text.Trim();
                        var userName = worksheet.Cells[row, 3].Text.Trim();
                        var password = worksheet.Cells[row, 4].Text.Trim();
                        var phone = worksheet.Cells[row, 5].Text.Trim();
                        var dobText = worksheet.Cells[row, 6].Text.Trim();
                        var genderText = worksheet.Cells[row, 7].Text.Trim();
                        var email = worksheet.Cells[row, 8].Text.Trim();
                        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                        {
                            usersFailed.Add(new { Row = row, Reason = "Thiếu username hoặc password." });
                            continue;
                        }
                        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                            continue;

                        DateTime? dob = null;
                        if (DateTime.TryParseExact(dobText, new[] { "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
                        {
                            dob = parsedDob;
                        }
                        bool? gender = null;
                        if (!string.IsNullOrEmpty(genderText))
                        {
                            genderText = genderText.ToLower();
                            if (genderText == "nam")
                                gender = true;
                            else if (genderText == "nữ" || genderText == "nu")
                                gender = false;
                        }
                        var userDto = new UserDTO
                        {
                            UserName = userName,
                            PassWordHash = password,
                            PhoneNumber = phone,
                            Email = email,
                            FullName = fullname,
                            Dob = dob,
                            Gender = gender,
                            Statuss = true,
                            CreateAt = DateTime.Now
                        };
                        await _userRepos.Register(userDto);
                        usersCreated.Add(userName);
                    }
                    catch (Exception exRow)
                    {
                        usersFailed.Add(new { Row = row, Reason = exRow.Message });
                    }
                }

                return Ok(new { message = "Upload và tạo user thành công", users = usersCreated });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Lỗi khi xử lý file Excel: " + ex.Message });
            }
        }

        [Authorize(Policy = "DetailUS")]
        [HttpGet("user")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var currentUserRoleIds = User.Claims
                 .Where(c => c.Type == ClaimTypes.Role)
                 .Select(c => int.Parse(c.Value))
                 .ToList();
                var currentUserName = User.Identity?.Name;
                if (string.IsNullOrEmpty(currentUserName))
                    return Unauthorized("Không tìm thấy thông tin người dùng");
                var users = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);

                if (!users.Any())
                    return Forbid();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "DetailUS")]
        [HttpGet("searchuser")]
        public async Task<IActionResult> SearchUser([FromQuery] string? username, [FromQuery] string? usercode, [FromQuery] string? fullname, [FromQuery] string? email)
        {
            var currentUserRoleIds = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => int.Parse(c.Value)).ToList();
            var currentUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
                return Unauthorized("Không tìm thấy thông tin người dùng");
            try
            {
                var users = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);

                // 🔍 Lọc theo điều kiện tìm kiếm  
                if (!string.IsNullOrWhiteSpace(username))
                    users = users.Where(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(usercode))
                    users = users.Where(u => u.UserCode?.Contains(usercode, StringComparison.OrdinalIgnoreCase) == true);

                if (!string.IsNullOrWhiteSpace(fullname))
                    users = users.Where(u => u.FullName?.Contains(fullname, StringComparison.OrdinalIgnoreCase) == true);

                if (!string.IsNullOrWhiteSpace(email))
                    users = users.Where(u => u.Email?.Contains(email, StringComparison.OrdinalIgnoreCase) == true);

                var result = users.ToList();

                if (!result.Any())
                    return NotFound("Không tìm thấy người dùng nào phù hợp.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [Authorize(Policy = "EditUS")]
        [HttpPut("{username}")]
        public async Task<IActionResult> UpdateUser(string username, [FromBody] UserDTO userDto)
        {
            var currentUserRoleIds = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => int.Parse(c.Value)).ToList();
            var currentUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
                return Unauthorized("Không tìm thấy thông tin người dùng");

            if (username != userDto.UserName || !ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ hoặc ID không khớp.");
            try
            {
                // Lấy danh sách user được phép truy cập
                var allowedUsers = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);

                // Kiểm tra người cần sửa có nằm trong danh sách được phép không
                var targetUser = allowedUsers.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (targetUser == null)
                    return Forbid("Bạn không có quyền sửa người dùng này.");
                await _userRepos.UpdateUser(userDto);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "CreateUS")]
        [HttpGet("lock/{username}")]
        public async Task<IActionResult> LockUser(string username)
        {
            try
            {
                var result = await _userRepos.LockUser(username);
                return Ok(new { username, result });
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }
        [Authorize(Policy = "CreateUS")]
        [HttpGet("changerole/{username}/{newRoleId}")]
        public async Task<IActionResult> ChangeRole(string username, int newRoleId)
        {
            try
            {
                var result = await _userRepos.ChangeRole(username, newRoleId);
                return Ok(new { username, result });
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }
    }
}