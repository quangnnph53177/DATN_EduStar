using API.Models;
using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectController : ControllerBase
    {
        private readonly ISubject _service;
        public SubjectController(ISubject service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> Get(string? subjectName, int? numberofCredit, string? subcode, bool? status,int? semesterId)
        {
            if(string.IsNullOrEmpty(subjectName)&&
                string.IsNullOrEmpty(subcode)&&
                numberofCredit==null&&
                status==null) 
            {
                var re = await _service.Getall();
                return Ok(re);
            }
            var result = await _service.Search(subjectName, numberofCredit, subcode, status,semesterId);
            if (result == null || !result.Any())
            {
                return NotFound(new { message = "Không tìm thấy môn phù hợp." });
            }

            return Ok(result);
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetId(int Id)
        {
            return Ok(await _service.GetById(Id));
        }
        [HttpPost]
        public async Task<IActionResult> Create(SubjectViewModel sub)
        {
            return Ok(await _service.CreateSubject(sub));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SubjectViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest("Id không khớp giữa route và body.");
            }

            try
            {
                var success = await _service.UpdateSubject(model);

                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy môn học cần cập nhật." });
                }

                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }
        }

        //[HttpGet("search")]
        //public async Task<IActionResult> Search(string? subjectname, int? numberofcredot, string? subcode, bool? status)
        //{
        //    var result = await _service.Search(subjectname, numberofcredot, subcode, status);
        //    return Ok(result);
        //}
        [HttpPut("{id}/Lock")]
        public async Task<IActionResult> Lock(int id)
        {
            return Ok(await _service.OpenAndClose(id));
        }
        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var item =await _service.GetById(Id);
            var result =await _service.DeleteSubject(Id);
            if (!result)
            {
                if (item == null)
                    return NotFound(new { message = "Không tìm thấy môn học." });

                if (item.Status == true)
                    return BadRequest(new { message = "Không thể xóa vì môn học đang hoạt động." });

       
                return BadRequest(new { message = "Không thể xóa môn học vì lý do không xác định." });
            }

            return Ok(new { message = "Xóa môn học thành công." });
            
        }
    }
}
