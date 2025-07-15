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
                var createdUser = await _userRepos.Register(userDto, imgFile);
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

                await _logRepos.LogAsync(createdUser.Id, "CreateUser", null, newData, performedByGuid);
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
            var result = await _userRepos.ConfirmEmail(token);
            return Ok(new { success = true, result });
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
        //[HttpPost("upload")]
        //public async Task<IActionResult> Upload(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Không có file được tải lên.");

        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //    try
        //    {
        //        using var stream = new MemoryStream();
        //        await file.CopyToAsync(stream);
        //        stream.Position = 0;

        //        using var package = new ExcelPackage(stream);
        //        var worksheet = package.Workbook.Worksheets[0];
        //        if (worksheet == null)
        //            return BadRequest("File Excel không có worksheet nào.");

        //        int rowCount = worksheet.Dimension.Rows;
        //        var usersCreated = new List<string>();
        //        var usersFailed = new List<object>();

        //        for (int row = 2; row <= rowCount; row++) // Bắt đầu từ row 2 vì row 1 là header
        //        {
        //            try
        //            {
        //                // Đọc từng cột 
        //                var fullname = worksheet.Cells[row, 2].Text.Trim();
        //                var userName = worksheet.Cells[row, 3].Text.Trim();
        //                var password = worksheet.Cells[row, 4].Text.Trim();
        //                var phone = worksheet.Cells[row, 5].Text.Trim();
        //                var dobText = worksheet.Cells[row, 6].Text.Trim();
        //                var genderText = worksheet.Cells[row, 7].Text.Trim();
        //                var email = worksheet.Cells[row, 8].Text.Trim();
        //                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        //                {
        //                    usersFailed.Add(new { Row = row, Reason = "Thiếu username hoặc password." });
        //                    continue;
        //                }
        //                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        //                {
        //                    usersFailed.Add(new { Row = row, Reason = "Thiếu username hoặc password." });
        //                    continue;
        //                }

        //                DateTime? dob = null;
        //                if (DateTime.TryParseExact(dobText, new[] { "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
        //                {
        //                    dob = parsedDob;
        //                }
        //                bool? gender = null;
        //                if (!string.IsNullOrEmpty(genderText))
        //                {
        //                    genderText = genderText.ToLower();
        //                    if (genderText == "nam")
        //                        gender = true;
        //                    else if (genderText == "nữ" || genderText == "nu")
        //                        gender = false;
        //                }
        //                var userDto = new UserDTO
        //                {
        //                    UserName = userName,
        //                    PassWordHash = password,
        //                    PhoneNumber = phone,
        //                    Email = email,
        //                    FullName = fullname,
        //                    Dob = dob,
        //                    Gender = gender,
        //                    Statuss = true,
        //                    CreateAt = DateTime.Now
        //                };
        //                var createdUser = await _userRepos.Register(userDto, null);
        //                usersCreated.Add(userName);
        //                var newData = JsonSerializer.Serialize(new
        //                {
        //                    createdUser.Email,
        //                    createdUser.UserName,
        //                    createdUser.Statuss
        //                });
        //                Guid? performedByGuid = null;
        //                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //                if (Guid.TryParse(performedBy, out var userGuid))
        //                {
        //                    performedByGuid = userGuid;
        //                }

        //                // Kiểm tra performedBy có tồn tại trong DB  
        //                var existed = await _context.Users.FindAsync(performedByGuid);
        //                if (existed == null)
        //                    return BadRequest("Người thực hiện không tồn tại.");

        //                await _logRepos.LogAsync(createdUser.Id, "CreateListUser", null, newData, performedByGuid);
        //            }
        //            catch (Exception exRow)
        //            {
        //                usersFailed.Add(new { Row = row, Reason = exRow.Message });
        //            }
        //        }

        //        return Ok(new { message = "Upload và tạo user thành công", users = usersCreated });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = "Lỗi khi xử lý file Excel: " + ex.Message });
        //    }
        //}
        [HttpPost("preview-upload")]
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
                var previewUsers = new List<object>();

                for (int row = 2; row <= rowCount; row++)
                {
                    var fullname = worksheet.Cells[row, 2].Text.Trim();
                    var userName = worksheet.Cells[row, 3].Text.Trim();
                    var passwordhash = worksheet.Cells[row, 4].Text.Trim();
                    var phone = worksheet.Cells[row, 5].Text.Trim();
                    var dobText = worksheet.Cells[row, 6].Text.Trim();
                    var genderText = worksheet.Cells[row, 7].Text.Trim();
                    var email = worksheet.Cells[row, 8].Text.Trim();
                    var address = worksheet.Cells[row, 9].Text.Trim();

                    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passwordhash))
                    {
                        continue; // Bỏ qua dòng thiếu thông tin cần thiết
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
                        gender = genderText == "nam" ? true : genderText == "nữ" || genderText == "nu" ? false : null;
                    }

                    previewUsers.Add(new
                    {
                        FullName = fullname,
                        UserName = userName,
                        PasswordHash = passwordhash,
                        PhoneNumber = phone,
                        Dob = dob,
                        Gender = gender,
                        Email = email,
                        Address = address,
                    });
                }

                return Ok(new
                {
                    message = "Đọc file Excel thành công",
                    users = previewUsers
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Lỗi khi xử lý file Excel: " + ex.Message });
            }
        }
        [HttpPost("create-from-preview")]
        public async Task<IActionResult> CreateFromPreview([FromBody] List<UserDTO> users)
        {
            if (users == null || users.Count == 0)
                return BadRequest("Không có dữ liệu người dùng để tạo.");

            var created = new List<string>();
            var failed = new List<object>();

            foreach (var userDto in users)
            {
                try
                {
                    // Kiểm tra dữ liệu đầu vào
                    var missingFields = new List<string>();
                    if (string.IsNullOrWhiteSpace(userDto.UserName)) missingFields.Add("username");
                    if (string.IsNullOrWhiteSpace(userDto.PassWordHash)) missingFields.Add("passwordhash");
                    if (string.IsNullOrWhiteSpace(userDto.Email)) missingFields.Add("email");

                    if (missingFields.Any())
                    {
                        failed.Add(new
                        {
                            userDto.UserName,
                            Reason = $"Thiếu trường: {string.Join(", ", missingFields)}"
                        });
                        continue;
                    }

                    if (userDto.Dob.HasValue && userDto.Dob.Value >= DateTime.Today)
                    {
                        failed.Add(new
                        {
                            userDto.UserName,
                            Reason = "Ngày sinh không hợp lệ."
                        });
                        continue;
                    }

                    var createdUser = await _userRepos.Register(userDto, null);
                    created.Add(userDto.UserName);

                    // Ghi log
                    var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    Guid.TryParse(performedBy, out var performedByGuid);
                    var newData = JsonSerializer.Serialize(new
                    {
                        createdUser.UserName,
                        createdUser.Email,
                        createdUser.PhoneNumber,
                        createdUser.Statuss,
                        createdUser.CreateAt
                    });
                    await _logRepos.LogAsync(createdUser.Id, "CreateFromPreview", null, newData, performedByGuid);
                }
                catch (Exception ex)
                {
                    failed.Add(new
                    {
                        userDto.UserName,
                        Reason = ex.Message
                    });
                }
            }

            return Ok(new
            {
                success = true,
                message = "Tạo người dùng từ dữ liệu preview thành công.",
                createdCount = created.Count,
                failedCount = failed.Count,
                createdUsers = created,
                failedUsers = failed
            });
        }

        [HttpDelete("cleanup-unconfirmed")]
        // [Authorize(Policy = "CreateUS")]
        public async Task<IActionResult> CleanupUnconfirmed()
        {
            try
            {
                var deletedUsers = await _userRepos.CleanupUnconfirmedUsers();

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
                var deletedInfo = deletedUsers.Select(u => new
                {
                    u.UserName,
                    u.Email
                }).ToList();

                var newData = JsonSerializer.Serialize(new
                {
                    message = "Đã xóa tài khoản chưa xác nhận.",
                    deletedCount = deletedUsers.Count,
                    deletedAccounts = deletedInfo
                });

                await _logRepos.LogAsync(
                    null, // không ghi cụ thể user bị tác động vì là nhiều user
                    "SoftDelete",
                    null,
                    newData,
                    performedByGuid
                );
                return Ok(new
                {
                    message = $"Đã xóa {deletedUsers.Count} tài khoản chưa xác nhận.",
                    deleteCount = deletedUsers.Count,
                    deletedAccounts = deletedInfo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.InnerException?.Message ?? ex.Message
                });
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
                    filteredUsers = users.Where(u => u.RoleIds.Any(rid => rid == 1 || rid == 2 || rid == 3)/* && u.UserName != currentUserName*/);
                }
                else if (currentUserRoleIds.Contains(2)) // Giảng viên
                {
                    filteredUsers = users.Where(u => u.RoleIds.Any(rid => rid == 3)/* && u.UserName != currentUserName*/);
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

        [HttpGet("admin")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> GetAdminView()
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
                var filtered = users.Where(u => u.RoleIds.Contains(1));
                return Ok(filtered); // hoặc return View("AdminView", filtered);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("teacher")]
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> GetTeacherView()
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
                filteredUsers = users.Where(u => u.RoleIds.Contains(2));
            }
            else if (currentUserRoleIds.Contains(2)) // Giảng viên
            {
                filteredUsers = users.Where(u => u.UserName == currentUserName);
            }
            else
            {
                return Forbid("Bạn không có quyền truy cập vào trang này.");
            }
            return Ok(filteredUsers);
        }

        [HttpGet("student")]
        [Authorize(Roles = "1,2,3")]
        public async Task<IActionResult> GetStudentView()
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
                // Lọc theo vai trò

                Dictionary<string, List<UserDTO>> resultDict;

                if (currentUserRoleIds.Contains(1) || currentUserRoleIds.Contains(2)) // 👑 Admin
                {
                    var filtered = users.Where(u => u.RoleIds.Contains(3)) // Chỉ sinh viên
                         .ToList();

                    return Ok(filtered);
                }
                //else if () // 👨‍🏫 Giảng viên
                //{
                //    var teacher = users.FirstOrDefault(u => u.UserName == currentUserName);
                //    if (teacher == null)
                //        return Forbid("Không tìm thấy giảng viên.");

                //    var classList = await _userRepos.GetStudentByTeacher(teacher.Id);

                //    var uniqueStudents = classList.Classes
                //         .SelectMany(c => c.StudentsInfor)
                //         .Where(s => s.UserName != currentUserName && s.UserName != null)
                //         .GroupBy(s => s.UserName) // hoặc s.Id nếu muốn chắc chắn hơn
                //         .Select(g => g.First())   // chỉ lấy 1 bản ghi duy nhất
                //         .ToList();

                //    return Ok(uniqueStudents);
                //}
                else // 👩‍🎓 Sinh viên -> chỉ trả về lớp của họ
                {
                    var filtered = users.Where(u => u.UserName == currentUserName);

                    return Ok(filtered);
                }
                //return Ok(resultDict);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Exception: {ex.GetType().Name} - {ex.Message}\nStackTrace:\n{ex.StackTrace}");
            }

        }

        [Authorize(Policy = "DetailUS")]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var currentUserRoleIds = User.Claims
                   .Where(c => c.Type == ClaimTypes.Role)
                   .Select(c => int.Parse(c.Value))
                   .ToList();
            var currentUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
                return Unauthorized("Không tìm thấy thông tin người dùng");
            try
            {
                // Lấy tất cả user, sau đó chỉ lấy user có UserName trùng với user hiện tại
                var users = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName);
                var user = users.FirstOrDefault(u => u.UserName == currentUserName);
                if (user == null)
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "SearchUS")]
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
        public async Task<IActionResult> UpdateUser(string username, [FromForm] UserDTO userDto, IFormFile? imgFile)
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
                var allowedUsers = await _userRepos.GetAllUsers(currentUserRoleIds, currentUserName, true);

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
                var oldData = JsonSerializer.Serialize(new
                {
                    lockedUser.Email,
                    lockedUser.UserName,
                    Status = lockedUser.Statuss
                });

                // Gọi xử lý Lock/Unlock
                var resultMessage = await _userRepos.LockUser(username, performedByGuid.Value);

                // Lấy lại thông tin mới sau khi cập nhật
                var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                var description = (bool)updatedUser.Statuss ? $"Mở khóa tài khoản \"{username}\"" : $"Khóa tài khoản \"{username}\"";
                var newData = JsonSerializer.Serialize(new
                {
                    updatedUser.Email,
                    updatedUser.UserName,
                    Status = updatedUser.Statuss,
                    description
                });
                var action = (bool)updatedUser.Statuss ? "Unlock" : "Lock";
                await _logRepos.LogAsync(
                    updatedUser.Id,
                    action,
                    oldData,
                    newData,
                    performedByGuid
                );
                return Ok(new { success = true, message = resultMessage });
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