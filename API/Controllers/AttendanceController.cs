using API.Services;
using API.ViewModel;
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
        [HttpGet("{Id}")]
        public async Task<IActionResult> StudentsForAttendance(int Id)
        {
           
            return Ok(await _service.GetStudentsForAttendance(Id));
        }
        [HttpPost]
        public async Task<IActionResult> CreateSession(CreateAttendanceSessionViewModel model)
        {
            await _service.CreateSession(model);
            return Ok(model);
        }
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetSessionsForStudent(Guid studentId)
        {
            return Ok(await _service.GetSessionsForStudent(studentId));
        }
    }
}
