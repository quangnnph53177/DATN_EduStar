using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace API.Services.Repositories
{
    public class RegisterRepos : IRegisterService
    {
        private readonly AduDbcontext _context;
        public RegisterRepos(AduDbcontext context)
        {
            _context = context;
        }
        public async Task<List<string>> CreateBulk(List<RegisterViewModel> model)
        {
            var message = new List<string>();
            foreach (var item in model)
            {
                if (_context.Users.Any(u => u.UserName == item.UserName))
                {
                    message.Add($"tên đăng nhập { item.UserName} đã ồn tại");
                    continue;
                }
           
                var use = new User
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    PassWordHash = item.PassWordHash,
                    Email = item.Email,
                    PhoneNumber = item.PhoneNumber,
                    Statuss = true,
                    CreateAt = DateTime.Now,
                };
                _context.Users.Add(use);
                await _context.SaveChangesAsync();
                var uia = new UserProfile
                {
                    UserId = use.Id,
                    FullName = item.FullName,
                    Gender = item.Gender,
                    Avatar = item.Avatar,
                    Address = item.Address,
                    Dob = item.Dob,
                };
                _context.UserProfiles.Add(uia);
                await _context.SaveChangesAsync();
             
                    var st = new StudentsInfor
                    {
                        UserId = use.Id,
                        StudentsCode = item.StudentsCode,

                    };
                    _context.StudentsInfors.Add(st);
                }

                        await _context.SaveChangesAsync();
            return message;
        
        }

        public async Task<List<RegisterViewModel>> ExcelFile(IFormFile Ifile)
        {
            var users = new List<RegisterViewModel>();
            using var stream = new MemoryStream();
            await Ifile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var statusText = worksheet.Cells[row, 5].Text.Trim().ToLower();
                bool status = statusText == "active" || statusText == "true" || statusText == "1";

                var genderText = worksheet.Cells[row, 7].Text.Trim().ToLower();
                bool gender = genderText == "nam" || genderText == "male" || genderText == "1";
                users.Add(new RegisterViewModel
                {

                    UserName = worksheet.Cells[row, 1].Text,
                    PassWordHash = worksheet.Cells[row, 2].Text,
                    Email = worksheet.Cells[row, 3].Text,
                    PhoneNumber = worksheet.Cells[row, 4].Text,
                    Statuss =status,
                   FullName = worksheet.Cells[row, 6].Text,
                    Gender = gender,
                    Address = worksheet.Cells[row, 8].Text,
                    Dob = DateOnly.TryParse(worksheet.Cells[row, 9].Text, out var dob) ? dob : null,
                    StudentsCode = worksheet.Cells[row, 10].Text

                });
            }

            return users;
        }
    }
}
