using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class CheckUserStatus
    {
        private readonly RequestDelegate _next;
        public CheckUserStatus(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, AduDbcontext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userName = context.User.Identity.Name;
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null || user.Statuss != true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Tài khoản đã bị khóa. Vui lòng đăng nhập lại bằng tài khoản khác.mại iu hẹ hẹ hẹ cái db");
                    return;
                }
            }
            await _next(context);
        }
    }
}
