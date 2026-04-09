using Microsoft.EntityFrameworkCore;
using Task4UserManager.Models;

namespace Task4UserManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<AppUser>();

        user.ToTable("Users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        user.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        user.Property(x => x.NormalizedEmail)
            .HasMaxLength(255)
            .IsRequired();

        user.Property(x => x.PasswordHash)
            .IsRequired();

        user.Property(x => x.IsBlocked)
            .HasDefaultValue(false);

        user.Property(x => x.CreatedAtUtc)
            .IsRequired();

        user.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedEmail_Unique");
    }
}
