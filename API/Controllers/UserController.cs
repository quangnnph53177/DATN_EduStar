using API.Services;
using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepos _userRepos;
        public UserController(IUserRepos userRepos)
        {
            _userRepos = userRepos;
        }
        [Authorize(Policy = "CreateUS")]//Nếu chưa có tài khoản thì commit cái này lại để tạo tài khoản để đăng nhập
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDto)
        {
            if (userDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ");

            try
            {
                var user = await _userRepos.Register(userDto);
                return Ok(new { message = "Đăng ký thành công", userId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (loginDto == null || !ModelState.IsValid)
                return BadRequest("Dữ liệu đăng nhập không hợp lệ.");

            try
            {
                var token = await _userRepos.Login(loginDto.UserName, loginDto.Password);
                return Ok(new { message = "Đăng nhập thành công", token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "CreateUS")]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được tải lên.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null)
                    return BadRequest("File Excel không có worksheet nào.");

                int rowCount = worksheet.Dimension.Rows;
                var usersCreated = new List<string>();

                for (int row = 2; row <= rowCount; row++) // Bắt đầu từ row 2 vì row 1 là header
                {
                    // Đọc từng cột 
                    var fullname = worksheet.Cells[row, 2].Text.Trim();
                    var userName = worksheet.Cells[row, 3].Text.Trim();
                    var password = worksheet.Cells[row, 4].Text.Trim();
                    var phone = worksheet.Cells[row, 5].Text.Trim();
                    var dobText = worksheet.Cells[row, 6].Text.Trim();
                    var genderText = worksheet.Cells[row, 7].Text.Trim();
                    var email = worksheet.Cells[row, 8].Text.Trim();

                    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                        continue;

                    DateTime? dob = null;
                    if (DateTime.TryParseExact(dobText, new[] { "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
                    {
                        dob = parsedDob;
                    }
                    bool? gender = null;
                    if (!string.IsNullOrEmpty(genderText))
                    {
                        genderText = genderText.ToLower();
                        if (genderText == "nam")
                            gender = true;
                        else if (genderText == "nữ" || genderText == "nu")
                            gender = false;
                    }
                    var userDto = new UserDTO
                    {
                        UserName = userName,
                        PassWordHash = password,
                        PhoneNumber = phone,
                        Email = email,
                        FullName = fullname,
                        Dob = dob,
                        Gender = gender,
                        Statuss = true,
                        CreateAt = DateTime.Now
                    };
                    await _userRepos.Register(userDto);
                    usersCreated.Add(userName);
                }

                return Ok(new { message = "Upload và tạo user thành công", users = usersCreated });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Lỗi khi xử lý file Excel: " + ex.Message });
            }
        }

        [Authorize(Policy = "DetailUS")]
        [HttpGet("user")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userRepos.GetAllUsers();
                
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Policy = "DetailUS")]
        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserByName(string username)
        {
            try
            {
                return Ok(await _userRepos.GetUserByName(username));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [Authorize(Policy = "EditUS")]
        [HttpPut("{username}")]
        public async Task<IActionResult> UpdateUser(string username, [FromBody] UserDTO userDto)
        {
            if (username != userDto.UserName || !ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ hoặc ID không khớp.");
            try
            {
                await _userRepos.UpdateUser(userDto);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
