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
    }
}