using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Handlers;

public interface IWebhookJobHandler
{
    bool CanHandle(WebhookJob job);
    Task HandleAsync(WebhookJob job, CancellationToken cancellationToken = default);
}
