using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace EXE.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    
    public EmailSender(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("Farm2U", _config["EmailSettings:SenderEmail"]));
        emailMessage.To.Add(new MailboxAddress("", email));
        emailMessage.Subject = subject;

        emailMessage.Body = new TextPart("html") { Text = htmlMessage };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["EmailSettings:SmtpUser"], _config["EmailSettings:SmtpPass"]);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
    }
}