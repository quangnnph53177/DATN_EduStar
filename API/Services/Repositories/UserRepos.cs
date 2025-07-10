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

        public UserRepos(AduDbcontext adu, IConfiguration configuration, IEmailRepos emailService)
        {
            _context = adu;
            _configuration = configuration;
            _emailService = emailService;
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
            return new UserDTO
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Statuss = u.Statuss ?? false,
                CreateAt = u.CreateAt,
                UserCode = u.UserProfile?.UserCode,
                FullName = u.UserProfile?.FullName,
                Gender = u.UserProfile?.Gender,
                Avatar = u.UserProfile?.Avatar,
                Address = u.UserProfile?.Address,
                Dob = u.UserProfile.Dob.HasValue ? u.UserProfile.Dob.Value.ToDateTime(TimeOnly.MinValue) : null,
                RoleIds = u.Roles.Select(r => r.Id).ToList()
            };
        }
        private async Task<string> SaveAvatar(IFormFile imgFile, string? oldPath = null)
        {
            var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(imgFile.FileName).ToLowerInvariant();
            if (!validImageFormats.Contains(ext)) throw new ArgumentException("Định dạng ảnh không hợp lệ");

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

        public async Task<User> Register(UserDTO usd, IFormFile? imgFile)
        {
            // Kiểm tra thông tin đầu vào  
            if (string.IsNullOrWhiteSpace(usd.UserName) || string.IsNullOrWhiteSpace(usd.PassWordHash) || string.IsNullOrWhiteSpace(usd.Email))
            {
                throw new Exception("Thiếu thông tin bắt buộc.");
            }

            // Kiểm tra tài khoản đã tồn tại chưa  
            if (await _context.Users.AnyAsync(u => u.UserName == usd.UserName))
                throw new Exception("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == usd.Email))
                throw new Exception("Email đã được sử dụng.");

            var hashedPassword = PasswordHasher.HashPassword(usd.PassWordHash);

            var roleIds = (usd.RoleIds == null || !usd.RoleIds.Any()) ? new List<int> { 3 } : usd.RoleIds;
            var roles = await _context.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
            if (!roles.Any())
                throw new Exception("Không tìm thấy role hợp lệ để gán.");

            if (imgFile != null && imgFile.Length > 0)
            {
                usd.Avatar = await SaveAvatar(imgFile);
            }
            if (usd.Dob.HasValue && usd.Dob.Value.Date >= DateTime.Now.Date)
            {
                throw new Exception("Ngày sinh không hợp lệ. Hãy nhập lại ngày sinh.");
            }
            string userCode = await GenerateUserCode(roles);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = usd.UserName,
                PassWordHash = hashedPassword,
                Email = usd.Email,
                PhoneNumber = usd.PhoneNumber,
                Roles = roles,
                Statuss = false,
                CreateAt = DateTime.Now
            };

            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FullName = usd.FullName,
                UserCode = userCode,
                Gender = usd.Gender,
                Dob = usd.Dob.HasValue ? DateOnly.FromDateTime(usd.Dob.Value) : null,
                Avatar = usd.Avatar,
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

            string message = $"Vui lòng xác nhận email bằng cách <a href='{confirmationLink}'>nhấn vào đây.</a>";
            await _emailService.SendEmail(user.Email, "Xác nhận email", message);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<bool> ConfirmEmail(string token)
        {
            var decodedToken = HttpUtility.UrlDecode(token);
            var email = Encoding.UTF8.GetString(Convert.FromBase64String(decodedToken));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return false;

            if (!user.Statuss.HasValue || user.Statuss == false)
            {
                user.Statuss = true;
                await _context.SaveChangesAsync();
            }
            return true;
        }
        public async Task CleanupUnconfirmedUsers()
        {
            var now = DateTime.Now;

            var expiredUsers = await _context.Users
                .Where(u => u.Statuss == false && EF.Functions.DateDiffDay(u.CreateAt, now) >= 7)
                .ToListAsync();

            if (expiredUsers.Any())
            {
                var userIds = expiredUsers.Select(u => u.Id).ToList();

                var profiles = await _context.UserProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync();
                _context.UserProfiles.RemoveRange(profiles);

                var students = await _context.StudentsInfors.Where(s => userIds.Contains(s.UserId)).ToListAsync();
                _context.StudentsInfors.RemoveRange(students);

                _context.Users.RemoveRange(expiredUsers);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<LoginResult> Login(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không được để trống.");

            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.UserName == userName);

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
         .Include(u => u.UserProfile)
         .Include(u => u.Roles)
         .AsSplitQuery();

            query = FilterUsersByRole(query, currentUserRoleIds, currentUserName);

            if (excludeTeacher)
            {
                query = query.Where(u => !u.Roles.Any(r => r.Id == 2)); // loại giảng viên
            }

            var users = await query.OrderByDescending(u => u.Statuss).ToListAsync();

            return users
                .Select(u => new { User = u, MainRole = u.Roles.Min(r => r.Id) })
                .OrderByDescending(u => u.User.Statuss ?? false)
                .ThenBy(u => u.MainRole)
                .Select(u => MapToUserDTO(u.User));
        }
        public async Task<string> LockUser(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng với tên đăng nhập đã cho.");
            user.Statuss = !(user.Statuss ?? false); // Đảo trạng thái
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user.Statuss == true ? "Mở khóa thành công" : "Khóa thành công";
        }

        public async Task UpdateUser(UserDTO userd, IFormFile imgFile)
        {
            var upuser = await _context.Users
                  .Include(p => p.UserProfile)
                  .FirstOrDefaultAsync(d => d.UserName.ToLower() == userd.UserName.ToLower());

            if (upuser == null)
                throw new Exception("Không tìm thấy người dùng.");
            if (await _context.Users.AnyAsync(u => u.UserName == userd.UserName && u.Id != upuser.Id))
                throw new Exception("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == userd.Email && u.Id != upuser.Id))
                throw new Exception("Email đã được sử dụng.");

            // Check for unique UserCode
            if (!string.IsNullOrWhiteSpace(userd.UserCode))
            {
                bool userCodeExists = await _context.UserProfiles
                    .AnyAsync(up => up.UserCode == userd.UserCode && up.UserId != upuser.Id);
                if (userCodeExists)
                    throw new Exception("UserCode đã tồn tại.");
            }
            if (!string.IsNullOrWhiteSpace(userd.Email))
                upuser.Email = userd.Email;
            // Kiểm tra UserProfile có null không, nếu null thì khởi tạo
            if (upuser.UserProfile == null)
            {
                upuser.UserProfile = new UserProfile
                {
                    UserId = upuser.Id
                };
            }
            if (!string.IsNullOrWhiteSpace(userd.FullName))
                upuser.UserProfile.FullName = userd.FullName;
            if (!string.IsNullOrWhiteSpace(userd.UserCode))
                upuser.UserProfile.UserCode = userd.UserCode;
            if (userd.Gender.HasValue)
                upuser.UserProfile.Gender = userd.Gender;
            if (!string.IsNullOrWhiteSpace(userd.PhoneNumber))
                upuser.PhoneNumber = userd.PhoneNumber;

            if (imgFile != null && imgFile.Length > 0)
            {
                upuser.UserProfile.Avatar = await SaveAvatar(imgFile, upuser.UserProfile.Avatar);
            }
            if (!string.IsNullOrWhiteSpace(userd.Address))
                upuser.UserProfile.Address = userd.Address;
            if (userd.Dob.HasValue)
                upuser.UserProfile.Dob = DateOnly.FromDateTime(userd.Dob.Value);

            _context.Users.Update(upuser);
            await _context.SaveChangesAsync();
        }
        public async Task<string> ChangeRole(string userName, int newRoleId)
        {
            // Lấy user và các vai trò hiện tại
            var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng với tên đăng nhập đã cho.");

            // Lấy danh sách tất cả vai trò
            var allRoles = await _context.Roles.ToListAsync();

            // Kiểm tra vai trò mới có tồn tại không
            var newRole = allRoles.FirstOrDefault(r => r.Id == newRoleId);
            if (newRole == null)
                throw new Exception("Vai trò muốn chuyển không tồn tại.");

            // Gán vai trò mới cho user (chỉ giữ vai trò này)
            user.Roles = new List<Role> { newRole };

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return $"Đã đổi vai trò thành công sang: {newRole.RoleName}";
        }
        public async Task ForgetPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Email không tồn tại. Hãy sử dụng email khác.");
            // Tạo token xác nhận email
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email));
            var forgetpasswordLink = $"https://localhost:7298/api/User/forget-password?token={HttpUtility.UrlEncode(token)}";

            string message = $"Vui lòng xác nhận email bằng cách <a href='{forgetpasswordLink}'>nhấn vào đây.</a>";
            await _emailService.SendEmail(user.Email, "Đặt lại mật khẩu", message);

        }
        public async Task<string> ResetPassword(string token, string newPassword)
        {
            var decodedToken = HttpUtility.UrlDecode(token);
            var email = Encoding.UTF8.GetString(Convert.FromBase64String(decodedToken));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Email không tồn tại.");
            // Kiểm tra mật khẩu mới
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new Exception("Mật khẩu mới không được để trống.");
            // Hash mật khẩu mới
            user.PassWordHash = PasswordHasher.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return "Đặt lại mật khẩu thành công.";
        }
        //Lấy danh sách sinh viên có trong lớp của giảng viên
        public async Task<TeacherWithClassesViewModel> GetStudentByTeacher(Guid? teacherId)
        {
            // 🔎 Truy vấn tên giảng viên
            var teacher = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == teacherId);

            if (teacher == null)
                throw new Exception("Không tìm thấy giảng viên.");
            var teacherName = teacher.UserProfile?.FullName ?? "Không rõ";
            // Lấy các lớp của giảng viên  
            var classList = await _context.Classes
                .Where(c => c.UsersId == teacherId)
                .Include(c => c.Students)
                    .ThenInclude(s => s.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(c => c.Students)
                    .ThenInclude(s => s.User)
                        .ThenInclude(u => u.Roles)
                .ToListAsync();

            var result = new TeacherWithClassesViewModel
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.UserProfile?.FullName ?? "Không rõ",
                Classes = classList.Select(c => new ClassWithStudentsViewModel
                {
                    ClassId = c.Id,
                    ClassName = c.NameClass,
                    StudentsInfor = c.Students
                        .Where(s => s.UserId != null && s.User != null)
                        .Select(s => MapToUserDTO(s.User))
                        .ToList()
                }).ToList()
            };

            return result;
        }
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