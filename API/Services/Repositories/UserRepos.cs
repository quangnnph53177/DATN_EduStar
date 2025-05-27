using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services.Repositories
{
    public class UserRepos : IUserRepos
    {
        private readonly AduDbcontext _context;
        private readonly IConfiguration _configuration;
        public UserRepos(AduDbcontext adu, IConfiguration configuration)
        {
            _context = adu;
            _configuration = configuration;
        }
        public async Task<User> Register(UserDTO usd)
        {
            // Kiểm tra thông tin đầu vào
            if (string.IsNullOrWhiteSpace(usd.UserName) ||
                string.IsNullOrWhiteSpace(usd.PassWordHash) ||
                string.IsNullOrWhiteSpace(usd.Email))
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
            var roles = await _context.Roles
                 .Where(r => roleIds.Contains(r.Id))
                 .ToListAsync();
            if (!roles.Any())
                throw new Exception("Không tìm thấy role hợp lệ để gán.");


            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = usd.UserName,
                PassWordHash = hashedPassword,
                Email = usd.Email,
                PhoneNumber = usd.PhoneNumber,
                Roles = roles,
                Statuss = true,
                CreateAt = DateTime.Now
            };

            var userProfile = new UserProfile
            {
                UserId = user.Id,
                FullName = usd.FullName,
                Gender = usd.Gender,
                Dob = usd.Dob.HasValue ? DateOnly.FromDateTime(usd.Dob.Value) : null,
                Avatar = usd.Avatar,
                Address = usd.Address
            };
            _context.Users.Add(user);
            _context.UserProfiles.Add(userProfile);

            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<string> Login(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không được để trống.");

            var user = await _context.Users
                .Include(u => u.Roles).ThenInclude(r => r.Permissons)
                .FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                throw new Exception("Tên đăng nhập không tồn tại.");

            // Kiểm tra trạng thái tài khoản
            if (user.Statuss != true)
                throw new Exception("Tài khoản đang bị khóa hoặc không hoạt động.");
            bool isPasswordValid = PasswordHasher.Verify(password, user.PassWordHash);
            if (!isPasswordValid)
                throw new Exception("Mật khẩu không chính xác.");
            string token = GenerateJwtToken(user);
            return token;
        }
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),

            };
            if (user.Roles == null || !user.Roles.Any())
            {
                claims.Add(new Claim(ClaimTypes.Role, "User")); // default role
            }
            else
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));

                    foreach (var perm in role.Permissons)
                    {
                        claims.Add(new Claim("Permisson", perm.PermissonName));
                    }
                }
            }

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

        public async Task<IEnumerable<UserDTO>> GetAllUsers()
        {
            var lst = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Roles)
                .AsSplitQuery()
                .ToListAsync();
            var userDtos = lst.Select(u => new UserDTO
            {
                UserName = u.UserName,
                PassWordHash = u.PassWordHash,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Statuss = u.Statuss ?? true,
                CreateAt = u.CreateAt,

                // UserProfile
                FullName = u.UserProfile?.FullName,
                Gender = u.UserProfile?.Gender,
                Avatar = u.UserProfile?.Avatar,
                Address = u.UserProfile?.Address,
                Dob = u.UserProfile?.Dob.HasValue == true ? u.UserProfile.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                // RoleIds lấy danh sách Id role
                RoleIds = u.Roles.Select(r => r.Id).ToList()
            });

            return userDtos;
        }

        public async Task<UserDTO> GetUserByName(string username)
        {
            var user = await _context.Users.Include(u => u.UserProfile)
                .Include(u => u.Roles)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng với username đã cho.");
            var userDto = new UserDTO
            {
                UserName = user.UserName,
                PassWordHash = user.PassWordHash,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Statuss = user.Statuss ?? true,
                CreateAt = user.CreateAt,

                FullName = user.UserProfile?.FullName,
                Gender = user.UserProfile?.Gender,
                Avatar = user.UserProfile?.Avatar,
                Address = user.UserProfile?.Address,
                Dob = user.UserProfile?.Dob.HasValue == true ? user.UserProfile.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                RoleIds = user.Roles.Select(r => r.Id).ToList()
            };

            return userDto;
        }


        public async Task UpdateUser(UserDTO userd)
        {
            var upinsv = await _context.Users
                  .Include(p => p.UserProfile)
                  .FirstOrDefaultAsync(d => d.UserName == userd.UserName);
            if (upinsv == null)
                throw new Exception("Không tìm thấy người dùng.");
            if (await _context.Users.AnyAsync(u => u.UserName == userd.UserName && u.Id != upinsv.Id))
                throw new Exception("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == userd.Email && u.Id != upinsv.Id))
                throw new Exception("Email đã được sử dụng.");
            upinsv.PassWordHash = userd.PassWordHash;
            upinsv.Email = userd.Email;
            upinsv.UserProfile.FullName = userd.FullName;
            upinsv.UserProfile.Gender = userd.Gender;
            upinsv.PhoneNumber = userd.PhoneNumber;
            upinsv.UserProfile.Avatar = userd.Avatar;
            upinsv.UserProfile.Address = userd.Address;
            upinsv.Statuss = userd.Statuss;
            upinsv.UserProfile.Dob = userd.Dob.HasValue ? DateOnly.FromDateTime(userd.Dob.Value) : null;
            
            _context.Users.Update(upinsv);
            await _context.SaveChangesAsync();
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
