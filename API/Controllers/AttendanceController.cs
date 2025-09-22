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
        public async Task<IActionResult> CreateSession(CreateAttendanceViewModel model)

        {
            await _service.CreateSession(model);
            return Ok(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSession(int? classId, int? studyShiftid, int? roomid, int? subjectid)
        {
            if (classId == null && studyShiftid == null
                && roomid == null && subjectid == null
                )
            {
                var session = await _service.GetAllSession();
                return Ok(session);

            }
            var se = await _service.Search(classId, studyShiftid, roomid, subjectid);
            if (se == null || !se.Any())
            {
                return NotFound(new { message = ("ko có phiên nào như này") });
            }
            return Ok(se);
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetDetailsession(int Id)
        {
            return Ok(await _service.GetByIndex(Id));

        }

        [HttpPost("checkin")]
        public async Task<IActionResult> Checkin([FromBody] CheckInDto dto)
        {
            return Ok(await _service.CheckInStudent(dto));
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var studentIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(studentIdString, out var studentId))
                return Unauthorized();
            var history = await _service.GetHistoryForStudent(studentId);
            return Ok(history);
        }
        [HttpPost("checkin-by-face")]
        //[Authorize(Roles = "Teacher,Admin,Student")]
        public async Task<IActionResult> CheckinByFace([FromForm] CheckInByFaceDto dto)
        {
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}

            var studentIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(studentIdString, out var studentId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin sinh viên" });
            }

            try
            {
                // Chuyển IFormFile thành mảng byte
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await dto.FaceImage.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }

                var result = await _service.CheckInByFace(dto.AttendanceId, studentId, imageBytes);

                if (result)
                {
                    return Ok(new { success = true, message = "Điểm danh bằng khuôn mặt thành công" });
                }
                return BadRequest(new { success = false, message = "Điểm danh thất bại" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi điểm danh bằng khuôn mặt", error = ex.Message });
            }
        }
    }
}
