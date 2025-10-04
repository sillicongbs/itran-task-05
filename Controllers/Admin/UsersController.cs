using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAuthManage.Domains;
using UserAuthManage.Models;

namespace UserAuthManage.Controllers.Admin
{
    
    [Route("Admin/[controller]/[action]")]
    public sealed class UsersController(AppDbContext db) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rows = await db.Users
                .AsNoTracking()
                .OrderByDescending(u => u.LastLoginUtc) // THE THIRD REQUIREMENT: sorted by last login desc
                .Select(u => new Row(u.Id, u.Name, u.Email, u.LastLoginUtc, u.Status))
                .ToListAsync();

            return View(rows);
        }

        public sealed record Row(Guid Id, string Name, string Email, DateTime? LastLoginUtc, UserStatus Status);

        // Bulk Posts (same Index view posts to these via <form> handler):
        [HttpPost]
        public async Task<IActionResult> Block([FromForm] Guid[] selectedIds)
        {
            var users = await db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
            foreach (var u in users) u.Status = UserStatus.Blocked;
            await db.SaveChangesAsync();
            // If current user is among selected -> sign out and go to Login immediately
            if (Guid.TryParse(User.FindFirst("uid")?.Value, out var myId) && selectedIds.Contains(myId))
            {
                await HttpContext.SignOutAsync();
                TempData["Danger"] = "Your account was blocked. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
            TempData["Success"] = "Blocked successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Unblock([FromForm] Guid[] selectedIds)
        {
            var users = await db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
            foreach (var u in users) if (u.Status == UserStatus.Blocked) u.Status = UserStatus.Active;
            await db.SaveChangesAsync();
            TempData["Success"] = "Unblocked successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromForm] Guid[] selectedIds)
        {
            var users = await db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
            db.Users.RemoveRange(users); // THE REQUIREMENT: physical delete
            await db.SaveChangesAsync();
            if (Guid.TryParse(User.FindFirst("uid")?.Value, out var myId) && selectedIds.Contains(myId))
            {
                await HttpContext.SignOutAsync();
                TempData["Danger"] = "Your account was deleted.";
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = "Deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnverified()
        {
            var users = await db.Users.Where(u => u.Status == UserStatus.Unverified).ToListAsync();
            db.Users.RemoveRange(users);
            await db.SaveChangesAsync();
            TempData["Success"] = "All unverified users deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
