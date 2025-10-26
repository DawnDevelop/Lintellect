using System.Diagnostics.Metrics;

namespace Lintellect.Api.Infrastructure.Telemetry;

/// <summary>
/// Metrics for analysis job processing and API performance.
/// </summary>
public class AnalysisMetrics
{
  private readonly Counter<long> _jobsSubmitted;
  private readonly Counter<long> _jobsCompleted;
  private readonly Counter<long> _jobsFailed;
  private readonly Histogram<double> _jobDuration;
  private readonly Counter<long> _apiCallsTotal;
  private readonly Histogram<double> _apiCallDuration;

  public AnalysisMetrics(IMeterFactory meterFactory)
  {
    var meter = meterFactory.Create("DevOpsPrAnalyzer.Analysis");

    _jobsSubmitted = meter.CreateCounter<long>(
        "jobs.submitted",
        "Total number of analysis jobs submitted");

    _jobsCompleted = meter.CreateCounter<long>(
        "jobs.completed",
        "Total number of analysis jobs completed successfully");

    _jobsFailed = meter.CreateCounter<long>(
        "jobs.failed",
        "Total number of analysis jobs that failed");

    _jobDuration = meter.CreateHistogram<double>(
        "jobs.duration",
        "seconds",
        "Duration of analysis job processing in seconds");

    _apiCallsTotal = meter.CreateCounter<long>(
        "api.calls.total",
        "Total number of external API calls made");

    _apiCallDuration = meter.CreateHistogram<double>(
        "api.calls.duration",
        "seconds",
        "Duration of external API calls in seconds");
  }

  /// <summary>
  /// Records a job submission.
  /// </summary>
  public void RecordJobSubmitted(string analyzerType)
  {
    _jobsSubmitted.Add(1, new KeyValuePair<string, object?>("analyzer_type", analyzerType));
  }

  /// <summary>
  /// Records a job completion.
  /// </summary>
  public void RecordJobCompleted(string analyzerType, double durationSeconds)
  {
    _jobsCompleted.Add(1, new KeyValuePair<string, object?>("analyzer_type", analyzerType));
    _jobDuration.Record(durationSeconds, new KeyValuePair<string, object?>("analyzer_type", analyzerType));
  }

  /// <summary>
  /// Records a job failure.
  /// </summary>
  public void RecordJobFailed(string analyzerType, string errorType, double durationSeconds)
  {
    _jobsFailed.Add(1,
        new KeyValuePair<string, object?>("analyzer_type", analyzerType),
        new KeyValuePair<string, object?>("error_type", errorType));
    _jobDuration.Record(durationSeconds,
        new KeyValuePair<string, object?>("analyzer_type", analyzerType),
        new KeyValuePair<string, object?>("status", "failed"));
  }

  /// <summary>
  /// Records an external API call.
  /// </summary>
  public void RecordApiCall(string apiName, string operation, double durationSeconds, bool success)
  {
    _apiCallsTotal.Add(1,
        new KeyValuePair<string, object?>("api_name", apiName),
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("success", success));

    _apiCallDuration.Record(durationSeconds,
        new KeyValuePair<string, object?>("api_name", apiName),
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("success", success));
  }
}
