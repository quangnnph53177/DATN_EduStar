using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.ViewModel;
using API.Services; 

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly IClassRepos _classRepos; 
        public ClassesController(IClassRepos classRepos) 
        {
            _classRepos = classRepos;
        }

        // GET: api/Classes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClassViewModel>>> GetClasses()
        {
            try
            {
                var classes = await _classRepos.GetAllClassesAsync();
                return Ok(classes); // Trả về danh sách ClassViewModel
            }
            catch (InvalidOperationException ex)
            {
                // Xử lý lỗi từ repository
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        // GET: api/Classes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassViewModel>> GetClass(int id)
        {
            try
            {
                var classViewModel = await _classRepos.GetClassByIdAsync(id);
                if (classViewModel == null)
                {
                    return NotFound($"Class with ID {id} not found.");
                }
                return Ok(classViewModel);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        // PUT: api/Classes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClass(int id, ClassViewModel classViewModel)
        {
            if (classViewModel == null)
            {
                return BadRequest("Class data cannot be null.");
            }

            try
            {
                // Gọi phương thức Update từ repository
                await _classRepos.UpdateClassAsync(id, classViewModel);
                return NoContent(); // Trả về 204 No Content cho PUT thành công
            }
            catch (ArgumentException ex) // Lỗi validate từ repository
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex) // Lỗi không tìm thấy ID từ repository
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex) // Lỗi logic nghiệp vụ từ repository (trùng lặp, v.v.)
            {
                return Conflict(ex.Message); // Sử dụng Conflict (409) cho lỗi trùng lặp
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred while updating the class: {ex.Message}");
            }
        }

        // POST: api/Classes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ClassViewModel>> PostClass(ClassViewModel classViewModel)
        {
            if (classViewModel == null)
            {
                return BadRequest("Class data cannot be null.");
            }

            try
            {
                // Gọi phương thức Add từ repository
                await _classRepos.AddClassAsync(classViewModel);
                return Created(); // Trả về 201 Created (thành công nhưng không có URL cụ thể của resource mới)
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred while creating the class: {ex.Message}");
            }
        }

        // DELETE: api/Classes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                // Gọi phương thức Delete từ repository
                await _classRepos.DeleteClassAsync(id);
                return NoContent(); // Trả về 204 No Content cho DELETE thành công
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message); // Hoặc BadRequest tùy loại lỗi ràng buộc
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred while deleting the class: {ex.Message}");
            }
        }
    }
}