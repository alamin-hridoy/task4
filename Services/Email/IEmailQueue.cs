namespace Task4UserManager.Services.Email;

public interface IEmailQueue
{
    Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<EmailMessage> DequeueAsync(CancellationToken cancellationToken);
}
