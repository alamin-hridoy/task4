namespace Task4UserManager.Services.Helpers;

public static class EmailHelper
{
    public static string Normalize(string email) =>
        email.Trim().ToUpperInvariant();
}
