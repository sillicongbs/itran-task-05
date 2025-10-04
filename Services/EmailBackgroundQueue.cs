using System.Threading.Channels;

namespace UserAuthManage.Services
{
    public class EmailBackgroundQueue
    {
        private readonly Channel<EmailMessage> _ch = Channel.CreateUnbounded<EmailMessage>();
        public ValueTask SendAsync(EmailMessage msg) => _ch.Writer.WriteAsync(msg);
        public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken ct) => _ch.Reader.ReadAllAsync(ct);
    }
}
