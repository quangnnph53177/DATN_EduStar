using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudent _service;
        public StudentsController(IStudent service)
        {
             _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var itemindex = await _service.GetAllStudents();
            if (itemindex == null) return null;
            var addst = itemindex.Select(u => new StudentViewModels
            {
                id=u.UserId,
                FullName= u.User.UserProfile.FullName,
                UserName = u.User.UserName,
                Email = u.User.Email,
                PhoneNumber = u.User.PhoneNumber,
                StudentCode = u.StudentsCode,
                Gender = u.User.UserProfile.Gender,
                Avatar =  u.User.UserProfile.Avatar,
                Address = u.User.UserProfile.Address,
                Dob = u.User.UserProfile.Dob,
                Status = u.User.Statuss.GetValueOrDefault(),
            });
            return Ok(addst);
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> Details(Guid Id)
        {
            return Ok(await _service.GetById(Id));
        }
        [HttpPut("{Id}/boss")]
        public async Task<IActionResult> UpdateWithBoss(Guid id, [FromBody] StudentViewModels model)
        {
            if (id != model.id)
            {
                return BadRequest("ID không khớp.");
            }

            try
            {
                await _service.UpdatebyBoss(model);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }
        }
        [HttpPut("{Id}/beast")]
        public async Task<IActionResult> UpdateWithbeast(Guid Id, [FromBody] StudentViewModels model)
        {
            if (Id != model.id)
            {
                return BadRequest("Id không khớp");
            }
            try
            {
                await _service.UpdateByBeast(model);
                return Ok(new { message = "Cập nhập thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhập :{ex.Message}" });
            }
        }
        [HttpPut("{id}/toggle-active")]
        public async Task<IActionResult> OpenandLock(Guid id)
        {
            var result = await _service.KhoaMoSinhVienAsync(id); 
            if (!result) return NotFound("Không tìm thấy sinh viên.");
            return Ok("Cập nhật trạng thái thành công.");
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid Id)
        {


            return Ok(await _service.DeleteStudent(Id));
           
        }
        [HttpGet("search")]
        public async Task<IActionResult> search(string Studencode, string fullName, string username, string email)
        {
            var result =await _service.Search(Studencode, fullName, username, email);
            return Ok(result);
        }

    }
}
