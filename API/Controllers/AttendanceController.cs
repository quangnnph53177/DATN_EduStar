using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAllSession()
        {
            var session=  await _service.GetAllSession();
            return Ok(session);
        }
        [HttpGet("{Id}")]
        public  async Task<IActionResult> GetDetailsession(int Id)
        {
            return Ok (await _service.GetByIndex(Id));
        }
        [HttpPost("checkin")]
        public async Task<IActionResult> Checkin([FromBody] CheckInDto dto)
        {
            return Ok(await _service.CheckInStudent(dto));
        }
       
        [HttpGet("history")]
        public async Task<IActionResult> History(Guid studentId)
        {
            return Ok(await _service.GetHistoryForStudent(studentId));
        }
    }
}
