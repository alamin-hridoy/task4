namespace Task4UserManager.ViewModels.Users;

public class UsersIndexVm
{
    public string? Filter { get; set; }
    public List<UserRowVm> Users { get; set; } = [];
}
