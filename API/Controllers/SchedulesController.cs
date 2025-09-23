using API.Data;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulesController : ControllerBase
    {
        private readonly IShedulesRepos _services;
        private readonly IAuditLogRepos _auditLog;
        private readonly AduDbcontext _context;
        public SchedulesController(IShedulesRepos services, IAuditLogRepos auditLog, AduDbcontext aduDbcontext)
        {
            _services = services;
            _auditLog = auditLog;
            _context = aduDbcontext;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _services.GetAll();
            if (result == null) return NotFound();

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
                IsActive = c.IsActive
            });
            return Ok(check);
        }
        [Authorize]
        [HttpGet("codinh")]
        public async Task<IActionResult> getcodinh()
        {
            var result = await _services.GetAllCoDinh();
            return Ok(result);
        }
        [Authorize]
        [HttpGet("Id")]
        public async Task<IActionResult> GetByStudent()
        {
            var userId = User.GetUserId();
            var result = await _services.GetByStudent(userId);
            return Ok(result);
        }
        [Authorize]
        [HttpGet("Teacher")]
        public async Task<IActionResult> GetbyTeacher()
        {
            var userId = User.GetUserId();
            var result = await _services.GetByTeacher(userId);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("arrangeschedules")]
        public async Task<IActionResult> getarrschedule()
        {
            await _services.AutogenerateSchedule();
            return Ok(new { success = true, message = "Tạo lịch học tự động thành công" });
        }
        [Authorize(Policy = "CreateUS")]
        [HttpPost("create")]
        public async Task<IActionResult> Create(SchedulesDTO model)
        {
            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? performedByGuid = Guid.TryParse(performedBy, out var guid) ? guid : null;
                await _services.CreateSchedules(model);
                await _auditLog.LogAsync(performedByGuid, "Create Schedule", JsonSerializer.Serialize(new { }), System.Text.Json.JsonSerializer.Serialize(model), performedByGuid);
                return Ok(new { message = "Thêm thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi thêm: {ex.Message}" });
            }
        }
        [Authorize]
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
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Getid(int id)
        {
            try
            {
                var result = await _services.GetById(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Updatesch(int id, SchedulesDTO model)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? performedByGuid = Guid.TryParse(performedBy, out var guid) ? guid : null;
            Console.WriteLine($"[API] Received update request for ID: {id}");
            Console.WriteLine($"[API] Model ID: {model?.Id}");

            // Kiểm tra tham số hợp lệ
            if (model == null)
            {
                return BadRequest(new { message = "Dữ liệu không được để trống" });
            }

            if (id != model.Id)
            {
                return BadRequest(new { message = "Id không khớp với dữ liệu gửi lên" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
            }

            try
            {
                var oldSchedule = await _context.Schedules
                    .Include(s => s.Subject)
                    .Include(s => s.Room)
                    .Include(s => s.StudyShift)
                    .Include(s => s.User) // nếu có relation
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (oldSchedule == null)
                    return NotFound(new { message = "Không tìm thấy lịch học" });
                if (model.TeacherId.HasValue)
                {
                    var teacher = await _context.Users
                        .Where(u => u.Id == model.TeacherId.Value)
                        .Select(u => new { u.UserProfile.FullName })
                        .FirstOrDefaultAsync();

                    model.TeacherName = teacher?.FullName;
                }
                // ⚡ Map dữ liệu cũ sang DTO để tránh vòng lặp
                var oldDataDto = new SchedulesDTO
                {
                    Id = oldSchedule.Id,
                    ClassName = oldSchedule.ClassName,
                    SubjectId = oldSchedule.SubjectId,
                    RoomId = oldSchedule.RoomId,
                    StudyShiftId = oldSchedule.StudyShiftId,
                    StartDate = oldSchedule.StartDate,
                    EndDate = oldSchedule.EndDate,
                    TeacherId = oldSchedule.UsersId,
                    TeacherName = oldSchedule.User?.UserProfile?.FullName,
                    Status = oldSchedule.Status.ToString(),
                };

                Console.WriteLine($"[API] Calling UpdateSchedules service...");

                // 🔥 Audit log: serialize DTO, không serialize entity
                await _auditLog.LogAsync(
                    performedByGuid,
                    "Update Schedule",
                    System.Text.Json.JsonSerializer.Serialize(oldDataDto), // olddata (safe)
                    System.Text.Json.JsonSerializer.Serialize(model),      // newdata
                    performedByGuid
                );

                // Update vào DB
                await _services.UpdateSchedules(model);

                Console.WriteLine($"[API] Update successful");
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Update failed: {ex}");
                return BadRequest(new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }
        }

        [HttpPut("toggle-status/{Id}")]
        public async Task<IActionResult> ToggleStatus(int Id)
        {
            try
            {
                var isActive = await _services.ToggleScheduleStatus(Id);
                var message = isActive ? "Kích hoạt lịch học thành công" : "Vô hiệu hóa lịch học thành công";

                return Ok(new
                {
                    message = message,
                    isActive = isActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi thay đổi trạng thái: {ex.Message}" });
            }
        }
        [Authorize]
        [HttpPost("{schedulesId}/assignStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignStudentToClass([FromBody] AssignStudentsRequest request)
        {
            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? performedByGuid = Guid.TryParse(performedBy, out var guid) ? guid : null;
                await _auditLog.LogAsync(performedByGuid, "Assign Students to Class",

                oldData: JsonSerializer.Serialize(new { SchedulesId = request.SchedulesId }),
                //newData: JsonSerializer.Serialize(new { StudentIds = request.StudentIds }), performedByGuid);
                //oldData: $"SchedulesId: {request.SchedulesId}"// không cần object phức tạp
                JsonSerializer.Serialize(request), performedByGuid);
                var success = await _services.AssignStudentToClassAsync(request);
                if (success)
                {
                    return Ok(new { success = true, message = "Gán học viên vào lớp thành công." });
                }

                return Conflict("Một số học viên đã được gán hoặc lớp/học viên không tồn tại.");
            }
            catch (Exception ex)
            {
                // Kiểm tra lỗi về lịch học bị vô hiệu hóa
                if (ex.Message.Contains("vô hiệu hóa"))
                {
                    return BadRequest(new { message = ex.Message });
                }

                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Lỗi máy chủ: {ex.Message}");
            }
        }
        [Authorize]
        [HttpDelete("{schedulesId}/removeStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveStudentFromClass(int SchedulesId, Guid studentId)
        {
            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? performedByGuid = Guid.TryParse(performedBy, out var guid) ? guid : null;
                await _auditLog.LogAsync(performedByGuid, "Remove Student from Class", "Đã xóa", $"SchedulesId: {SchedulesId}, StudentId: {studentId}", performedByGuid);
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