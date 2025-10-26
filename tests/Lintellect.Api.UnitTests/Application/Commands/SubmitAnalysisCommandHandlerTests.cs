using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.UnitTests.Application.Commands;

[TestFixture]
public class SubmitAnalysisCommandHandlerTests
{
    private Mock<IApplicationDbContext> _mockContext = null!;
    private AnalysisJobQueue _queue = null!;
    private SubmitAnalysisCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        var mockDbSet = new Mock<DbSet<AnalysisJob>>();
        _mockContext.Setup(c => c.AnalysisJobs).Returns(mockDbSet.Object);
        _queue = new AnalysisJobQueue();
        _handler = new SubmitAnalysisCommandHandler(_mockContext.Object, _queue);
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

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);

        _mockContext.Verify(c => c.AnalysisJobs.Add(It.Is<AnalysisJob>(j =>
            j.Status == AnalysisStatus.Pending &&
            j.AnalysisRequest != null)), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(cancellationToken), Times.Once);

        // Verify that the job was enqueued by checking if we can dequeue it
        var dequeuedJob = await _queue.DequeueAsync(cancellationToken);
        dequeuedJob.ShouldNotBeNull();
        dequeuedJob.Id.ShouldBe(result);
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

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        Guid.TryParse(result.ToString(), out _).ShouldBeTrue();
    }
}
