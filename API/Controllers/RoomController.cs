using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoom _service;
        public RoomController(IRoom service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return Ok(await _service.GetAll());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            return Ok(await _service.GetById(id));
        }
        [HttpPost]
        public async Task<IActionResult> Create(RoomDTO ro)
        {
            await _service.Create(ro);
            return Ok("tạo thành công");
        }
        [HttpPut]
        public async Task<IActionResult> Update(RoomDTO ro)
        {
            await _service.Update(ro);
            return Ok("sửa ok");  
        }

    }
}
