using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepos _roleRepos;
        public RoleController(IRoleRepos roleRepos)
        {
            _roleRepos = roleRepos;
        }
        [HttpPost("assign-permissions")]
        [Authorize(Policy = "CreateUS")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionDTO dto)
        {
            if (dto == null || dto.PermissionIds == null || dto.PermissionIds.Count == 0)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }
            try
            {
                await _roleRepos.AssignPermissionsAsync(dto.RoleId, dto.PermissionIds);
                return Ok("Gán quyền thành công.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost("create-role")]
        [Authorize(Policy = "CreateUS")]
        public async Task<Role> CreateRoles([FromBody] RoleDTO role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role), "Vai trò không được null");
            }
            return await _roleRepos.CreateRole(role);
        }
        [HttpGet("getroles")]
        [Authorize(Policy = "DetailUS")]
        public async Task<List<Role>> GetAllRoles()
        {
            return await _roleRepos.GetAllRoles();
        }
    }
}
