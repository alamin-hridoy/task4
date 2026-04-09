using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Task4UserManager.Services.Email;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var fromEmail = _configuration["Resend:FromEmail"];
        var fromName = _configuration["Resend:FromName"] ?? "Task4 App";

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(fromEmail) ||
            apiKey.Contains("your_api_key", StringComparison.OrdinalIgnoreCase) ||
            fromEmail.Contains("your_email", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Resend is not fully configured. Confirmation e-mail to {Recipient} was skipped. Subject: {Subject}",
                message.To,
                message.Subject);
            return;
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new ResendEmailRequest
        {
            From = $"{fromName} <{fromEmail}>",
            To = [message.To],
            Subject = message.Subject,
            Html = message.HtmlBody
        };

        _logger.LogInformation(
            "Starting Resend API send to {Recipient} from {FromEmail}.",
            message.To,
            fromEmail);

        using var response = await _httpClient.PostAsJsonAsync("emails", payload, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Resend API send failed for {Recipient}. Status: {StatusCode}. Response: {ResponseBody}",
                message.To,
                (int)response.StatusCode,
                responseBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation(
            "Resend API send completed successfully for {Recipient}. Response: {ResponseBody}",
            message.To,
            responseBody);
    }

    private sealed class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; init; } = string.Empty;

        [JsonPropertyName("to")]
        public string[] To { get; init; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; init; } = string.Empty;
    }
}
