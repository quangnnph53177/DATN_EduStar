using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintRepos _repos;
        public ComplaintsController(IComplaintRepos complaint)
        {
            _repos = complaint;
        }
        [Authorize]
        [HttpGet("complaints")]
        public async Task<IActionResult> GetAllComplaints()
        {
            try
            {

                var currentUserRoleIds = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => int.Parse(c.Value))
                    .ToList();
                var currentUsername = User.Identity?.Name ?? string.Empty;
                if (currentUserRoleIds == null || currentUsername == null)
                {
                    return Unauthorized("User roles or username not found in context.");
                }
                var complaints = await _repos.GetAllComplaints(currentUserRoleIds, currentUsername);
                return Ok(complaints);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("class-change-complaint")]
        [Authorize]
        public async Task<IActionResult> SubmitClassChangeComplaint([FromBody] ClassChangeComplaintDTO dto)
        {
            if (dto == null || dto.CurrentClassId == 0 || dto.RequestedClassId == 0 || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }
            try
            {
                // Lấy StudentId từ token
                var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var result = await _repos.SubmitClassChangeComplaint(dto, studentId);
                return Ok(new { Message = "Đăng ký khiếu nại thành công.", ComplaintId = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi đăng ký khiếu nại: {ex.Message}");
            }
        }
        [HttpGet("GetstudentinClass")]
        [Authorize]
        public async Task<IActionResult> GetStudentInClass()
        {
            try
            {
                // Lấy userId từ token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("Không tìm thấy thông tin người dùng.");

                var userId = Guid.Parse(userIdClaim.Value);
                var classes = await _repos.GetClassesOfStudent(userId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetOtherClasses/{currentClassId}")]
        [Authorize]
        public async Task<IActionResult> GetOtherClasses(int currentClassId)
        {
            try
            {
                var result = await _repos.GetClassesInSameSubject(currentClassId);

                // Nếu dùng DTO:
                var classDtos = result.Select(c => new ClassCreateViewModel
                {
                    ClassName = c.NameClass,
                    Semester = c.Semester,
                    SubjectId = c.SubjectId ?? 0,
                    YearSchool = (int)c.YearSchool
                }).ToList();

                return Ok(classDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize(Policy = "ProcessComplaintUS")]
        [HttpPut("process/{id}")]
        public async Task<IActionResult> ProcessComplaint(int id, [FromBody] ProcessComplaintDTO dto)
        {
            try
            {
                // Lấy handlerId từ token
                var handlerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (handlerId == null)
                    return Unauthorized("Không xác định được người dùng đăng nhập.");

                // Convert handlerId to Guid
                var guidHandler = Guid.Parse(handlerId);
                var result = await _repos.ProcessClassChangeComplaint(id, dto, guidHandler);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
    }
}
