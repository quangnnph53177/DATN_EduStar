using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintRepos _repos;
        private readonly IAuditLogRepos _auditLogRepos;
        public ComplaintsController(IComplaintRepos complaint, IAuditLogRepos auditLogRepos)
        {
            _repos = complaint;
            _auditLogRepos = auditLogRepos;
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
        [Authorize(Roles = "3")]
        [HttpPost("class-change-complaint")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitClassChangeComplaint([FromForm] ClassChangeComplaintDTO dto, IFormFile imgFile)
        {
            if (dto == null || dto.CurrentClassId == 0 || dto.RequestedClassId == 0 || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }
            if (imgFile == null || imgFile.Length == 0)
            {
                return BadRequest("Vui lòng tải lên hình ảnh minh chứng.");
            }
            try
            {
                // Lấy StudentId từ token
                var studentCode = User.FindFirstValue("UserCode");
                if (string.IsNullOrEmpty(studentCode))
                    return Unauthorized("Không xác định được sinh viên từ token.");
                var result = await _repos.SubmitClassChangeComplaint(dto, studentCode, imgFile);
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? performedByGuid = Guid.TryParse(performedBy, out var guid) ? guid : null;

                await _auditLogRepos.LogAsync(
                    performedByGuid,
                    "SubmitClassChangeComplaint",
                    $"{ dto.CurrentClassId}",
                    JsonSerializer.Serialize(new
                    {
                        StudentCode = studentCode,
                        CurrentClassId = dto.CurrentClassId,
                        RequestedClassId = dto.RequestedClassId,
                        Reason = dto.Reason
                    }),
                    performedByGuid // Người thực hiện hành động
                );
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
                await _auditLogRepos.LogAsync(
                    guidHandler,
                    "ProcessClassChangeComplaint",
                    $"{id}",
                    JsonSerializer.Serialize(dto),
                    guidHandler // Người thực hiện hành động
                );
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
