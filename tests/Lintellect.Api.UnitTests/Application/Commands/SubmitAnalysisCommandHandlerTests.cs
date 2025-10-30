using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.UnitTests.Application.Commands;

[TestFixture]
public class SubmitAnalysisCommandHandlerTests
{
    private IApplicationDbContext _mockContext = null!;
    private AnalysisJobQueue _queue = null!;
    private SubmitAnalysisCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = Substitute.For<IApplicationDbContext>();
        var mockDbSet = Substitute.For<DbSet<AnalysisJob>>();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        _queue = new AnalysisJobQueue();
        _handler = new SubmitAnalysisCommandHandler(_mockContext, _queue);
    }

    [Test]
    public async Task Handle_WithValidCommand_CreatesJobAndEnqueues()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockContext.SaveChangesAsync(cancellationToken)
            .Returns(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);

        _mockContext.Received(1).AnalysisJobs.Add(Arg.Is<AnalysisJob>(j =>
            j.Status == AnalysisStatus.Pending &&
            j.AnalysisRequest != null));
        await _mockContext.Received(1).SaveChangesAsync(cancellationToken);

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

        _mockContext.SaveChangesAsync(cancellationToken)
            .Returns(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        Guid.TryParse(result.ToString(), out _).ShouldBeTrue();
    }
}
