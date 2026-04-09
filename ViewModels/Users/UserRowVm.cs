namespace Task4UserManager.ViewModels.Users;

public class UserRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastSeenText { get; set; } = string.Empty;
    public DateTime? LastSeenSortValue { get; set; }
    public string CreatedAtText { get; set; } = string.Empty;
}
