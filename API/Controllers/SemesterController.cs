using API.Data;
using API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SemesterController : ControllerBase
    {
        private readonly AduDbcontext _context;

        public SemesterController(AduDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var semesters = await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync();
            return Ok(semesters);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Semester semester)
        {
            semester.IsActive = false; // Mặc định không active
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
            return Ok(semester);
        }

        [HttpPut("activate/{id}")]
        public async Task<IActionResult> Activate(int id)
        {
            var currentActive = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (currentActive != null)
                currentActive.IsActive = false;

            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
                return NotFound("Không tìm thấy học kỳ.");

            semester.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok("Đã kích hoạt học kỳ.");
        }
    }
}
