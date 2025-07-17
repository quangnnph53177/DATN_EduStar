using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachingRegistrationController : ControllerBase
    {
        private readonly ITeachingRegistrationRepos _repos;
        private readonly ILogger<TeachingRegistrationController> _logger;
        public TeachingRegistrationController(ITeachingRegistrationRepos repos, ILogger<TeachingRegistrationController> logger)
        {
            _repos = repos;
            _logger = logger;
        }
        // GET: api/TeachingRegistrationApi/classes
        //[HttpGet("classes")]
        ////[Authorize(Roles = "Teacher")]
        //public async Task<IActionResult> GetAssignedClasses()
        //{
        //    var userName = User.Identity?.Name;
        //    if (string.IsNullOrEmpty(userName))
        //    {
        //        return Unauthorized("Bạn cần đăng nhập để xem danh sách lớp.");
        //    }

        //    var classes = await _repos.GetClasses(userName);
        //    return Ok(classes);
        //}

        //// GET: api/TeachingRegistrationApi/days
        //[HttpGet("days")]
        //public async Task<IActionResult> GetDays()
        //{
        //    var days = await _repos.GetDays();
        //    return Ok(days);
        //}

        //// GET: api/TeachingRegistrationApi/shifts
        //[HttpGet("shifts")]
        //public async Task<IActionResult> GetStudyShifts()
        //{
        //    var shifts = await _repos.GetStudyShifts();
        //    return Ok(shifts);
        //}

        //// GET: api/TeachingRegistrationApi/rooms?dayId=2&shiftId=1&start=2025-08-01&end=2025-12-01
        //[HttpGet("rooms")]
        //public async Task<IActionResult> GetAvailableRooms(int dayId, int shiftId, DateTime start, DateTime end)
        //{
        //    var rooms = await _repos.GetRooms(dayId, shiftId, start, end);
        //    return Ok(rooms);
        //}

        // POST: api/TeachingRegistrationApi/register
        [HttpPost("register")]
        // [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Register([FromBody] TeachingRegistrationViewModel request)
        {
            try
            {
                var teacherId = User.GetUserId();

                // Bước 1: kiểm tra điều kiện đăng ký
                var result = await _repos.CanRegister(
                    teacherId,
                    request.ClassId,
                    request.SemesterId,
                    request.DayId,
                    request.StudyShiftId,
                    request.StartDate,
                    request.EndDate
                );
                if (result != "OK")
                {
                    _logger.LogWarning("Không thể đăng ký: {Message}", result); // THÊM LOG
                    return BadRequest(result); // đây chính là trả về 400
                }

                // Bước 2: tiến hành đăng ký
                result = await _repos.RegisterTeaching(
                    teacherId,
                    request.ClassId,
                    request.SemesterId,
                    request.DayId,
                    request.StudyShiftId,
                    request.StartDate,
                    request.EndDate
                );

                if (result != "Đăng ký thành công.")
                    return BadRequest(result); // Trường hợp xảy ra lỗi khi ghi DB, hoặc lớp không đúng giảng viên

                return Ok(result);

            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                return StatusCode(StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi khi đăng ký: " + ex.Message);


            }
        }

        // PUT: api/TeachingRegistrationApi/confirm/5
        [HttpPut("confirm/{registrationId}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmRegistration(int registrationId)
        {
            var success = await _repos.ConfirmTeachingRegistration(registrationId);
            return Ok(success);
        }
        // GET: api/TeachingRegistrationApi/registrations
        [HttpGet("registrations")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return Unauthorized("Bạn cần đăng nhập.");

            var roleIds = User.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => int.Parse(c.Value))
                .ToList();

            var isAdmin = roleIds.Contains(1); // 1 = Admin
            var registrations = await _repos.GetTeacherRegister(userName, isAdmin);

            return Ok(registrations);
        }
    }
    public static class ClaimsHelper
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userId = user.FindFirst("UserId")?.Value;
            return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
        }
    }
}
