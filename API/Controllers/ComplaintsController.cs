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
                return BadRequest(ex.Message);
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
                var studentId = User.FindFirstValue("UserCode");

                var result = await _repos.SubmitClassChangeComplaint(dto, studentId);
                return Ok(new { Message = "Đăng ký khiếu nại thành công.", ComplaintId = result });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetClassesOfStudent")]
        [Authorize]
        public async Task<IActionResult> GetClassesOfStudent([FromQuery] string? userCode = null)
        {
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                string usercodes;

                if (role == "3")
                {
                    usercodes = User.FindFirstValue("UserCode");
                    if (string.IsNullOrEmpty(usercodes))
                        return Unauthorized("Không tìm thấy mã sinh viên trong token.");
                }
                else
                {
                    if (string.IsNullOrEmpty(userCode))
                        return BadRequest("Mã sinh viên không được để trống.");
                    usercodes = userCode;
                }

                var classes = await _repos.ClassesOfStudent(usercodes);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("chiTietKhieuNai/{id}")]
        public async Task<IActionResult> ChiTietKhieuNai(int id)
        {
            try
            {
                var result = await _repos.ChiTietKhieuNai(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
    }
}
