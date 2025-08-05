using API.Data;
using API.Models;
using API.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace API.Services.Repositories
{
    public class UserRepos : IUserRepos
    {
        private readonly AduDbcontext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailRepos _emailService;
        private readonly ILogger<UserRepos> _logger;

        public UserRepos(AduDbcontext adu, IConfiguration configuration, IEmailRepos emailService, ILogger<UserRepos> logger)
        {
            _context = adu;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }
        private IQueryable<User> FilterUsersByRole(IQueryable<User> query, List<int> currentUserRoleIds, string? currentUserName)
        {
            if (currentUserRoleIds.Contains(1)) // Admin
            {
                return query.Where(u => u.Roles.Any(r => r.Id == 1 || r.Id == 2 || r.Id == 3 || u.UserName == currentUserName));
            }
            else if (currentUserRoleIds.Contains(2)) // Giảng viên
            {
                return query.Where(u => u.Roles.Any(r => r.Id == 3 || u.UserName == currentUserName));
            }
            else
            {
                return query.Where(u => u.UserName == currentUserName);
            }
        }
        private UserDTO MapToUserDTO(User u)
        {
            if (u == null)
                throw new Exception("Đối tượng người dùng đang bị null (không tồn tại trong hệ thống).");

            if (u.UserProfile == null)
                _logger.LogWarning($"Hồ sơ người dùng đang bị thiếu đối với tài khoản: {u.UserName}");

            if (u.Roles == null)
                _logger.LogWarning($"Danh sách vai trò chưa được tải hoặc chưa được gán cho tài khoản:{u.UserName}");
            return new UserDTO
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Statuss = u.Statuss ?? false,
                IsConfirm = u.IsConfirm ?? false,
                CreateAt = u.CreateAt,
                UserCode = u.UserProfile?.UserCode,
                FullName = u.UserProfile?.FullName ?? "",
                Gender = u.UserProfile?.Gender,
                Avatar = u.UserProfile?.Avatar,
                Address = u.UserProfile?.Address,
                Dob = u.UserProfile?.Dob.HasValue == true
                ? u.UserProfile.Dob.Value.ToDateTime(TimeOnly.MinValue)
                : null,
                RoleIds = u.Roles?.Select(r => r.Id).ToList() ?? new List<int>(),
                ClassName = u.StudentsInfor?.Classes?.Select(c => c.NameClass).ToList()
            };
        }
        private async Task<string> SaveAvatar(IFormFile imgFile, string? oldPath = null)
        {
            var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(imgFile.FileName).ToLowerInvariant();
            if (!validImageFormats.Contains(ext))
                _logger.LogError("Định dạng ảnh không hợp lệ");

            var avatarDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(avatarDir)) Directory.CreateDirectory(avatarDir);

            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                var fullOldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
                if (File.Exists(fullOldPath))
                    File.Delete(fullOldPath);
            }

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(avatarDir, uniqueName);
            await using var stream = new FileStream(savePath, FileMode.Create);
            await imgFile.CopyToAsync(stream);

            return $"/images/avatars/{uniqueName}";

        }
        private async Task<string> GenerateUserCode(List<Role> roles)
        {
            if (roles.Any(r => r.Id == 1)) // Admin
            {
                int count = await _context.Users.CountAsync(u => u.Roles.Any(r => r.Id == 1));
                return $"AD{(count + 1):D5}";
            }
            if (roles.Any(r => r.Id == 2)) // Giảng viên
            {
                int count = await _context.Users.CountAsync(u => u.Roles.Any(r => r.Id == 2));
                return $"GV{(count + 1):D5}";
            }
            if (roles.Any(r => r.Id == 3)) // Sinh viên
            {
                int count = await _context.Users.CountAsync(u => u.Roles.Any(r => r.Id == 3));
                return $"SV{(count + 1):D5}";
            }
            return $"US{Guid.NewGuid().ToString()[..5].ToUpper()}";
        }

        public async Task<User> Register(UserRegisterDTO usd, IFormFile? imgFile)
        {
            // Kiểm tra tài khoản đã tồn tại chưa  
            if (await _context.Users.AnyAsync(u => u.UserName == usd.UserName))
                throw new Exception("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == usd.Email))
                throw new Exception("Email đã được sử dụng.");
            var passWordHash = PasswordHasher.HashPassword(usd.Password);

            var roleIds = (usd.RoleIds == null || !usd.RoleIds.Any()) ? new List<int> { 3 } : usd.RoleIds;
            var roles = await _context.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
            if (!roles.Any())
                throw new Exception("Không tìm thấy role hợp lệ để gán.");

            string? avatarPath = null;
            if (imgFile != null && imgFile.Length > 0)
            {
                avatarPath = await SaveAvatar(imgFile);
            }
            string userCode = await GenerateUserCode(roles);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = usd.UserName,
                PassWordHash = passWordHash,
                Email = usd.Email,
                PhoneNumber = usd.PhoneNumber,
                Roles = roles,
                Statuss = false,
                IsConfirm = false,
                CreateAt = DateTime.Now

            };
            _context.Users.Add(user);
            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FullName = usd.FullName,
                UserCode = userCode,
                Gender = usd.Gender,
                Dob = usd.Dob.HasValue ? DateOnly.FromDateTime(usd.Dob.Value) : null,
                Avatar = avatarPath,
                Address = usd.Address
            });

            if (roles.Any(r => r.Id == 3))
            {
                _context.StudentsInfors.Add(new StudentsInfor
                {
                    UserId = user.Id,
                    StudentsCode = userCode,
                });
            }

            // Gửi email xác nhận  
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email));
            var confirmationLink = $"https://localhost:7298/api/User/confirm?token={HttpUtility.UrlEncode(token)}";
            string message = $@"
                <!DOCTYPE html>
                <html lang='vi'>
                <head>
                    <meta charset='UTF-8'>
                    <title>Xác nhận email</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f8f9fa;
                            margin: 0;
                            padding: 0;
                        }}
                        .email-container {{
                            max-width: 600px;
                            margin: 40px auto;
                            background-color: #ffffff;
                            padding: 30px;
                            border-radius: 10px;
                            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                        }}
                        h2 {{
                            color: #333;
                        }}
                        p {{
                            color: #555;
                            font-size: 15px;
                        }}
                        .info-table {{
                            width: 100%;
                            margin: 20px 0;
                            border-collapse: collapse;
                        }}
                        .info-table td {{
                            padding: 8px 10px;
                            border: 1px solid #ddd;
                        }}
                        .info-table td.label {{
                            font-weight: bold;
                            background-color: #f1f1f1;
                            width: 150px;
                        }}
                        .btn {{
                            display: inline-block;
                            margin-top: 20px;
                            padding: 12px 24px;
                            font-size: 16px;
                            color: #ffffff;
                            background-color: #007bff;
                            text-decoration: none;
                            border-radius: 6px;
                        }}
                        .btn:hover {{
                            background-color: #0056b3;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 12px;
                            color: #888;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <h2>🎉 Yêu cầu xác nhận tài khoản để đăng nhập</h2>
                        <p>Chúng tôi đã tạo tài khoản cho bạn trên hệ thống. Dưới đây là thông tin đăng nhập:</p>

                        <table class='info-table'>
                            <tr>
                                <td class='label'>Tên đăng nhập:</td>
                                <td>{user.UserName}</td>
                            </tr>
                            <tr>
                                <td class='label'>Mật khẩu:</td>
                                <td>{usd.Password}</td>
                            </tr>
 <tr>
                                <td class='label'>Email:</td>
                                <td>{usd.Email}</td>
                            </tr>
                        </table>

                        <p>👉 Vui lòng nhấn vào nút bên dưới để xác nhận email và kích hoạt tài khoản:</p>
                        <a href='{confirmationLink}' class='btn'>Xác nhận tài khoản</a>

                        <p class='footer'>Nếu bạn không yêu cầu hành động này, vui lòng bỏ qua email này.</p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmail(user.Email, "Xác nhận email", message);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<string> ConfirmEmail(string token)
        {
            var decodedToken = HttpUtility.UrlDecode(token);
            var email = Encoding.UTF8.GetString(Convert.FromBase64String(decodedToken));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return "Không tìm thấy tài khoản.";

            if (user.IsConfirm == true)
                return "Tài khoản đã được xác nhận trước đó.";

            user.IsConfirm = true;
            user.Statuss = true;
            await _context.SaveChangesAsync();

            return "Xác nhận email thành công. Tài khoản đã được mở khóa.";
        }
        public async Task<List<User>> CleanupUnconfirmedUsers()
        {
            var now = DateTime.Now;

            // 1. Lấy danh sách user unlock (trong quá khứ)
            var unlockedUserMap = await _context.Auditlogs
                .Where(a => a.Active == "Unlock")
                .GroupBy(a => a.Userid)
                .Select(g => new
                {
                    UserId = g.Key,
                    UnlockAfterCreate = g.Min(x => x.Timestamp)
                })
                .ToListAsync();

            // 2. Lấy danh sách user chưa xác nhận, chưa xóa, quá hạn 2 phút
            var allExpired = await _context.Users
                .Where(u =>
                    u.Statuss == false &&
                    EF.Functions.DateDiffMinute(u.CreateAt, now) >= 2 &&
                    (u.IsDeleted == null || u.IsDeleted == false))
                .ToListAsync();

            // 3. Lọc bỏ user nào đã được unlock sau khi tạo
            var expiredUsers = allExpired
                .Where(u =>
                    !unlockedUserMap.Any(un =>
                        un.UserId == u.Id && un.UnlockAfterCreate >= u.CreateAt))
                .ToList();

            if (expiredUsers.Any())
            {
                var userIds = expiredUsers.Select(u => u.Id).ToList();

                var profiles = await _context.UserProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync();
                _context.UserProfiles.RemoveRange(profiles);

                var students = await _context.StudentsInfors.Where(s => userIds.Contains(s.UserId)).ToListAsync();
                _context.StudentsInfors.RemoveRange(students);

                // ❗️XÓA MỀM user — KHÔNG dùng RemoveRange
                foreach (var user in expiredUsers)
                {
                    user.IsDeleted = true;

                    // Gắn thêm hậu tố vào Email & Username để tránh trùng nếu tạo lại
                    user.Email += $"_deleted_{Guid.NewGuid():N}@deleted.local";
                    user.UserName += "_deleted";
                }
                await _context.SaveChangesAsync();

            }
            return expiredUsers;
        }

        public async Task<LoginResult> Login(string userName, string password)
        {

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không được để trống.");

            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            //if (user.IsDeleted == true)
            //    throw new Exception("Tài khoản đã bị xóa.");
            if (user == null)
                throw new Exception("Tên đăng nhập không tồn tại.");

            // Kiểm tra trạng thái tài khoản
            if (user.Statuss != true)
                throw new Exception("Tài khoản đang bị khóa hoặc không hoạt động.");

            bool isPasswordValid = PasswordHasher.Verify(password, user.PassWordHash);
            if (!isPasswordValid)
                throw new Exception("Mật khẩu không chính xác.");

            var permissions = user.Roles?
                .SelectMany(r => r.Permissions)
                .Where(p => p != null)
                .Select(p => p.PermissionName)
                .Distinct()
                .ToList() ?? new List<string>();

            string token = GenerateJwtToken(user, permissions);

            // Fix for CS0029: Convert RoleName to a List<string>
            var roleNames = user.Roles.Select(r => r.RoleName).ToList();

            return new LoginResult
            {
                Token = token,
                RoleId = user.Roles.Select(r => r.Id).ToList(),
                RoleName = roleNames, // Updated to return a List<string>
                UserName = user.UserName,
                Permission = permissions
            };
        }
        private string GenerateJwtToken(User user, List<string> permissions)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roleIds = user.Roles.Select(r => r.Id.ToString());
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                 new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            claims.AddRange(user.Roles.Select(r => new Claim("RoleName", r.RoleName) /* Tên vai trò*/ ));
            // Thêm RoleId (sử dụng roleId để kiểm tra quyền nhanh gọn)
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Id.ToString())));

            // Thêm Permission
            claims.AddRange(permissions.Select(p => new Claim("Permission", p)));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = credentials,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsers(List<int> currentUserRoleIds, string? currentUserName, bool excludeTeacher = false)
        {
            var query = _context.Users
                .Where(u => u.IsDeleted == false)
                 .Include(u => u.UserProfile)
                 .Include(u => u.Roles)
                 .Include(u => u.StudentsInfor)
                 .ThenInclude(s => s.Classes)
                 .AsSplitQuery();
;

            query = FilterUsersByRole(query, currentUserRoleIds, currentUserName);

            if (excludeTeacher)
            {
                query = query.Where(u => !u.Roles.Any(r => r.Id == 2)); // loại giảng viên
            }

            var users = await query.ToListAsync();

            return users
                .Select(u => new { User = u, MainRole = u.Roles.Min(r => r.Id) })
                .OrderByDescending(u => u.User.Statuss ?? false  )
                .ThenBy(u => u.User?.UserProfile?.FullName)
                .Select(u => MapToUserDTO(u.User));
        }
        public async Task<string> LockUser(string userName, Guid currentUserId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                return "Không tìm thấy người dùng với tên đăng nhập đã cho.";
            if (user.Id == currentUserId)
                return "Không thể khóa hoặc mở khóa chính tài khoản của bạn.";
            user.Statuss = !(user.Statuss ?? false); // Đảo trạng thái
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user.Statuss == true ? "Mở khóa thành công" : "Khóa thành công";

        }
        public async Task UpdateUser(UserDTO userDto, IFormFile? imgFile)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == userDto.UserName.ToLower());

            if (user == null)
                throw new Exception("Không tìm thấy người dùng.");

            if (await _context.Users.AnyAsync(u => u.UserName == userDto.UserName && u.Id != user.Id))
                throw new Exception("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email && u.Id != user.Id))
                throw new Exception("Email đã được sử dụng.");

            if (!string.IsNullOrWhiteSpace(userDto.UserCode))
            {
                bool userCodeExists = await _context.UserProfiles
                    .AnyAsync(up => up.UserCode == userDto.UserCode && up.UserId != user.Id);
                if (userCodeExists)
                    throw new Exception("UserCode đã tồn tại.");
            }

            user.Email = userDto.Email;
            user.PhoneNumber = userDto.PhoneNumber;

            if (user.UserProfile == null)
                user.UserProfile = new UserProfile { UserId = user.Id };

            user.UserProfile.FullName = userDto.FullName;
            user.UserProfile.UserCode = userDto.UserCode;
            user.UserProfile.Gender = userDto.Gender;
            user.UserProfile.Address = userDto.Address;
            user.UserProfile.Dob = userDto.Dob.HasValue ? DateOnly.FromDateTime(userDto.Dob.Value) : null;

            if (imgFile != null && imgFile.Length > 0)
            {
                var validExts = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(imgFile.FileName).ToLowerInvariant();
                if (!validExts.Contains(ext)) throw new Exception("Chỉ hỗ trợ ảnh jpg, jpeg, png");

                var avatarPath = await SaveAvatar(imgFile, user.UserProfile.Avatar);
                user.UserProfile.Avatar = avatarPath;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task<string> ChangeRole(string userName, int newRoleId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.StudentsInfor)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
                return "Không tìm thấy người dùng với tên đăng nhập đã cho.";

            var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == newRoleId);
            if (newRole == null)
                return "Vai trò muốn chuyển không tồn tại.";

            // Xác định vai trò hiện tại
            var currentRoleId = user.Roles.Min(r => r.Id);

            // Gán role mới (chỉ giữ 1 vai trò)
            user.Roles = new List<Role> { newRole };

            if (currentRoleId == 3 && (newRoleId == 1 || newRoleId == 2)) // Sinh viên → Admin/Giảng viên
            {
                if (user.StudentsInfor != null)
                    _context.StudentsInfors.Remove(user.StudentsInfor);

                if (user.UserProfile == null)
                {
                    user.UserProfile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = "Tên người dùng",
                        Gender = false,
                        Address = "Chưa cập nhật",
                        Avatar = "default.png",
                        Dob = DateOnly.FromDateTime(new DateTime(1990, 1, 1))
                    };
                }
            }
            else if ((currentRoleId == 1 || currentRoleId == 2) && newRoleId == 3) // Admin/Giảng viên → Sinh viên
            {
                if (user.UserProfile != null)
                    _context.UserProfiles.Remove(user.UserProfile);

                if (user.StudentsInfor == null)
                {
                    user.StudentsInfor = new StudentsInfor
                    {
                        UserId = user.Id,
                        StudentsCode = "SV" + DateTime.Now.Ticks.ToString().Substring(10)
                    };
                }
            }
            else
            {                
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return $"✅ Đã đổi vai trò thành công sang: {newRole.RoleName}";
        }
        public async Task ForgetPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Email không tồn tại. Hãy sử dụng email khác.");
            // Tạo token xác nhận email
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email));
            var forgetpasswordLink = $"https://localhost:7298/api/User/forget-password?token={HttpUtility.UrlEncode(token)}";

            string message = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <title>Xác nhận email</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            background-color: #ffffff;
                            margin: 50px auto;
                            padding: 30px;
                            border-radius: 8px;
                            max-width: 600px;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                            text-align: center;
                        }}
                        .btn {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin-top: 20px;
                            font-size: 16px;
                            color: #fff;
                            background-color: #007bff;
                            text-decoration: none;
                            border-radius: 5px;
                        }}
                        .btn:hover {{
                            background-color: #0056b3;
                        }}
                        p {{
                            font-size: 16px;
                            color: #333;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Yêu cầu đặt lại mật khẩu</h2>
                        <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                        <p>Vui lòng nhấn vào nút bên dưới để tiến hành:</p>
                        <a href='{forgetpasswordLink}' class='btn'>Đặt lại mật khẩu</a>
                        <p>Nếu bạn không yêu cầu hành động này, bạn có thể bỏ qua email này.</p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmail(user.Email, "Đặt lại mật khẩu", message);

        }
        public async Task<string> ResetPassword(string token, string newPassword)
        {
            var decodedToken = HttpUtility.UrlDecode(token);
            var email = Encoding.UTF8.GetString(Convert.FromBase64String(decodedToken));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return "Email không tồn tại.";
            // Kiểm tra mật khẩu mới
            if (string.IsNullOrWhiteSpace(newPassword))
                return"Mật khẩu mới không được để trống.";
            // Hash mật khẩu mới
            user.PassWordHash = PasswordHasher.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return "Đặt lại mật khẩu thành công.";
        }
        //Lấy danh sách sinh viên có trong lớp của giảng viên
        //public async Task<TeacherWithClassesViewModel> GetStudentByTeacher(Guid? teacherId)
        //{
        //    // 🔎 Truy vấn tên giảng viên
        //    var teacher = await _context.Users
        //        .Include(u => u.UserProfile)
        //        .FirstOrDefaultAsync(u => u.Id == teacherId);

        //    if (teacher == null)
        //        throw new Exception("Không tìm thấy giảng viên.");
        //    var teacherName = teacher.UserProfile?.FullName ?? "Không rõ";
        //    // Lấy các lớp của giảng viên  
        //    var classList = await _context.Classes
        //        .Where(c => c.UsersId == teacherId)
        //        .Include(c => c.Students)
        //            .ThenInclude(s => s.User)
        //                .ThenInclude(u => u.UserProfile)
        //        .Include(c => c.Students)
        //            .ThenInclude(s => s.User)
        //                .ThenInclude(u => u.Roles)
        //        .ToListAsync();

        //    var result = new TeacherWithClassesViewModel
        //    {
        //        TeacherId = teacher.Id,
        //        TeacherName = teacher.UserProfile?.FullName ?? "Không rõ",
        //        Classes = classList.Select(c => new ClassWithStudentsViewModel
        //        {
        //            ClassId = c.Id,
        //            ClassName = c.NameClass,
        //            StudentsInfor = c.Students
        //                .Where(s => s.UserId != null && s.User != null)
        //                .Select(s => MapToUserDTO(s.User))
        //                .ToList()
        //        }).ToList()
        //    };

        //    return result;
        //}
        public static class PasswordHasher
        {
            private const int SaltSize = 16; // 128-bit salt
            private const int Iterations = 100000; // Số lần lặp (nên >= 100,000)
            private const int HashSize = 32; // 256-bit hash

            public static string HashPassword(string password)
            {
                // Tạo salt ngẫu nhiên
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

                // Tạo hash với PBKDF2
                var pbkdf2 = new Rfc2898DeriveBytes(
                    password: password,
                    salt: salt,
                    iterations: Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256
                );
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Kết hợp salt + hash
                byte[] hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                return Convert.ToBase64String(hashBytes);
            }

            public static bool Verify(string password, string hashedPassword)
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                var pbkdf2 = new Rfc2898DeriveBytes(
                    password: password,
                    salt: salt,
                    iterations: Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256
                );
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // So sánh hash
                for (int i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i + SaltSize] != hash[i])
                        return false;
                }
                return true;
            }
        }
    }
}