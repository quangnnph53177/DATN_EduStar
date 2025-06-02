using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticalController : ControllerBase
    {
        private readonly IStatistical _service;
        public StatisticalController(IStatistical service)
        {
            _service = service;
        }
        [HttpGet("StudentByClass")]
        public async Task<IActionResult> StudentByClass()
        {
            return Ok(await _service.GetStudentByClass());
        }
        [HttpGet("StudentByGender")]
        public async Task<IActionResult> StudentByGender()
        {
            return Ok(await _service.GetStudentByGender());
        }
        [HttpGet("StudentByAddress")]
        public async Task<IActionResult> StudentByAddress()
        {
            return Ok(await _service.GetStudentByAddress());
        }
        [HttpGet("StudentByStatus")]
        public async Task<IActionResult> StudentByStatuss()
        {
            return Ok(await _service.GetStudentByStatus());
        }
        //[HttpGet("Room")]
        //public async Task<IActionResult> Room()
        //{
        //    return Ok(await _service.GetRoomStudies());
        //}
    }
}
