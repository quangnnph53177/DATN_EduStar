using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudent _service;
      
        public StudentsController(IStudent service)
        {
            _service = service;
            
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var itemindex = await _service.GetAllStudents();
            if (itemindex == null) return null;
            var addst = itemindex.Select(u => new StudentViewModels
            {
                id = u.UserId,
                FullName = u.User.UserProfile.FullName,
                UserName = u.User.UserName,
                Email = u.User.Email,
                PhoneNumber = u.User.PhoneNumber,
                StudentCode = u.StudentsCode,
                Gender = u.User.UserProfile.Gender,
                Avatar = u.User.UserProfile.Avatar,
                Address = u.User.UserProfile.Address,
                Dob = u.User.UserProfile.Dob,
                Status = u.User.Statuss.GetValueOrDefault(),
            });
            return Ok(addst);
        }
        [HttpGet("auditlog")]
        public async Task<IActionResult> Log()
        {
            var item = await _service.GetAuditLogs();
            if (item == null) return null;
            var result = item.Select(a => new AuditLogViewModel
            {
                Id = a.Id,
                UserName = a.User.UserName,
                NewData = a.NewData,
                OldData = a.OldData,
                Active = a.Active,
                Timestamp = a.Timestamp,
              
            });
            return Ok(result);
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> Details(Guid Id)
        {
            return Ok(await _service.GetById(Id));
        }
        [HttpPut("boss")]
        public async Task<IActionResult> UpdateWithBoss(Guid id, [FromBody] StudentViewModels model)
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
        [HttpPut("beast")]
        public async Task<IActionResult> UpdateWithbeast(Guid Id, [FromBody] StudentViewModels model)
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
        [HttpDelete]
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

                if (sv.id != null )
                    return BadRequest(new { message = "Không thể xóa vì sinh viên đã được phân lớp." });

                return BadRequest(new { message = "Không thể xóa sinh viên vì lý do không xác định." });
            }

            return Ok(new { message = "Xóa sinh viên thành công." });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string studentCode, string fullName, string username, string email)
        {
            // Kiểm tra nếu không có tiêu chí nào
            if (string.IsNullOrWhiteSpace(studentCode) &&
                string.IsNullOrWhiteSpace(fullName) &&
                string.IsNullOrWhiteSpace(username) &&
                string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Vui lòng nhập ít nhất một tiêu chí tìm kiếm." });
            }

            var result = await _service.Search(studentCode, fullName, username, email);

            if (result == null || !result.Any())
            {
                return NotFound(new { message = "Không tìm thấy sinh viên phù hợp." });
            }

            return Ok(result);
        }

        [HttpGet("excel/{Id}")]
        public async Task<IActionResult> excelfile(int Id)
        {
            var cls = await _service.GetStudentsByClass(Id);
            var exc = await _service.ExportStudentsToExcel(cls);
            var fileName = $"DanhSach_SinhVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(exc,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendtoClass(int classId, string subject)
        {
            await _service.SendNotificationtoClass(classId, subject );
            return Ok();
        }

    }
}
