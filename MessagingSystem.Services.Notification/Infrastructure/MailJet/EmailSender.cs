using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Newtonsoft.Json.Linq;

namespace MessagingSystem.Services.Notification.Infrastructure.MailJet;

public class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger) : IEmailSender
{
    private MailJetSettings? MailJetSettings { get; set; }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogError("Email is null or empty");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(htmlMessage))
            {
                logger.LogError("Message is empty");
                return;
            }
            
            MailJetSettings = configuration.GetSection("MailJet").Get<MailJetSettings>();
            if (MailJetSettings == null)
            {
                logger.LogError("MailJet settings are null");
                return;
            }
            
            var client = new MailjetClient(MailJetSettings.ApiKey, MailJetSettings.SecretKey);

            var request = new MailjetRequest
                {
                    Resource = Send.Resource,
                }
                .Property(Send.FromEmail, Wc.FromEmail)
                .Property(Send.FromName, Wc.FromName)
                .Property(Send.Subject, subject)
                .Property(Send.HtmlPart, htmlMessage)
                .Property(Send.Recipients, new JArray {
                    new JObject {
                        { "Email", email },
                        { "Name", email }
                    }
                });

            var response = await client.PostAsync(request);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Email sent successfully to user.");
            }
            else
            {
                logger.LogError("Failed to send email. Status: {StatusCode}, Response: {ResponseData}", 
                    response.StatusCode, response.GetData());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while sending email to {email}.", email);
            throw;
        }
    }
}