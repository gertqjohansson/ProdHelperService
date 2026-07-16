using Azure;
using Azure.Communication.Email;

namespace ProdHelperService.Auth;

public class AzureCommunicationEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
    {
        string connectionString = configuration["Email:ConnectionString"] is { Length: > 0 } cs
            ? cs
            : throw new InvalidOperationException("Email:ConnectionString is not configured.");
        string senderAddress = configuration["Email:SenderAddress"] is { Length: > 0 } sender
            ? sender
            : throw new InvalidOperationException("Email:SenderAddress is not configured.");

        var emailClient = new EmailClient(connectionString);
        var message = new EmailMessage(senderAddress, toEmail, new EmailContent(subject)
        {
            PlainText = plainTextBody,
            Html = htmlBody,
        });

        await emailClient.SendAsync(WaitUntil.Started, message);
    }
}
