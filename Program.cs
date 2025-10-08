using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UserAuthManage.Filters;
using UserAuthManage.Models;
using UserAuthManage.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ConnStr")));

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(opt =>
  {
      opt.LoginPath = "/Account/Login";
      opt.LogoutPath = "/Account/Logout";
      opt.AccessDeniedPath = "/Account/Login";
      opt.SlidingExpiration = true;
  });

builder.Services.AddHttpContextAccessor();

// async email pipeline
builder.Services.AddSingleton<EmailBackgroundQueue>();
builder.Services.AddHostedService<EmailSenderHostedService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();
app.MapGet("/db-check", async (IConfiguration cfg) =>
{
    var cs = cfg.GetConnectionString("ConnStr");
    try
    {
        using var cn = new SqlConnection(cs);
        await cn.OpenAsync();
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "DB connect failed", detail: ex.Message);
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// THE FIFTH REQUIREMENT: block/redirect for deleted/blocked users (except login/register/confirm/static)
app.UseMiddleware<UserGuardMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
