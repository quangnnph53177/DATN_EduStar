using API.Data;
using API.Services;
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
    [Authorize] // Yêu cầu đăng nhập
    public class TeacherAttendanceController : ControllerBase
    {
       
        private readonly IAttendance _attendanceService;
        private readonly AduDbcontext _context;
        public TeacherAttendanceController(IAttendance attendanceService)
        {
            _attendanceService = attendanceService;
        }

        // Lấy danh sách các lớp giảng viên dạy
        [HttpGet("my-classes")]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var teacherIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(teacherIdString, out var teacherId))
                    return Unauthorized("Không xác định được giảng viên");

                var classes = await _attendanceService.GetTeacherClasses(teacherId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Tạo phiên điểm danh cho lớp của giảng viên
        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSessionForMyClass([FromBody] CreateAttendanceViewModel model)
        {
            try
            {
                var teacherIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(teacherIdString, out var teacherId))
                    return Unauthorized("Không xác định được giảng viên");

                // Kiểm tra lớp có thuộc về giảng viên này không
                var schedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.Id == model.SchedulesId && s.UsersId == teacherId);

                if (schedule == null)
                    return BadRequest("Bạn không có quyền tạo phiên cho lớp này");

                // Sử dụng lại method CreateSession đã có
                await _attendanceService.CreateSession(model);

                return Ok(new { message = "Tạo phiên điểm danh thành công", data = model });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Xem chi tiết phiên điểm danh của lớp mình
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetMySessionDetail(int sessionId)
        {
            try
            {
                var teacherIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(teacherIdString, out var teacherId))
                    return Unauthorized("Không xác định được giảng viên");

                // Kiểm tra phiên có thuộc về giảng viên này không
                var attendance = await _context.Attendances
                    .Include(a => a.Schedules)
                    .FirstOrDefaultAsync(a => a.Id == sessionId && a.Schedules.UsersId == teacherId);

                if (attendance == null)
                    return NotFound("Không tìm thấy phiên điểm danh hoặc bạn không có quyền xem");

                // Sử dụng lại method GetByIndex đã có
                var result = await _attendanceService.GetByIndex(sessionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
