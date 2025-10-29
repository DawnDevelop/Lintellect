using System.Threading.Channels;
using Lintellect.Api.Domain.Entities;

namespace Lintellect.Api.Infrastructure.Services.Webhooks;

/// <summary>
/// Channel-based job queue for webhook events following CleanArchitecture pattern.
/// </summary>
public sealed class WebhookJobQueue
{
    private readonly Channel<WebhookEvent> _channel;

    public WebhookJobQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<WebhookEvent>(options);
    }

    /// <summary>
    /// Enqueues a webhook event for processing.
    /// </summary>
    public async Task<bool> EnqueueAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _channel.Writer.WriteAsync(webhookEvent, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dequeues a webhook event for processing.
    /// </summary>
    public async Task<WebhookEvent?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the channel reader for background processing.
    /// </summary>
    public ChannelReader<WebhookEvent> Reader => _channel.Reader;

    /// <summary>
    /// Gets the channel writer for enqueueing webhook events.
    /// </summary>
    public ChannelWriter<WebhookEvent> Writer => _channel.Writer;
}

