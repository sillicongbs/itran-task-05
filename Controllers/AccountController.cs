using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserAuthManage.Domains;
using UserAuthManage.Models;
using UserAuthManage.Services;

namespace UserAuthManage.Controllers
{
    public class AccountController(AppDbContext db, EmailBackgroundQueue queue, IConfiguration cfg) : Controller
    {
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(UserRequestDto userRequest)
        {
            if (string.IsNullOrWhiteSpace(userRequest.Email) || string.IsNullOrWhiteSpace(userRequest.Password))
            {
                TempData["Danger"] = "Email and password are required.";
                return View();
            }
            var norm = userRequest.Email.Trim().ToLowerInvariant();   // same normalization you store
            var exists = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.NormalizedEmail == norm);
            if (exists)
            {
               
                TempData["Danger"] = "This email is already registered.";
                return View();
            }
            var user = new User
            {
                Name = string.IsNullOrWhiteSpace(userRequest.Name) ? "Anonymous" : userRequest.Name.Trim(),
                Email = userRequest.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userRequest.Password),
                Status = UserStatus.Unverified,
                Phone = userRequest.Phone.Trim(),
                Address = userRequest.Address.Trim(),
                EmailConfirmToken = Guid.NewGuid()
            };

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(); // IMPORTANT: DB enforces uniqueness (no code checks)
            }
            catch (DbUpdateException)
            {
                TempData["Danger"] = "This e-mail is already registered.";
                return View();
            }

            var baseUrl = cfg["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var url = $"{baseUrl}/Account/ConfirmEmail?email={Uri.EscapeDataString(user.Email)}&token={user.EmailConfirmToken}";
            await queue.SendAsync(new EmailMessage
            {
                To = user.Email,
                Subject = "Confirm your e-mail",
                Html = $"<p>Confirm: <a href=\"{url}\">link</a></p>"
            });

            TempData["Success"] = "Registered. Please confirm via e-mail.";
            return RedirectToAction(nameof(RegisterSuccess), new
            {
                email = user.Email,
                token = user.EmailConfirmToken
            });
        }
        [HttpGet]
        public IActionResult RegisterSuccess(string? email, Guid? token, [FromServices] IWebHostEnvironment env)
        {
            // Optional: hide the inline confirm link outside Development
            ViewBag.ShowDirectConfirmLink = env.IsDevelopment();

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
            {
                TempData["Danger"] = "Email and password are required.";
                return View();
            }

            var norm = loginRequest.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == norm);
            if (user is null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                TempData["Danger"] = "Invalid credentials.";
                return View();
            }
            if (user.Status == UserStatus.Blocked)
            {
                TempData["Danger"] = "Account is blocked.";
                return View();
            }

            user.LastLoginUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var claims = new List<Claim> {
                        new("uid", user.Id.ToString()),
                        new(ClaimTypes.Name, user.Name),
                        new(ClaimTypes.Email, user.Email)
                    };
            var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
            return RedirectToAction("Index", "Users", new { area = "Admin" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet] // GET /Account/ConfirmEmail?email=...&token=...
        public async Task<IActionResult> ConfirmEmail(string email, Guid token)
        {
            
            // important: quick parameter guard
            if (string.IsNullOrWhiteSpace(email) || token == Guid.Empty)
                return View("ConfirmEmailInvalid");

            var norm = email.Trim().ToLowerInvariant();

            // NormalizedEmail is a shadow computed column; query via EF.Property
            var user = await db.Users.SingleOrDefaultAsync(
                u => EF.Property<string>(u, "NormalizedEmail") == norm);

            if (user is null || user.EmailConfirmToken != token)
                return View("ConfirmEmailInvalid");

            // Flip Unverified -> Active. Note: Blocked stays Blocked (as required).
            if (user.Status == UserStatus.Unverified)
                user.Status = UserStatus.Active;

            user.EmailConfirmToken = null;               // one-time use
            user.EmailConfirmedAtUtc = DateTime.UtcNow;  // audit
            await db.SaveChangesAsync();

            ViewBag.Email = user.Email;                  // optional: show on the success page
            return View("ConfirmEmailSuccess");

        }
    }
}
