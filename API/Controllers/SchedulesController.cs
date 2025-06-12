using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulesController : ControllerBase
    {
        private readonly IShedulesRepos _services;
        public SchedulesController(IShedulesRepos services)
        {
            _services = services;
        }
        [HttpGet]
        public async Task<IActionResult> Get() 
        {
            var result =await _services.GetAll();
            if (result == null) return null;
            var check = result.Select(c => new SchedulesViewModel
            {
                Id = c.Id,
                ClassName= c.Class.NameClass,
                RoomCode = c.Room.RoomCode,
                StudyShift = c.StudyShift.StudyShiftName,
                WeekDay = c.Day.Weekdays
            });
            return Ok(check);
        }
        [HttpGet("Id")]
        public async Task<IActionResult> GetByStudent(Guid id)
        {
            var result = await _services.GetByStudent(id);
            return Ok(result);
        }
        [HttpPost("arrangeschedules")]
        public async Task<IActionResult> getarrschedule()
        {
            var  result = _services.AutogenerateSchedule();
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create(SchedulesDTO model)
        {
            var result = await _services.CreateSchedules(model);
            return Ok(result);
        }
        [HttpGet("excel")]
        public async Task<IActionResult> exportbyexcel(Guid id)
        {
            var schedules = await _services.GetByStudent(id);
            var excel  = await _services.ExportSchedules(schedules);
            var filename = $"lichhoccua{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(excel,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Getid(int id)
        {
            return Ok(await _services.GetById(id));
        }
    }
}
