using SendGrid;
using SendGrid.Helpers.Mail;

namespace BillFlow.NotificationService.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        IConfiguration config,
        ILogger<SendGridEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody)
    {
        var apiKey = _config["SendGrid:ApiKey"];

        // In development without a real API key — log and skip
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SENDGRID_API_KEY")
        {
            _logger.LogWarning(
                "[DEV] Email skipped (no SendGrid key). " +
                "Would send '{Subject}' to {Email}",
                subject, toEmail);
            return true;  // return true so we don't retry in dev
        }

        var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@billflow.io";
        var fromName = _config["SendGrid:FromName"] ?? "BillFlow";

        var client = new SendGridClient(apiKey);

        var msg = MailHelper.CreateSingleEmail(
            from: new EmailAddress(fromEmail, fromName),
            to: new EmailAddress(toEmail, toName),
            subject: subject,
            plainTextContent: null,
            htmlContent: htmlBody);

        try
        {
            var response = await client.SendEmailAsync(msg);
            var success = (int)response.StatusCode is >= 200 and < 300;

            if (success)
                _logger.LogInformation(
                    "Email sent to {Email}: '{Subject}'", toEmail, subject);
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError(
                    "SendGrid error {StatusCode} for {Email}: {Body}",
                    response.StatusCode, toEmail, body);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}