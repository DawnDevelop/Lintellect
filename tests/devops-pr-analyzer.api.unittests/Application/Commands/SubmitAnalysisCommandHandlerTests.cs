namespace devops_pr_analyzer.api.unittests.Application.Commands;

[TestFixture]
public class SubmitAnalysisCommandHandlerTests
{
    private Mock<IApplicationDbContext> _mockContext = null!;
    private Mock<AnalysisJobQueue> _mockQueue = null!;
    private SubmitAnalysisCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockQueue = new Mock<AnalysisJobQueue>();
        _handler = new SubmitAnalysisCommandHandler(_mockContext.Object, _mockQueue.Object);
    }

    [Test]
    public async Task Handle_WithValidCommand_CreatesJobAndEnqueues()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockContext.Setup(c => c.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);
        _mockQueue.Setup(q => q.EnqueueAsync(It.IsAny<AnalysisJob>(), cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeEmpty();

        _mockContext.Verify(c => c.AnalysisJobs.Add(It.Is<AnalysisJob>(j =>
            j.Status == AnalysisStatus.Pending &&
            j.AnalysisRequest != null)), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(cancellationToken), Times.Once);
        _mockQueue.Verify(q => q.EnqueueAsync(It.IsAny<AnalysisJob>(), cancellationToken), Times.Once);
    }

    [Test]
    public async Task Handle_WithValidCommand_ReturnsJobId()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockContext.Setup(c => c.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);
        _mockQueue.Setup(q => q.EnqueueAsync(It.IsAny<AnalysisJob>(), cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeEmpty();
        Guid.TryParse(result.ToString(), out _).Should().BeTrue();
    }
}
