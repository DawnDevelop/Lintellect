namespace devops_pr_analyzer.api.unittests.Domain;

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
        job.Status.Should().Be(AnalysisStatus.Pending);
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.AnalysisRequest.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithValidRequest_RaisesDomainEvent()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();

        // Act
        var job = new AnalysisJob(request);

        // Assert
        job.DomainEvents.Should().HaveCount(1);
        job.DomainEvents.Should().ContainSingle(e => e is AnalysisJobCreatedEvent);

        var createdEvent = job.DomainEvents.OfType<AnalysisJobCreatedEvent>().Single();
        createdEvent.JobId.Should().Be(job.Id);
        createdEvent.ProjectName.Should().Be("TestProject");
        createdEvent.RepositoryName.Should().Be("TestRepo");
        createdEvent.PullRequestId.Should().Be(123);
    }

    [Test]
    public void Start_WhenPending_UpdatesStatusAndStartTime()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();

        // Act
        job.Start();

        // Assert
        job.Status.Should().Be(AnalysisStatus.Running);
        job.StartedAt.Should().NotBeNull();
        job.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void Start_WhenNotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start(); // First start is valid

        // Act & Assert
        var act = () => job.Start();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot start job in Running status");
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
        job.DomainEvents.Should().HaveCount(1);
        job.DomainEvents.Should().ContainSingle(e => e is AnalysisJobStartedEvent);

        var startedEvent = job.DomainEvents.OfType<AnalysisJobStartedEvent>().Single();
        startedEvent.JobId.Should().Be(job.Id);
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
        job.Status.Should().Be(AnalysisStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        job.Summary.Should().Be("Test summary");
        job.DetailedAnalysis.Should().Be("Test detailed analysis");
        job.InlineSuggestions.Should().Be("Test inline suggestions");
        job.AnalyzerUsed.Should().Be("MockAnalyzer");
    }

    [Test]
    public void Complete_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob(); // Still pending

        // Act & Assert
        var act = () => job.Complete("summary", "analysis", "suggestions", "analyzer");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete job in Pending status");
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
        job.DomainEvents.Should().HaveCount(1);
        job.DomainEvents.Should().ContainSingle(e => e is AnalysisJobCompletedEvent);

        var completedEvent = job.DomainEvents.OfType<AnalysisJobCompletedEvent>().Single();
        completedEvent.JobId.Should().Be(job.Id);
        completedEvent.AnalyzerUsed.Should().Be("MockAnalyzer");
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
        job.Status.Should().Be(AnalysisStatus.Failed);
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        job.ErrorMessage.Should().Be("Test error message");
    }

    [Test]
    public void Fail_WhenPending_UpdatesStatusAndError()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob(); // Still pending

        // Act
        job.Fail("Test error message");

        // Assert
        job.Status.Should().Be(AnalysisStatus.Failed);
        job.CompletedAt.Should().NotBeNull();
        job.ErrorMessage.Should().Be("Test error message");
    }

    [Test]
    public void Fail_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.Complete("summary", "analysis", "suggestions", "analyzer");

        // Act & Assert
        var act = () => job.Fail("error");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot fail job in Completed status");
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
        job.DomainEvents.Should().HaveCount(1);
        job.DomainEvents.Should().ContainSingle(e => e is AnalysisJobFailedEvent);

        var failedEvent = job.DomainEvents.OfType<AnalysisJobFailedEvent>().Single();
        failedEvent.JobId.Should().Be(job.Id);
        failedEvent.ErrorMessage.Should().Be("Test error message");
    }

    [Test]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var job = AnalysisJobBuilder.ValidJob();
        job.Start();
        job.DomainEvents.Should().NotBeEmpty();

        // Act
        job.ClearDomainEvents();

        // Assert
        job.DomainEvents.Should().BeEmpty();
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
        job.DomainEvents.Should().NotContain(createdEvent);
    }
}
