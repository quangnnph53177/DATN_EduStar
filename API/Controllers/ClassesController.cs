using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Models;
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
        public async Task<ActionResult<IEnumerable<Class>>> GetClasses()
        {
            return Ok(await _classRepos.GetAllClassesAsync());
        }

        // GET: api/Classes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Class>> GetClass(int id)
        {
            await _classRepos.GetClassByIdAsync(id);
            var @class = await _classRepos.GetClassByIdAsync(id);
            if (@class == null)
            {
                return NotFound();
            }
            return Ok(@class);
        }

        // PUT: api/Classes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClass(int id, Class @class)
        {
            await _classRepos.UpdateClassAsync(id, @class);
            if (id != @class.Id)
            {
                return BadRequest("Class ID mismatch.");
            }
            try
            {
                await _classRepos.UpdateClassAsync(id, @class);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _classRepos.GetClassByIdAsync(id) == null)
                {
                    return NotFound("Class not found.");
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // POST: api/Classes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Class>> PostClass(Class @class)
        {
            await _classRepos.CreateClassAsync(@class);
            if (@class == null)
            {
                return BadRequest("Class data cannot be null.");
            }
            try
            {
                await _classRepos.CreateClassAsync(@class);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating class: {ex.Message}");
            }
            return CreatedAtAction("GetClass", new { id = @class.Id }, @class);
        }

        // DELETE: api/Classes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            await _classRepos.DeleteClassAsync(id);
            return Ok("Class deleted successfully.");
        }
    }
}
