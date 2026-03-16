using System.Threading.Channels;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;

namespace BitbucketCustomServices.Services;

public class WebhookJobChannel : IWebhookJobChannel
{
    private readonly Channel<WebhookJob> _channel = Channel.CreateUnbounded<WebhookJob>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask WriteAsync(WebhookJob job, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public bool TryRead(out WebhookJob job) => _channel.Reader.TryRead(out job);
}
