using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendance _service;

        public AttendanceController(IAttendance service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")] // Chỉ GV và Admin được tạo phiên
        public async Task<IActionResult> CreateSession([FromBody] CreateAttendanceViewModel model)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _service.CreateSession(model);
                return Ok(new { success = true, message = "Tạo phiên điểm danh thành công", data = model });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSession(
            [FromQuery] int? classId,
            [FromQuery] int? studyShiftid,
            [FromQuery] int? roomid,
            [FromQuery] int? subjectid)
        {
            try
            {
                // Validate các ID nếu có
                if (classId.HasValue && classId.Value <= 0)
                    return BadRequest(new { message = "ClassId không hợp lệ" });

                if (studyShiftid.HasValue && studyShiftid.Value <= 0)
                    return BadRequest(new { message = "StudyShiftId không hợp lệ" });

                if (roomid.HasValue && roomid.Value <= 0)
                    return BadRequest(new { message = "RoomId không hợp lệ" });

                if (subjectid.HasValue && subjectid.Value <= 0)
                    return BadRequest(new { message = "SubjectId không hợp lệ" });

                // Nếu không có filter nào thì lấy tất cả
                if (!classId.HasValue && !studyShiftid.HasValue && !roomid.HasValue && !subjectid.HasValue)
                {
                    var sessions = await _service.GetAllSession();
                    return Ok(sessions);
                }

                // Tìm kiếm với filter
                var searchResults = await _service.Search(classId, studyShiftid, roomid, subjectid);

                if (searchResults == null || !searchResults.Any())
                {
                    return Ok(new List<IndexAttendanceViewModel>()); // Trả về list rỗng thay vì 404
                }

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách phiên điểm danh", error = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetailSession(int id)
        {
            // Validate ID
            if (id <= 0)
            {
                return BadRequest(new { message = "ID phiên điểm danh không hợp lệ" });
            }

            try
            {
                var session = await _service.GetByIndex(id);

                if (session == null)
                {
                    return NotFound(new { message = $"Không tìm thấy phiên điểm danh với ID: {id}" });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết phiên điểm danh", error = ex.Message });
            }
        }

        [HttpPost("checkin")]
        [Authorize(Roles = "Teacher,Admin")] // Chỉ GV và Admin được điểm danh
        public async Task<IActionResult> Checkin([FromBody] CheckInDto dto)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate AttendanceId
            if (dto.AttendanceId <= 0)
            {
                return BadRequest(new { message = "AttendanceId không hợp lệ" });
            }

            // Validate StudentId
            if (dto.StudentId == Guid.Empty)
            {
                return BadRequest(new { message = "StudentId không hợp lệ" });
            }

            // Validate Status (giả sử Status là enum với giá trị 0-2)
            //if (dto.Status < 0 || dto.Status <= 2)
            //{
            //    return BadRequest(new { message = "Status không hợp lệ (0: Vắng, 1: Có mặt, 2: Đi muộn)" });
            //}

            try
            {
                var result = await _service.CheckInStudent(dto);

                if (result)
                {
                    return Ok(new { success = true, message = "Điểm danh thành công" });
                }

                return BadRequest(new { success = false, message = "Điểm danh thất bại" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi điểm danh", error = ex.Message });
            }
        }

        [HttpGet("history")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                // Lấy StudentId từ claims
                var studentIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(studentIdString))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                if (!Guid.TryParse(studentIdString, out var studentId))
                {
                    return BadRequest(new { message = "StudentId không hợp lệ" });
                }

                var history = await _service.GetHistoryForStudent(studentId);

                if (history == null || !history.Any())
                {
                    return Ok(new List<StudentAttendanceHistory>()); // Trả về list rỗng
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử điểm danh", error = ex.Message });
            }
        }

        [HttpGet("teacher-classes")]
        [Authorize(Roles = "Teacher")] // Chỉ giáo viên
        public async Task<IActionResult> GetTeacherClasses()
        {
            try
            {
                var teacherIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(teacherIdString, out var teacherId))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin giáo viên" });
                }

                var classes = await _service.GetTeacherClasses(teacherId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách lớp", error = ex.Message });
            }
        }
    }
}