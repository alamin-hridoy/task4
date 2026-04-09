using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task4UserManager.Data;
using Task4UserManager.Models;
using Task4UserManager.Services.Email;
using Task4UserManager.Services.Helpers;
using Task4UserManager.ViewModels.Account;

namespace Task4UserManager.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IEmailQueue _emailQueue;

    public AccountController(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        IEmailQueue emailQueue)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailQueue = emailQueue;
    }

    [HttpGet]
    public IActionResult Login(string? reason = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Users");
        }

        if (reason == "blocked")
        {
            TempData["StatusMessage"] = "Your account is blocked. Please contact an administrator.";
            TempData["StatusType"] = "danger";
        }
        else if (reason == "missing")
        {
            TempData["StatusMessage"] = "Your account is no longer available. Please register again.";
            TempData["StatusType"] = "warning";
        }

        return View(new LoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = EmailHelper.Normalize(model.Email);
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid e-mail or password.");
            return View(model);
        }

        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Your account is blocked.");
            return View(model);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid e-mail or password.");
            return View(model);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await SignInAsync(user, model.RememberMe);

        TempData["StatusMessage"] = user.EmailConfirmedAtUtc.HasValue
            ? "Welcome back."
            : "Welcome back. Your e-mail is still unverified.";
        TempData["StatusType"] = user.EmailConfirmedAtUtc.HasValue ? "success" : "warning";

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Users");
        }

        return View(new RegisterVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = EmailHelper.Normalize(model.Email);
        var emailExists = await _dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "This e-mail is already registered.");
            return View(model);
        }

        var user = new AppUser
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            CreatedAtUtc = DateTime.UtcNow,
            EmailConfirmationToken = Guid.NewGuid().ToString("N"),
            EmailConfirmationTokenExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "This e-mail is already registered.");
            return View(model);
        }

        await QueueConfirmationEmailAsync(user);
        await SignInAsync(user, isPersistent: false);

        TempData["StatusMessage"] = "Registration completed. We sent a confirmation e-mail asynchronously.";
        TempData["StatusType"] = "success";

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            TempData["StatusMessage"] = "Confirmation link is invalid.";
            TempData["StatusType"] = "danger";
            return RedirectToAction(nameof(Login));
        }

        if (user.IsBlocked)
        {
            TempData["StatusMessage"] = "Blocked accounts stay blocked.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Login));
        }

        if (user.EmailConfirmedAtUtc.HasValue)
        {
            TempData["StatusMessage"] = "Your e-mail is already confirmed.";
            TempData["StatusType"] = "info";
            return RedirectToAction(nameof(Login));
        }

        var isTokenValid = !string.IsNullOrWhiteSpace(user.EmailConfirmationToken)
            && user.EmailConfirmationToken == token
            && user.EmailConfirmationTokenExpiresAtUtc > DateTime.UtcNow;

        if (!isTokenValid)
        {
            TempData["StatusMessage"] = "Confirmation link is invalid or expired.";
            TempData["StatusType"] = "danger";
            return RedirectToAction(nameof(Login));
        }

        user.EmailConfirmedAtUtc = DateTime.UtcNow;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiresAtUtc = null;

        await _dbContext.SaveChangesAsync();

        TempData["StatusMessage"] = "Your e-mail has been confirmed.";
        TempData["StatusType"] = "success";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = "You have been logged out.";
        TempData["StatusType"] = "info";
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInAsync(AppUser user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
    }

    private async Task QueueConfirmationEmailAsync(AppUser user)
    {
        var confirmationUrl = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token = user.EmailConfirmationToken },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(confirmationUrl))
        {
            return;
        }

        var body =
            $"Hello {user.Name},<br/><br/>" +
            "Thank you for registering.<br/>" +
            $"Please confirm your e-mail by clicking this link: <a href=\"{confirmationUrl}\">{confirmationUrl}</a><br/><br/>" +
            "If you did not create this account, you can ignore this message.";

        await _emailQueue.QueueAsync(new EmailMessage(
            user.Email,
            "Confirm your registration",
            body));
    }
}
