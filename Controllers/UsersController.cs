using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task4UserManager.Data;
using Task4UserManager.ViewModels.Users;

namespace Task4UserManager.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? filter = null)
    {
        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var search = filter.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Email.ToLower().Contains(search));
        }

        var users = await query
            .OrderByDescending(x => x.LastLoginAtUtc.HasValue)
            .ThenByDescending(x => x.LastLoginAtUtc)
            .ThenBy(x => x.Name)
            .Select(x => new UserRowVm
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Status = x.IsBlocked
                    ? "Blocked"
                    : x.EmailConfirmedAtUtc.HasValue ? "Active" : "Unverified",
                LastSeenText = x.LastLoginAtUtc.HasValue
                    ? ToLastSeenText(x.LastLoginAtUtc.Value)
                    : "Never logged in",
                LastSeenSortValue = x.LastLoginAtUtc,
                CreatedAtText = x.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
            })
            .ToListAsync();

        var model = new UsersIndexVm
        {
            Filter = filter,
            Users = users
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(string operation, List<Guid> selectedIds, string? filter = null)
    {
        if (selectedIds.Count == 0)
        {
            TempData["StatusMessage"] = "Please select at least one user.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index), new { filter });
        }

        var users = await _dbContext.Users
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        if (users.Count == 0)
        {
            TempData["StatusMessage"] = "Selected users no longer exist.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index), new { filter });
        }

        var currentUserId = GetCurrentUserId();
        var shouldLogout = false;
        var logoutReason = "missing";

        switch (operation)
        {
            case "block":
                foreach (var user in users)
                {
                    user.IsBlocked = true;
                    if (user.Id == currentUserId)
                    {
                        shouldLogout = true;
                        logoutReason = "blocked";
                    }
                }

                TempData["StatusMessage"] = "Selected users were blocked.";
                TempData["StatusType"] = "success";
                break;

            case "unblock":
                foreach (var user in users)
                {
                    user.IsBlocked = false;
                }

                TempData["StatusMessage"] = "Selected users were unblocked.";
                TempData["StatusType"] = "success";
                break;

            case "delete":
                foreach (var user in users)
                {
                    if (user.Id == currentUserId)
                    {
                        shouldLogout = true;
                    }
                }

                _dbContext.Users.RemoveRange(users);
                TempData["StatusMessage"] = "Selected users were deleted.";
                TempData["StatusType"] = "success";
                break;

            case "delete-unverified":
                var unverifiedUsers = users
                    .Where(x => !x.EmailConfirmedAtUtc.HasValue)
                    .ToList();

                foreach (var user in unverifiedUsers)
                {
                    if (user.Id == currentUserId)
                    {
                        shouldLogout = true;
                    }
                }

                _dbContext.Users.RemoveRange(unverifiedUsers);
                TempData["StatusMessage"] = unverifiedUsers.Count == 0
                    ? "No unverified users were selected."
                    : "Selected unverified users were deleted.";
                TempData["StatusType"] = unverifiedUsers.Count == 0 ? "warning" : "success";
                break;

            default:
                TempData["StatusMessage"] = "Unknown action.";
                TempData["StatusType"] = "danger";
                return RedirectToAction(nameof(Index), new { filter });
        }

        await _dbContext.SaveChangesAsync();

        if (shouldLogout)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { reason = logoutReason });
        }

        return RedirectToAction(nameof(Index), new { filter });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string ToLastSeenText(DateTime utcDateTime)
    {
        var span = DateTime.UtcNow - utcDateTime;

        if (span.TotalSeconds < 60)
        {
            return "Less than a minute ago";
        }

        if (span.TotalMinutes < 60)
        {
            var minutes = (int)Math.Floor(span.TotalMinutes);
            return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")} ago";
        }

        if (span.TotalHours < 24)
        {
            var hours = (int)Math.Floor(span.TotalHours);
            return $"{hours} hour{(hours == 1 ? string.Empty : "s")} ago";
        }

        if (span.TotalDays < 30)
        {
            var days = (int)Math.Floor(span.TotalDays);
            return $"{days} day{(days == 1 ? string.Empty : "s")} ago";
        }

        return utcDateTime.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
    }
}
