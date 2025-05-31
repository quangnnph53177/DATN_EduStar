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
        [HttpGet]
        public async Task<IActionResult> StudentByClass()
        {
            return Ok(await _service.GetStudentByClass());
        }
    }
}
