namespace UserAuthManage.Services
{
    public sealed record EmailMessage
    {
        public required string To { get; init; }
        public required string Subject { get; init; }
        public required string Html { get; init; }
    }

    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken ct = default);
    }
}
