using API.Data;
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
    [Authorize] 
    public class TeacherAttendanceController : ControllerBase
    {
       
        private readonly IAttendance _attendanceService;
        private readonly AduDbcontext _context;
        public TeacherAttendanceController(IAttendance attendanceService, AduDbcontext context)
        {
            _context = context;
            _attendanceService = attendanceService;
        }
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


        // Trong AttendanceController hoặc SchedulesController
        [HttpPost("CreateQuickSession")]
        public async Task<IActionResult> CreateQuickSession([FromBody] CreateQuickSessionDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var model = new CreateAttendanceViewModel
                {
                    SchedulesId = dto.ScheduleId,
                    SessionCode = dto.SessionCode ?? GenerateSessionCode(),
                    Starttime = DateTime.Now,
                    Endtime = DateTime.Now.AddMinutes(30) // Hoặc thời gian kết thúc phù hợp
                };
                await _attendanceService.CreateSession(model);
                return Ok(new
                {
                    success = true,
                    message = "Tạo phiên điểm danh thành công!",
                    sessionCode = model.SessionCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private string GenerateSessionCode()
        {
            return DateTime.Now.ToString("yyyyMMddHHmm") + new Random().Next(1000, 9999);
        }

        public class CreateQuickSessionDto
        {
            public int ScheduleId { get; set; }
            public string? SessionCode { get; set; }
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
