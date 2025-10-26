using System.Threading.Channels;
using Lintellect.Api.Domain.Entities;

namespace Lintellect.Api.Infrastructure.Services;

/// <summary>
/// Channel-based job queue for analysis jobs following CleanArchitecture pattern.
/// </summary>
public sealed class AnalysisJobQueue
{
    private readonly Channel<AnalysisJob> _channel;

    public AnalysisJobQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<AnalysisJob>(options);
    }

    /// <summary>
    /// Enqueues an analysis job for processing.
    /// </summary>
    public async Task<bool> EnqueueAsync(AnalysisJob job, CancellationToken cancellationToken = default)
    {
        try
        {
            await _channel.Writer.WriteAsync(job, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dequeues an analysis job for processing.
    /// </summary>
    public async Task<AnalysisJob?> DequeueAsync(CancellationToken cancellationToken = default)
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
    public ChannelReader<AnalysisJob> Reader => _channel.Reader;

    /// <summary>
    /// Gets the channel writer for enqueueing jobs.
    /// </summary>
    public ChannelWriter<AnalysisJob> Writer => _channel.Writer;
}
