using API.Data;
using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using OfficeOpenXml;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class UserController : ControllerBase

    {
        private readonly IUserRepos _userRepos;
        private readonly IAuditLogRepos _logRepos;
        private readonly AduDbcontext _context;
        public UserController(IUserRepos userRepos, IAuditLogRepos auditLogRepos, AduDbcontext aduDbcontext)
        {
            _userRepos = userRepos;
            _logRepos = auditLogRepos;
            _context = aduDbcontext;
        }
       // [Authorize(Policy = "CreateUS")]
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] UserDTO userDto, IFormFile? imgFile)
        {
            if (userDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ");

            try
            {
                var createdUser = await _userRepos.Register(userDto,imgFile);
                var newData = JsonSerializer.Serialize(new
                {
                    createdUser.Email,
                    createdUser.UserName,
                    createdUser.Statuss
                });
                Guid? performedByGuid = null;
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(performedBy, out var userGuid))
                {
                    performedByGuid = userGuid;
                }

                // ✅ Kiểm tra performedBy có tồn tại trong DB  
                var existed = await _context.Users.FindAsync(performedByGuid);
                if (existed == null)
                    return BadRequest("Người thực hiện không tồn tại.");

                await _logRepos.LogAsync(createdUser.Id, "Tạo tài khoản", null, newData, performedByGuid);
                return Ok(new { message = "Đăng ký thành công, vui lòng kiểm tra email để xác nhận.", userId = createdUser.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
        [AllowAnonymous]
        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm(string token)
        {
            bool result = await _userRepos.ConfirmEmail(token);
            return result ? Ok("Xác nhận thành công.") : BadRequest("Xác nhận thất bại.");
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (loginDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu đăng nhập không hợp lệ.");

            try
            {
                var loginResult = await _userRepos.Login(loginDto.UserName, loginDto.Password);
                //return Ok(new { message = "Đăng nhập thành công", loginResult });
                return Ok(loginResult);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { success = false, error = ex.Message });
            }
        }
       // [Authorize(Policy = "CreateUS")]
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
                        {
                            usersFailed.Add(new { Row = row, Reason = "Thiếu username hoặc password." });
                            continue;
                        }

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
                        var createdUser = await _userRepos.Register(userDto,null);
                        usersCreated.Add(userName);
                        var newData = JsonSerializer.Serialize(new
                        {
                            createdUser.Email,
                            createdUser.UserName,
                            createdUser.Statuss
                        });
                        Guid? performedByGuid = null;
                        var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (Guid.TryParse(performedBy, out var userGuid))
                        {
                            performedByGuid = userGuid;
                        }

                        // Kiểm tra performedBy có tồn tại trong DB  
                        var existed = await _context.Users.FindAsync(performedByGuid);
                        if (existed == null)
                            return BadRequest("Người thực hiện không tồn tại.");

                        await _logRepos.LogAsync(createdUser.Id, "Tạo tài khoản âu nâuuuu", null, newData, performedByGuid);
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
       // [Authorize(Policy = "CreateUS")]
        [HttpDelete("cleanup-unconfirmed")]
        public async Task<IActionResult> CleanupUnconfirmed()
        {
            try
            {
                // Gọi repo xử lý xóa tài khoản chưa xác nhận
                await _userRepos.CleanupUnconfirmedUsers(); // Since the method returns void, no assignment is needed.

                // Ghi log
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? performedByGuid = null;

                if (Guid.TryParse(performedBy, out var userGuid))
                {
                    performedByGuid = userGuid;
                }

                var existed = await _context.Users.FindAsync(performedByGuid);
                if (existed == null)
                    return BadRequest("Người thực hiện không tồn tại.");

                string newData = "Đã xóa tài khoản chưa xác nhận."; // Adjusted to reflect the action.

                await _logRepos.LogAsync(
                    null, // không ghi cụ thể user bị tác động vì là nhiều user
                    "Xóa tài khoản chưa xác nhận",
                    null,
                    newData,
                    performedByGuid
                );

                return Ok("Đã xóa tài khoản chưa xác nhận trong 7 ngày.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        //[Authorize(Policy = "DetailUS")]
        [HttpGet("user")]
        public async Task<IActionResult> GetAllUsers()
        {
            // Thêm logic lọc theo vai trò vào trước khi trả về users trong GetAllUsers
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

                // Lọc theo vai trò
                IEnumerable<UserDTO> filteredUsers;
                if (currentUserRoleIds.Contains(1)) // Admin
                {
                    filteredUsers = users.Where(u => u.RoleIds.Any(rid => rid == 1 || rid == 2 || rid == 3) && u.UserName != currentUserName);
                }
                else if (currentUserRoleIds.Contains(2)) // Giảng viên
                {
                    filteredUsers = users.Where(u => u.RoleIds.Any(rid => rid == 3) && u.UserName != currentUserName);
                }
                else
                {
                    filteredUsers = users.Where(u => u.UserName == currentUserName); // Sinh viên chỉ xem thông tin của mình
                }

                if (!filteredUsers.Any())
                    return Forbid();

                return Ok(filteredUsers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        //[Authorize(Policy = "DetailUS")]
        //[HttpGet("me")]
        //public async Task<IActionResult> GetCurrentUser()
        //{
        //    var currentUserRoleIds = User.Claims
        //           .Where(c => c.Type == ClaimTypes.Role)
        //           .Select(c => int.Parse(c.Value))
        //           .ToList();
        //    var currentUserName = User.Identity?.Name;
        //    if (string.IsNullOrEmpty(currentUserName))
        //        return Unauthorized("Không tìm thấy thông tin người dùng");
        //    try
        //    {
        //        // Lấy tất cả user, sau đó chỉ lấy user có UserName trùng với user hiện tại
        //        var users = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);
        //        var user = users.FirstOrDefault(u => u.UserName == currentUserName);
        //        if (user == null)
        //            return NotFound("Không tìm thấy người dùng hiện tại.");
        //        return Ok(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}
       // [Authorize(Policy = "SearchUS")]
        [HttpGet("searchuser")]
        public async Task<IActionResult> SearchUser([FromQuery] string? keyword)
        {
            var currentUserRoleIds = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => int.Parse(c.Value)).ToList();
            var currentUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
                return Unauthorized("Không tìm thấy thông tin người dùng");
            try
            {
                var users = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.ToLower();

                    users = users.Where(u =>
                        (u.UserName != null && u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (u.UserCode != null && u.UserCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        ((u.Statuss ? "hoạt động" : "đã khóa").Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (u.Email != null && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    );
                }

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
       // [Authorize(Policy = "EditUS")]
        [HttpPut("updateuser/{username}")]
        public async Task<IActionResult> UpdateUser(string username, [FromForm] UserDTO userDto,IFormFile? imgFile)
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
                var allowedUsers = await _userRepos.GetAllUsersNoTeacher(currentUserRoleIds, currentUserName);

                // Kiểm tra người cần sửa có nằm trong danh sách được phép không
                var targetUser = allowedUsers.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (targetUser == null)
                    return BadRequest("Bạn không có quyền sửa người dùng này.");

                // Serialize old data
                var oldData = JsonSerializer.Serialize(new
                {
                    targetUser.UserName,
                    targetUser.Email,
                    targetUser.Statuss,
                    targetUser.UserCode,
                    targetUser.FullName,
                    targetUser.Avatar,
                    targetUser.Address,
                    targetUser.PhoneNumber,
                    targetUser.Dob
                });

                // 4️⃣ Thực hiện cập nhật
                await _userRepos.UpdateUser(userDto, imgFile);
                var newData = JsonSerializer.Serialize(new
                {
                    userDto.UserName,
                    userDto.Email,
                    userDto.Statuss,
                    userDto.UserCode,
                    userDto.FullName,
                    userDto.Avatar,
                    userDto.Address,
                    userDto.PhoneNumber,
                    userDto.Dob
                });

                //// Lấy lại thông tin user sau khi update để log new data
                //var updatedUsers = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);
                //var updatedUser = updatedUsers.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
                Guid? performedByGuid = null;
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(performedBy, out var userGuid))
                {
                    performedByGuid = userGuid;
                }

                // Kiểm tra performedBy có tồn tại trong DB  
                var existed = await _context.Users.FindAsync(performedByGuid);
                if (existed == null)
                    return BadRequest("Người thực hiện không tồn tại.");

                await _logRepos.LogAsync(targetUser.Id, $"Sửa tài khoản {username}", oldData, newData, performedByGuid);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
       // [Authorize(Policy = "CreateUS")]
        [HttpPost("lock/{username}")]
        public async Task<IActionResult> LockUser(string username)
        {
            try
            {
                var result = await _userRepos.LockUser(username);

                // Ghi log
                Guid? performedByGuid = null;
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(performedBy, out var userGuid))
                {
                    performedByGuid = userGuid;
                }

                // Kiểm tra performedBy có tồn tại trong DB  
                var existed = await _context.Users.FindAsync(performedByGuid);
                if (existed == null)
                    return BadRequest("Người thực hiện không tồn tại.");

                // Lấy thông tin user bị khóa để log
                var lockedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                string? oldData = null;
                string? newData = null;
                if (lockedUser != null)
                {
                    oldData = JsonSerializer.Serialize(new
                    {
                        lockedUser.Email,
                        lockedUser.UserName,
                        Statuss = !lockedUser.Statuss // Trạng thái trước khi khóa (giả định là ngược lại)
                    });
                    newData = JsonSerializer.Serialize(new
                    {
                        lockedUser.Email,
                        lockedUser.UserName,
                        lockedUser.Statuss // Trạng thái sau khi khóa
                    });
                }

                await _logRepos.LogAsync(lockedUser.Id, $"Khóa/Mở tài khoản {username}", oldData, newData, performedByGuid);
                return Ok(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        //[Authorize(Policy = "CreateUS")]
        [HttpPut("changerole/{username}")]
        public async Task<IActionResult> ChangeRole(string username, [FromQuery] int newRoleId)
        {
            try
            {
                // Lấy thông tin user trước khi đổi role để log oldData
                var userBefore = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                string? oldData = null;
                if (userBefore != null)
                {
                    oldData = JsonSerializer.Serialize(new
                    {
                        userBefore.Email,
                        userBefore.UserName,
                        userBefore.Statuss
                    });
                }

                var result = await _userRepos.ChangeRole(username, newRoleId);

                // Lấy thông tin user sau khi đổi role để log newData
                var userAfter = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                string? newData = null;
                if (userAfter != null)
                {
                    newData = JsonSerializer.Serialize(new
                    {
                        userAfter.Email,
                        userAfter.UserName,
                        userAfter.Statuss
                    });
                }

                // Ghi log
                Guid? performedByGuid = null;
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(performedBy, out var userGuid))
                {
                    performedByGuid = userGuid;
                }

                // Kiểm tra performedBy có tồn tại trong DB  
                var existed = await _context.Users.FindAsync(performedByGuid);
                if (existed == null)
                    return BadRequest("Người thực hiện không tồn tại.");

                await _logRepos.LogAsync(userAfter.Id, $"Đổi vai trò tài khoản sang roleId {newRoleId}", oldData, newData, performedByGuid);

                return Ok(new { username, result });
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("forgetpassword")]
        public async Task<IActionResult> ForgetPassword([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email không được để trống.");
            try
            {
                await _userRepos.ForgetPassword(email);
                return Ok("Đã gửi email đặt lại mật khẩu nếu email tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [AllowAnonymous]
        [HttpPut("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetDto)
        {
            if (resetDto == null || string.IsNullOrEmpty(resetDto.Token) || string.IsNullOrEmpty(resetDto.NewPassword))
                return BadRequest("Dữ liệu không hợp lệ.");
            try
            {
                var result = await _userRepos.ResetPassword(resetDto.Token, resetDto.NewPassword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        //[Authorize]
        [HttpGet("log")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var roleIds = User.Claims
               .Where(c => c.Type == ClaimTypes.Role)
               .Select(c => int.Parse(c.Value))
               .ToList();

            var currentUserName = User.Identity?.Name;

            var logs = await _logRepos.GetAuditLogsAsync(roleIds, currentUserName);

            var result = logs.Select(a => new AuditLogViewModel
            {
                Id = a.Id,
                UserName = a.User?.UserName == currentUserName ? "Bạn" : a.User?.UserName ?? "Không xác định",
                NewData = a.NewData,
                OldData = a.OldData,
                Active = a.Active,
                PerformeByName = a.PerformeByNavigation?.UserName,
                Timestamp = a.Timestamp
            }).OrderByDescending(x => x.Timestamp).ToList();

            return Ok(result);
        }
        public class ResetPasswordDTO
        {
            public string Token { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
        }
    }
}