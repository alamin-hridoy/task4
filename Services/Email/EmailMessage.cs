namespace Task4UserManager.Services.Email;

public record EmailMessage(string To, string Subject, string HtmlBody);
