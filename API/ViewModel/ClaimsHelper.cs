using System.Security.Claims;

namespace API.ViewModel
{
   
        public static class ClaimsHelper
        {
            public static Guid GetUserId(this ClaimsPrincipal user)
            {
                var userId = user.FindFirst("UserId")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
            }

            public static string GetUserName(this ClaimsPrincipal user)
            {
                return user.FindFirst("UserName")?.Value ?? user.Identity?.Name ?? "";
            }

            public static List<string> GetUserRoles(this ClaimsPrincipal user)
            {
                return user.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                    .Select(c => c.Value)
                    .ToList();
            }

            public static bool IsInRole(this ClaimsPrincipal user, string roleName)
            {
                return user.IsInRole(roleName) || GetUserRoles(user).Contains(roleName);
            }
        }
    
}
