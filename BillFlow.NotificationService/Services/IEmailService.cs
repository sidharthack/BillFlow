namespace BillFlow.NotificationService.Services;

public interface IEmailService
{
    Task<bool> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody);
}