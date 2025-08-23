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
            var result = await _services.GetAll();
            if (result == null) return null;
            var check = result.Select(c => new SchedulesViewModel
            {
                Id = c.Id,
                ClassName = c.ClassName,
                SubjectName = c.Subject.SubjectName,
                RoomCode = c.Room.RoomCode,
                StudyShift = c.StudyShift.StudyShiftName,
                weekdays = c.ScheduleDays.Select(d => d.DayOfWeekk.Weekdays).ToList(),
                starttime = c.StudyShift.StartTime,
                endtime = c.StudyShift.EndTime,
                startdate = c.StartDate,
                enddate = c.EndDate,
                UserId = c.UsersId,
                Status = c.Status.ToString(),

                //StudentCount =int.Parse,

            });
            return Ok(check);
        }
        [HttpGet("codinh")]
        public async Task<IActionResult> getcodinh()
        {
            var result = await _services.GetAllCoDinh();
            return Ok(result);
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
            await _services.AutogenerateSchedule();
            return Ok(new { success = true, message = "Tạo lịch học tự động thành công" });
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(SchedulesDTO model)
        {

            try
            {
                await _services.CreateSchedules(model);
                return Ok(new { message = "Thêm thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi thêm: {ex.Message}" });
            }
        }
        [HttpGet("excel")]
        public async Task<IActionResult> exportbyexcel(Guid id)
        {
            var schedules = await _services.GetByStudent(id);
            var excel = await _services.ExportSchedules(schedules);
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
        [HttpPut("{id}")]
        public async Task<IActionResult> Updatesch(int id, SchedulesDTO model)
        {
            if (id == null)
            {
                return BadRequest("Id không khớp");
            }
            try
            {
                await _services.UpdateSchedules(model);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }

        }
        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            await _services.DeleteSchedules(Id);
            return Ok(new { message = "xóa thành công" });
        }
        [HttpPost("{schedulesId}/assignStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignStudentToClass([FromBody] AssignStudentsRequest request)
        {
            try
            {
                var success = await _services.AssignStudentToClassAsync(request);
                if (success)
                {
                    return Ok(new { success = true, message = "Gán sinh viên vào lớp thành công." });
                }

                return Conflict("Một số sinh viên đã được gán hoặc lớp/sinh viên không tồn tại.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Lỗi máy chủ: {ex.Message}");
            }
        }
        [HttpDelete("{schedulesId}/removeStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveStudentFromClass( int SchedulesId, Guid studentId)
        {
            try
            {
                var success = await _services.RemoveStudentFromClassAsync(SchedulesId, studentId);
                if (success)
                {
                    return Ok(true);
                }
                return NotFound("The student is not enrolled in this class.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }
    }
}
