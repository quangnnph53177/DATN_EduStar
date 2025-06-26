using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionRepos _permissionRepos;
        public PermissionsController(IPermissionRepos permissionRepos)
        {
            _permissionRepos = permissionRepos;
        }
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePermission([FromBody] string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName))
            {
                return BadRequest("Permission name cannot be null or empty.");
            }
            try
            {
                var permission = await _permissionRepos.CreatePermissionAsync(permissionName);
                return Ok(permission);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionRepos.GetPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
