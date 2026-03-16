using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Services.Interfaces;

public interface IWebhookJobChannel
{
    ValueTask WriteAsync(WebhookJob job, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    bool TryRead(out WebhookJob job);
}
