using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string Password { get; set; }
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var client = new SmtpClient(_emailSettings.SmtpServer)
        {
            Port = _emailSettings.Port,
            Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(to);

        await client.SendMailAsync(mail);
    }
}
