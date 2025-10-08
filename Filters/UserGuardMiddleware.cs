using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using UserAuthManage.Domains;
using UserAuthManage.Models;

namespace UserAuthManage.Filters
{
    public class UserGuardMiddleware(RequestDelegate next)
    {
        public async Task Invoke(HttpContext ctx, AppDbContext db)
        {
            var path = (ctx.Request.Path.Value ?? "").ToLowerInvariant();
            bool allow = path.StartsWith("/db-check") ||
            path.StartsWith("/health") ||
                path.StartsWith("/account/login") ||
                path.StartsWith("/account/register") ||
                path.StartsWith("/account/confirmemail") ||
                path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib");

            if (allow) { await next(ctx); return; }

            if (ctx.User?.Identity?.IsAuthenticated != true)
            {
                ctx.Response.Redirect("/Account/Login");
                return;
            }

            var idStr = ctx.User.FindFirst("uid")?.Value;
            if (!Guid.TryParse(idStr, out var uid))
            {
                await ctx.SignOutAsync();
                ctx.Response.Redirect("/Account/Login");
                return;
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
            if (user is null || user.Status == UserStatus.Blocked)
            {
                await ctx.SignOutAsync();
                ctx.Response.Redirect("/Account/Login");
                return;
            }

            await next(ctx);
        }
    }
}
