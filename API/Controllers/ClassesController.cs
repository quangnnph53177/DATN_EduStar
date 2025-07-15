
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassRepos _classRepos;

        public ClassController(IClassRepos classRepos)
        {
            _classRepos = classRepos;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ClassDetailViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllClasses()
        {
            try
            {
                var classes = await _classRepos.GetAllClassesAsync();
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClassDetailViewModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClassById(int id)
        {
            try
            {
                var classDetail = await _classRepos.GetClassByIdAsync(id);
                if (classDetail == null)
                {
                    return NotFound($"Class with ID {id} not found.");
                }
                return Ok(classDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClass([FromBody] ClassCreateViewModel classViewModel)
        {
            try
            {
                if (classViewModel == null || !ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _classRepos.AddClassAsync(classViewModel);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] ClassUpdateViewModel classViewModel)
        {
            try
            {
                if (classViewModel == null || !ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                try
                {
                    await _classRepos.UpdateClassAsync(id, classViewModel);
                    return NoContent();
                }
                catch (KeyNotFoundException ex)
                {
                    return NotFound(ex.Message);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                await _classRepos.DeleteClassAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{classId}/assignStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignStudentToClass([FromBody] AssignStudentsRequest request )
        {
            try
            {
                var success = await _classRepos.AssignStudentToClassAsync(request);
                if (success)
                {
                    return Ok(new { success = true, message = "Gán sinh viên vào lớp thành công." });
                }

                return Conflict("Một số sinh viên đã được gán hoặc lớp/sinh viên không tồn tại.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Lỗi máy chủ: {ex.Message}");
            }
        }

        [HttpDelete("{classId}/removeStudent/{studentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveStudentFromClass(int classId, Guid studentId)
        {
            try
            {
                var success = await _classRepos.RemoveStudentFromClassAsync(classId, studentId);
                if (success)
                {
                    return Ok(true);
                }
                return NotFound("The student is not enrolled in this class.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{classId}/history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ClassChangeViewModel>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClassHistory(int classId)
        {
            try
            {
                var history = await _classRepos.GetClassHistoryAsync(classId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }
    }
}