using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IRegisterService _service;
        public RegisterController(IRegisterService service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> create(IFormFile file)
        {


            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            var users = await _service.ExcelFile(file);
            var result = await _service.CreateBulk(users);

            return Ok(result);
        }
    }
}
