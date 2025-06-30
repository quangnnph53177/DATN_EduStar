//using API.Models;
//using API.Services;
//using API.ViewModel;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace API.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ComplaintsController : ControllerBase
//    {
//        private readonly IComplaintRepos _repos;
//        public ComplaintsController(IComplaintRepos complaint)
//        {
//            _repos = complaint;
//        }
//        [Authorize]
//        [HttpGet("complaints")]
//        public async Task<IActionResult> GetAllComplaints()
//        {
//            try
//            {
//                var currentUserRoleIds = User.Claims
//                    .Where(c => c.Type == ClaimTypes.Role)
//                    .Select(c => int.Parse(c.Value))
//                    .ToList();
//                var currentUsername = User.Identity?.Name ?? string.Empty;
//                if (currentUserRoleIds == null || currentUsername == null)
//                {
//                    return Unauthorized("User roles or username not found in context.");
//                }
//                var complaints = await _repos.GetAllComplaints(currentUserRoleIds, currentUsername);
//                var vm = complaints.Select(c => new ComplaintViewModel
//                {
//                    Id = c.Id,
//                    ComplaintType = c.ComplaintType,
//                    Statuss = c.Statuss,
//                    Reason = c.Reason,
//                    ProofUrl = c.ProofUrl,
//                    StudentName = c.Student?.UserProfile?.FullName,
//                    ProcessedByName = c.ProcessedByNavigation?.UserProfile?.FullName,
//                    CreateAt = c.CreateAt,
//                    ProcessedAt = c.ProcessedAt,
//                    ResponseNote = c.ResponseNote
//                });
//                return Ok(vm);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving complaints: {ex.Message}");
//            }
//        }
//        [HttpPost("register")]
//        [Authorize]
//        public async Task<IActionResult> RegisterComplaint([FromBody] CreateComplaintDTO model)
//        {
//            if (string.IsNullOrWhiteSpace(model.ComplaintType) || string.IsNullOrWhiteSpace(model.Reason))
//                return BadRequest("Loại khiếu nại và lý do không được để trống.");

//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (userIdString == null)
//                return Unauthorized("Không thể xác định người dùng.");

//            var complaint = new Complaint
//            {
//                StudentId = Guid.Parse(userIdString),
//                ComplaintType = model.ComplaintType,
//                Reason = model.Reason,
//                ProofUrl = model.ProofUrl,
//                Statuss = "Chờ xử lý",
//                CreateAt = DateTime.UtcNow
//            };

//            var result = await _repos.CreateComplaint(complaint);

//            return Ok(new
//            {
//                Message = "Đăng ký khiếu nại thành công.",
//                ComplaintId = result.Id
//            });
//        }
//        public class CreateComplaintDTO
//        {
//            public string ComplaintType { get; set; }
//            public string Reason { get; set; }
//            public string? ProofUrl { get; set; }
//        }
//    }
//}
