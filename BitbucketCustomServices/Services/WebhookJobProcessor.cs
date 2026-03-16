using BitbucketCustomServices.Handlers;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace BitbucketCustomServices.Services;

public class WebhookJobProcessor : BackgroundService
{
    private readonly IWebhookJobChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookJobProcessor> _logger;

    public WebhookJobProcessor(
        IWebhookJobChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookJobProcessor> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook job processor started");

        while (await _channel.WaitToReadAsync(stoppingToken))
        {
            while (_channel.TryRead(out var job))
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook job for {Workspace}/{RepoSlug}, EventType: {EventType}",
                        job.Workspace, job.RepoSlug, job.EventType);
                }
            }
        }
    }

    private async Task ProcessJobAsync(WebhookJob job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IWebhookJobHandler>().ToList();

        foreach (var handler in handlers.Where(h => h.CanHandle(job)))
        {
            try
            {
                await handler.HandleAsync(job, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for job {Workspace}/{RepoSlug}",
                    handler.GetType().Name, job.Workspace, job.RepoSlug);
            }
        }
    }
}
