using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Domain.Events;

namespace Lintellect.Api.UnitTests.Domain;

[TestFixture]
public class AnalysisJobTests
{
    [Test]
    public void Constructor_WithValidRequest_CreatesJobWithPendingStatus()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();

        // Act
        var job = new AnalysisJob(request);

        // Assert
        job.Status.ShouldBe(AnalysisStatus.Pending);
        job.StartedAt.ShouldBeNull();
        job.CompletedAt.ShouldBeNull();
        job.ErrorMessage.ShouldBeNull();
        job.AnalysisRequest.ShouldNotBeNull();
    }

    [Test]
    public void Constructor_WithValidRequest_RaisesDomainEvent()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();

        // Act
        var job = new AnalysisJob(request);

        // Assert
        job.DomainEvents.Count.ShouldBe(1);
        job.DomainEvents.Count(e => e is AnalysisJobCreatedEvent).ShouldBe(1);

        var createdEvent = job.DomainEvents.OfType<AnalysisJobCreatedEvent>().Single();
        createdEvent.JobId.ShouldBe(job.Id);
        createdEvent.ProjectName.ShouldBe("TestProject");
        createdEvent.RepositoryName.ShouldBe("TestRepo");
        createdEvent.PullRequestId.ShouldBe(123);
    }

    [Test]
    public void Start_WhenPending_UpdatesStatusAndStartTime()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();

        // Act
        job.Start();

        // Assert
        job.Status.ShouldBe(AnalysisStatus.Running);
        job.StartedAt.ShouldNotBeNull();
        (job.StartedAt!.Value - DateTimeOffset.UtcNow).Duration().ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }

    [Test]
    public void Start_WhenNotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start(); // First start is valid

        // Act & Assert
        void act() => job.Start();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldBe("Cannot start job in Running status");
    }

    [Test]
    public void Start_WhenPending_RaisesDomainEvent()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.ClearDomainEvents(); // Clear constructor event

        // Act
        job.Start();

        // Assert
        job.DomainEvents.Count.ShouldBe(1);
        job.DomainEvents.Count(e => e is AnalysisJobStartedEvent).ShouldBe(1);

        var startedEvent = job.DomainEvents.OfType<AnalysisJobStartedEvent>().Single();
        startedEvent.JobId.ShouldBe(job.Id);
    }

    [Test]
    public void Complete_WhenRunning_UpdatesStatusAndResults()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();

        // Act
        job.Complete("Test summary", "Test detailed analysis", "Test inline suggestions", "MockAnalyzer");

        // Assert
        job.Status.ShouldBe(AnalysisStatus.Completed);
        job.CompletedAt.ShouldNotBeNull();
        (job.CompletedAt!.Value - DateTimeOffset.UtcNow).Duration().ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
        job.Summary.ShouldBe("Test summary");
        job.DetailedAnalysis.ShouldBe("Test detailed analysis");
        job.InlineSuggestions.ShouldBe("Test inline suggestions");
        job.AnalyzerUsed.ShouldBe("MockAnalyzer");
    }

    [Test]
    public void Complete_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob(); // Still pending

        // Act & Assert
        void act() => job.Complete("summary", "analysis", "suggestions", "analyzer");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldBe("Cannot complete job in Pending status");
    }

    [Test]
    public void Complete_WhenRunning_RaisesDomainEvent()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.ClearDomainEvents(); // Clear previous events

        // Act
        job.Complete("Test summary", "Test detailed analysis", "Test inline suggestions", "MockAnalyzer");

        // Assert
        job.DomainEvents.Count.ShouldBe(1);
        job.DomainEvents.Count(e => e is AnalysisJobCompletedEvent).ShouldBe(1);

        var completedEvent = job.DomainEvents.OfType<AnalysisJobCompletedEvent>().Single();
        completedEvent.JobId.ShouldBe(job.Id);
        completedEvent.AnalyzerUsed.ShouldBe("MockAnalyzer");
    }

    [Test]
    public void Fail_WhenRunning_UpdatesStatusAndError()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();

        // Act
        job.Fail("Test error message");

        // Assert
        job.Status.ShouldBe(AnalysisStatus.Failed);
        job.CompletedAt.ShouldNotBeNull();
        (job.CompletedAt!.Value - DateTimeOffset.UtcNow).Duration().ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
        job.ErrorMessage.ShouldBe("Test error message");
    }

    [Test]
    public void Fail_WhenPending_UpdatesStatusAndError()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob(); // Still pending

        // Act
        job.Fail("Test error message");

        // Assert
        job.Status.ShouldBe(AnalysisStatus.Failed);
        job.CompletedAt.ShouldNotBeNull();
        job.ErrorMessage.ShouldBe("Test error message");
    }

    [Test]
    public void Fail_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.Complete("summary", "analysis", "suggestions", "analyzer");

        // Act & Assert
        void act() => job.Fail("error");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldBe("Cannot fail job in Completed status");
    }

    [Test]
    public void Fail_WhenRunning_RaisesDomainEvent()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.ClearDomainEvents(); // Clear previous events

        // Act
        job.Fail("Test error message");

        // Assert
        job.DomainEvents.Count.ShouldBe(1);
        job.DomainEvents.Count(e => e is AnalysisJobFailedEvent).ShouldBe(1);

        var failedEvent = job.DomainEvents.OfType<AnalysisJobFailedEvent>().Single();
        failedEvent.JobId.ShouldBe(job.Id);
        failedEvent.ErrorMessage.ShouldBe("Test error message");
    }

    [Test]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.DomainEvents.Count.ShouldBeGreaterThan(0);

        // Act
        job.ClearDomainEvents();

        // Assert
        job.DomainEvents.Count.ShouldBe(0);
    }

    [Test]
    public void RemoveDomainEvent_RemovesSpecificEvent()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        var createdEvent = job.DomainEvents.OfType<AnalysisJobCreatedEvent>().Single();

        // Act
        job.RemoveDomainEvent(createdEvent);

        // Assert
        job.DomainEvents.ShouldNotContain(createdEvent);
    }
}
