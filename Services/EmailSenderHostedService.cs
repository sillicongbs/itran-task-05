using System.Net;
using System.Net.Mail;

namespace UserAuthManage.Services
{
    public class SmtpEmailSender(IConfiguration cfg) : IEmailSender
    {
        public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            var host = cfg["Smtp:Host"]!;
            var port = int.Parse(cfg["Smtp:Port"] ?? "587");
            var user = cfg["Smtp:User"];
            var pass = cfg["Smtp:Pass"];

            using var client = new SmtpClient(host, port) { EnableSsl = true };
            if (!string.IsNullOrEmpty(user))
                client.Credentials = new NetworkCredential(user, pass);

            var mail = new MailMessage("no-reply@useradminapp", message.To, message.Subject, message.Html)
            { IsBodyHtml = true };

            await client.SendMailAsync(mail, ct);
        }
    }

    public class EmailSenderHostedService(EmailBackgroundQueue q, IEmailSender sender, ILogger<EmailSenderHostedService> log) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var msg in q.ReadAllAsync(stoppingToken))
            {
                try { await sender.SendAsync(msg, stoppingToken); }
                catch (Exception ex) { log.LogError(ex, "Email send failed"); }
            }
        }
    }
}
