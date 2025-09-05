using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static API.Models.TeachingRegistration;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachingRegistrationController : ControllerBase
    {
        private readonly ITeachingRegistrationRepos _repos;
        public TeachingRegistrationController(ITeachingRegistrationRepos repos)
        {
            _repos = repos;
        }
        // lịch có thể đăng ký
        [HttpGet("allschedules")]
        public async Task<IActionResult> Allschedules()
        {
            try
            {
                var teacherId = User.GetUserId();
                var schedules = await _repos.GetAvailableSchedules(teacherId);
                return Ok(new { success = true, data = schedules });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        // giảng viên đăng ký
        [HttpPost("register")]
       // [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RegisterTeaching(int schedulesID)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                var teacherId = User.GetUserId();
                var result = await _repos.TeacherRegistration(teacherId, schedulesID);

                if (result.Contains("thành công"))
                    return Ok(new { success = true, message = result });
                else
                    return BadRequest(new { success = false, message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        //lịch giáo viên đã đăng ký
        [HttpGet("my-registrations")]
        //[Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            try
            {
                var teacherId = User.GetUserId();
                var registrations = await _repos.GetTeacherRegistrations(teacherId);
                return Ok(new { success = true, data = registrations });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        //tất cả lịch đã đăng ký
        [HttpGet("admin/all-registrations")]
     //   [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRegistrations([FromQuery] string? status = null)
        {
            try
            {
                var registrations = await _repos.GetAllRegistration(status);
                return Ok(new { success = true, data = registrations });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        //duyệt
        [HttpPut("admin/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRegistration(int RegistrationId, ApprovedStatus Approve)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                var adminId = User.GetUserId();
                var result = await _repos.ApproveRegistration(
                    RegistrationId,
                    Approve,
                    adminId
                );

                if (result.Contains("thành công") || result.Contains("từ chối"))
                    return Ok(new { success = true, message = result });
                else
                    return BadRequest(new { success = false, message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
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
        //    [HttpPost("register")]
        //    // [Authorize(Roles = "Teacher")]
        //    public async Task<IActionResult> Register([FromBody] TeachingRegistrationViewModel request, [FromQuery] string dayGroup)
        //    {
        //        try
        //        {
        //            var teacherId = User.GetUserId();
        //            List<int> dayIds = !string.IsNullOrEmpty(dayGroup)
        //            ? dayGroup switch
        //            {
        //                "2-4-6" => new List<int> { 2, 4, 6 },
        //                "3-5-7" => new List<int> { 3, 5, 7 },
        //                _ => new List<int>()
        //            }
        //            : new List<int> { request.DayId };

        //            if (!dayIds.Any())
        //                return BadRequest("Lịch học không hợp lệ. Vui lòng chọn Thứ 2-4-6 hoặc Thứ 3-5-7.");
        //            // ✅ Bước 2: Kiểm tra từng ngày
        //            var canRegister = await _repos.CanRegister(
        //                teacherId,
        //                request.ClassId,
        //                request.SemesterId,
        //                dayIds,
        //                request.StudyShiftId,
        //                request.StartDate,
        //                request.EndDate
        //            );

        //            if (canRegister != "OK")
        //            {
        //                return BadRequest($"Không thể đăng ký cho Thứ {dayIds}: {canRegister}");
        //            }

        //            // ✅ Bước 3: Tiến hành đăng ký tất cả ngày
        //            var result = await _repos.RegisterTeaching(
        //                teacherId,
        //                request.ClassId,
        //                request.SemesterId,
        //                dayIds,
        //                request.StudyShiftId,
        //                request.StartDate,
        //                request.EndDate
        //            );

        //            if (!result.Contains("thành công"))
        //                return BadRequest(result);

        //            return Ok(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Ghi log lỗi nếu cần
        //            return StatusCode(StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi khi đăng ký: " + ex.Message);


        //        }
        //    }

        //    // PUT: api/TeachingRegistrationApi/confirm/5
        //    [HttpPut("confirm/{registrationId}")]
        //    // [Authorize(Roles = "Admin")]
        //    public async Task<IActionResult> ConfirmRegistration(int registrationId)
        //    {
        //        var success = await _repos.ConfirmTeachingRegistration(registrationId);
        //        return Ok(success);
        //    }
        //    // GET: api/TeachingRegistrationApi/registrations
        //    [HttpGet("registrations")]
        //    public async Task<IActionResult> GetMyRegistrations()
        //    {
        //        var userName = User.Identity?.Name;
        //        if (string.IsNullOrEmpty(userName))
        //            return Unauthorized("Bạn cần đăng nhập.");

        //        var roleIds = User.Claims
        //            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
        //            .Select(c => int.Parse(c.Value))
        //            .ToList();

        //        var isAdmin = roleIds.Contains(1); // 1 = Admin
        //        var registrations = await _repos.GetTeacherRegister(userName, isAdmin);

        //        return Ok(registrations);
        //    }
        //}
        //public static class ClaimsHelper
        //{
        //    public static Guid GetUserId(this ClaimsPrincipal user)
        //    {
        //        var userId = user.FindFirst("UserId")?.Value;
        //        return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
        //    }
    }
}
