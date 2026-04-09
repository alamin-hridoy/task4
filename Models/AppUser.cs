using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task4UserManager.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsBlocked { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? EmailConfirmedAtUtc { get; set; }

    [MaxLength(200)]
    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; set; }

    [NotMapped]
    public string Status =>
        IsBlocked ? "Blocked" :
        EmailConfirmedAtUtc.HasValue ? "Active" :
        "Unverified";
}
