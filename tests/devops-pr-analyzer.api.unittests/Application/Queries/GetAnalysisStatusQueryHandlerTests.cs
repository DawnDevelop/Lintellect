using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace devops_pr_analyzer.api.unittests.Application.Queries;

[TestFixture]
public class GetAnalysisStatusQueryHandlerTests
{
    private Mock<IApplicationDbContext> _mockContext = null!;
    private Mock<DbSet<AnalysisJob>> _mockDbSet = null!;
    private GetAnalysisStatusQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockDbSet = new Mock<DbSet<AnalysisJob>>();
        _mockContext.Setup(c => c.AnalysisJobs).Returns(_mockDbSet.Object);
        _handler = new GetAnalysisStatusQueryHandler(_mockContext.Object);
    }

    [Test]
    public async Task Handle_WithExistingJob_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = AnalysisJobBuilder.ValidJob();
        var query = new GetAnalysisStatusQuery(jobId);
        var cancellationToken = CancellationToken.None;

        _mockDbSet.Setup(s => s.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<AnalysisJob, bool>>>(),
                cancellationToken))
            .ReturnsAsync(job);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(job);
    }

    [Test]
    public async Task Handle_WithNonExistentJob_ReturnsNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var query = new GetAnalysisStatusQuery(jobId);
        var cancellationToken = CancellationToken.None;

        _mockDbSet.Setup(s => s.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<AnalysisJob, bool>>>(),
                cancellationToken))
            .ReturnsAsync((AnalysisJob?)null);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeNull();
    }
}
