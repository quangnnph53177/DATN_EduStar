using API.Data;
using API.Models;
using API.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
        public async Task<User> Register(UserDTO usd, IFormFile imgFile)
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

            string userCode = "";
            if (roles.Any(r => r.Id == 1)) // Admin  
            {
                int adminCount = await _context.Users.Where(u => u.Roles.Any(r => r.Id == 1)).CountAsync();
                userCode = $"AD{(adminCount + 1).ToString("D5")}";
            }
            else if (roles.Any(r => r.Id == 2)) // Giảng viên  
            {
                int gvCount = await _context.Users.Where(u => u.Roles.Any(r => r.Id == 2)).CountAsync();
                userCode = $"GV{(gvCount + 1).ToString("D5")}";
            }
            else if (roles.Any(r => r.Id == 3)) // Sinh viên  
            {
                int svCount = await _context.Users.Where(u => u.Roles.Any(r => r.Id == 3)).CountAsync();
                userCode = $"SV{(svCount + 1).ToString("D5")}";
            }
            else // Mặc định (ví dụ sinh viên chưa có role), vẫn tạo userCode tạm  
            {
                userCode = $"US{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
            }

            if (imgFile != null && imgFile.Length > 0)
            {
                // Kiểm tra định dạng file ảnh  
                var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imgFile.FileName).ToLowerInvariant();
                if (!validImageFormats.Contains(fileExtension))
                    throw new ArgumentException("Định dạng ảnh không hợp lệ (chỉ chấp nhận .jpg, .jpeg, .png)");
                // Tạo đường dẫn lưu ảnh
                var imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images","avatars");
                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var savePath = Path.Combine(imageFolder, uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imgFile.CopyToAsync(stream);
                }
                // Gán đường dẫn ảo vào DTO
                usd.Avatar = $"/images/avatars/{uniqueFileName}";
            }

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
            var now = DateTime.UtcNow;

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
            // Trả về roleId đầu tiên (trường hợp nhiều role thì chọn role đầu tiên)
            int roleId = user.Roles.Select(r => r.Id).FirstOrDefault();
            return new LoginResult
            {
                Token = token,
                RoleId = roleId,
                UserName = user.UserName
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
                new Claim(ClaimTypes.Name, user.UserName),

            };

            // Thêm RoleId (sử dụng roleId để kiểm tra quyền nhanh gọn)
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Id.ToString())));

            // Thêm Permission
            claims.AddRange(permissions.Select(p => new Claim("Permission", p)));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        public async Task<IEnumerable<UserDTO>> GetAllUsers(List<int> currentUserRoleIds, string? currentUserName)
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Roles)
                .AsSplitQuery();

            if (currentUserRoleIds.Contains(1)) // Admin
            {
                query = query.Where(u => u.Roles.Any(r => r.Id == 1 || r.Id == 2 || r.Id == 3 || u.UserName == currentUserName));
            }
            else if (currentUserRoleIds.Contains(2)) // Giảng viên
            {
                query = query.Where(u => u.Roles.Any(r => r.Id == 3 || u.UserName == currentUserName));
            }
            else
            {
                query = query.Where(u => u.UserName == currentUserName);
            }

            var users = await query.OrderByDescending(u => u.Statuss).ToListAsync();

            return users.Select(x => new
            {
                User = x,
                MainRole = x.Roles.Min(r => r.Id) // hoặc viết logic riêng nếu cần xác định ưu tiên
            })
            .OrderByDescending(u => u.User.Statuss ?? false) // Hoạt động trước
            .ThenBy(u => u.MainRole) // Vai trò ưu tiên: Admin (1) → Giảng viên (2) → Sinh viên (3)
            .Select(u => new UserDTO
            {
                UserName = u.User.UserName, // Fixed: Accessing User property of the anonymous type
                Email = u.User.Email,
                PhoneNumber = u.User.PhoneNumber,
                Statuss = u.User.Statuss ?? false,
                CreateAt = u.User.CreateAt,
                UserCode = u.User.UserProfile?.UserCode,
                FullName = u.User.UserProfile?.FullName,
                Gender = u.User.UserProfile?.Gender,
                Avatar = u.User.UserProfile?.Avatar,
                Address = u.User.UserProfile?.Address,
                Dob = u.User.UserProfile?.Dob?.ToDateTime(TimeOnly.MinValue),
                RoleIds = u.User.Roles.Select(r => r.Id).ToList()
            });
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersNoTeacher(List<int> currentUserRoleIds, string? currentUserName)
        {
            IQueryable<User> query = _context.Users
            .Include(u => u.UserProfile)
            .Include(u => u.Roles)
            .AsSplitQuery();

            if (currentUserRoleIds.Contains(1)) // Admin
            {
                query = query.Where(u => u.Roles.Any(r => r.Id == 1 || r.Id == 2 || r.Id == 3 || u.UserName == currentUserName));
            }
            else if (currentUserRoleIds.Contains(2)) // Giảng viên
            {
                query = query.Where(u => u.UserName == currentUserName);
            }
            else
            {
                query = query.Where(u => u.UserName == currentUserName);
            }

            var users = await query.ToListAsync();

            return users.Select(u => new UserDTO
            {
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
                Dob = u.UserProfile?.Dob?.ToDateTime(TimeOnly.MinValue),
                RoleIds = u.Roles.Select(r => r.Id).ToList()
            });
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

            // Email
            if (!string.IsNullOrWhiteSpace(userd.Email))
                upuser.Email = userd.Email;

            // Kiểm tra UserProfile có null không, nếu null thì khởi tạo
            if (upuser.UserProfile == null)
                upuser.UserProfile = new UserProfile();

            // FullName
            if (!string.IsNullOrWhiteSpace(userd.FullName))
                upuser.UserProfile.FullName = userd.FullName;

            //if (!string.IsNullOrWhiteSpace(userd.UserCode))
            //    upuser.UserProfile.UserCode = userd.UserCode;

            // Gender
            if (userd.Gender.HasValue)
                upuser.UserProfile.Gender = userd.Gender;

            // PhoneNumber
            if (!string.IsNullOrWhiteSpace(userd.PhoneNumber))
                upuser.PhoneNumber = userd.PhoneNumber;

            if (imgFile != null && imgFile.Length > 0)
            {
                // 1. Kiểm tra định dạng hợp lệ
                var validImageFormats = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imgFile.FileName).ToLowerInvariant();
                if (!validImageFormats.Contains(fileExtension))
                    throw new ArgumentException("Định dạng ảnh không hợp lệ (chỉ chấp nhận .jpg, .jpeg, .png)");

                // 2. Xác định thư mục lưu ảnh
                var imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);

                // 3. Xóa ảnh cũ nếu có
                if (!string.IsNullOrWhiteSpace(upuser.UserProfile.Avatar))
                {
                    var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", upuser.UserProfile.Avatar.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                        System.IO.File.Delete(oldAvatarPath);
                }

                // 4. Lưu ảnh mới
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var savePath = Path.Combine(imageFolder, uniqueFileName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imgFile.CopyToAsync(stream);
                }

                // 5. Cập nhật đường dẫn tương đối vào DB (dùng khi hiển thị ảnh)
                upuser.UserProfile.Avatar = $"/images/avatars/{uniqueFileName}";
            }

            // Address
            if (!string.IsNullOrWhiteSpace(userd.Address))
                upuser.UserProfile.Address = userd.Address;

            // Dob
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