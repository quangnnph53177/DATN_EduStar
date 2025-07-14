using API.Services;
using API.Services.Repositories;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudent _service;
        private readonly IUserRepos _userRepos;

        public StudentsController(IStudent service, IUserRepos userRepos)
        {
            _service = service;
            _userRepos = userRepos;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? StudentCode, string? fullName, string? username, string? email, bool? gender, bool? status)
        {
            if (string.IsNullOrEmpty(StudentCode) &&
                    string.IsNullOrEmpty(fullName) &&
                    string.IsNullOrEmpty(username) &&
                    string.IsNullOrEmpty(email) &&
                        gender == null &&
                        status == null)
            {
                var itemindex = await _service.GetAllStudents();

                return Ok(itemindex);
            }
            var result = await _service.Search(StudentCode, fullName, username, email, gender, status);

            if (result == null || !result.Any())
            {
                return NotFound(new { message = "Không tìm thấy sinh viên phù hợp." });
            }

            return Ok(result);
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

        [HttpGet("auditlog")]
        public async Task<IActionResult> Log()
        {
            try
            {
                var data = await _service.GetAuditLogs();
                if (data == null) return NotFound("Không có dữ liệu audit.");

                var result = data.Select(a => new AuditLogViewModel
                {
                    Id = a.Id,
                    UserName = a.User?.UserName ?? "Unknown",
                    NewData = a.NewData,
                    OldData = a.OldData,
                    Active = a.Active,
                    Timestamp = a.Timestamp
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Ghi log ex.Message vào log file nếu cần
                return StatusCode(500, "Lỗi server khi lấy audit log.");
            }
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> Details(Guid Id)
        {
            return Ok(await _service.GetById(Id));
        }
        [HttpPut("boss/{id}")]
        public async Task<IActionResult> UpdateWithBoss(Guid id, StudentViewModels model)
        {
            if (id != model.id)
            {
                return BadRequest("ID không khớp.");
            }

            try
            {
                await _service.UpdatebyBoss(model);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }
        }
        [HttpPut("beast/{id}")]
        public async Task<IActionResult> UpdateWithbeast(Guid Id, StudentViewModels model)
        {
            if (Id != model.id)
            {
                return BadRequest("Id không khớp");
            }
            try
            {
                await _service.UpdateByBeast(model);
                return Ok(new { message = "Cập nhập thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhập :{ex.Message}" });
            }
        }
        [HttpPut("{id}/toggle-active")]
        public async Task<IActionResult> OpenandLock(Guid id)
        {
            var result = await _service.KhoaMoSinhVienAsync(id);
            if (!result) return NotFound("Không tìm thấy sinh viên.");
            return Ok("Cập nhật trạng thái thành công.");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Kiểm tra lại thông tin sinh viên để xác định lý do
            var sv = await _service.GetById(id);

            var result = await _service.DeleteStudent(id);

            if (!result)
            {
                if (sv == null)
                    return NotFound(new { message = "Không tìm thấy sinh viên." });

                if (sv.Status == true)
                    return BadRequest(new { message = "Không thể xóa vì sinh viên đang hoạt động." });

                if (sv.id != null)
                    return BadRequest(new { message = "Không thể xóa vì sinh viên đã được phân lớp." });

                return BadRequest(new { message = "Không thể xóa sinh viên vì lý do không xác định." });
            }

            return Ok(new { message = "Xóa sinh viên thành công." });
        }

        //[HttpGet("search")]
        //public async Task<IActionResult> Search(string StudentCode, string fullName, string username, string email,bool gender)
        //{
        //    // Kiểm tra nếu không có tiêu chí nào
        //    if (string.IsNullOrEmpty(StudentCode) &&
        //        string.IsNullOrEmpty(fullName) &&
        //        string.IsNullOrEmpty(username) &&
        //        string.IsNullOrEmpty(email) &&
        //            gender == null)
        //    {
        //        return BadRequest(new { message = "Vui lòng nhập ít nhất một tiêu chí tìm kiếm." });
        //    }

        //    var result = await _service.Search(StudentCode, fullName, username, email,gender);

        //    if (result == null || !result.Any())
        //    {
        //        return NotFound(new { message = "Không tìm thấy sinh viên phù hợp." });
        //    }

        //    return Ok(result);
        //}

        [HttpGet("excel")]
        public async Task<IActionResult> excelfile()
        {
            var cls = await _service.GetAllStudents();
            var exc = await _service.ExportStudentsToExcel(cls);
            var fileName = $"DanhSach_SinhVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(exc,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendtoClass(int classId, string subject)
        {
            await _service.SendNotificationtoClass(classId, subject);
            return Ok();
        }
        [HttpPost("gui")]
        public async Task<IActionResult> send(string subject, string message, Guid id)
        {
            await _service.SendAsync(subject, message, id);
            return Ok("đã gửi thành công");
        }

    }
}
