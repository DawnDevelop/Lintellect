using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Application.Models;
using FluentValidation.TestHelper;

namespace devops_pr_analyzer.api.unittests.Application.Validators;

[TestFixture]
public class SubmitAnalysisCommandValidatorTests
{
    private Mock<IGitClientFactory> _mockGitClientFactory = null!;
    private Mock<IGitClient> _mockGitClient = null!;
    private SubmitAnalysisCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockGitClientFactory = new Mock<IGitClientFactory>();
        _mockGitClient = new Mock<IGitClient>();
        _validator = new SubmitAnalysisCommandValidator(_mockGitClientFactory.Object);
    }

    [Test]
    public void Validate_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.Setup(f => f.CreateClient(It.IsAny<AnalysisRequest>()))
            .Returns(_mockGitClient.Object);

        _mockGitClient.Setup(c => c.HasSufficientPermissionsAsync(It.IsAny<AnalysisRequest>()))
            .ReturnsAsync([new(true)]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithNullAnalysisRequest_ReturnsError()
    {
        // Arrange
        var command = new SubmitAnalysisCommand(null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("Analysis request is required.");
    }

    [Test]
    public void Validate_WithNullGitInfo_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = null;
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo)
            .WithErrorMessage("Git information is required.");
    }

    [Test]
    public void Validate_WithEmptyProjectName_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { ProjectName = "" };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.ProjectName)
            .WithErrorMessage("Project name is required and must not exceed 255 characters.");
    }

    [Test]
    public void Validate_WithEmptyRepositoryName_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { RepositoryName = "" };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.RepositoryName)
            .WithErrorMessage("Repository name is required and must not exceed 255 characters.");
    }

    [Test]
    public void Validate_WithZeroPullRequestId_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { PullRequestId = 0 };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.PullRequestId)
            .WithErrorMessage("Pull request ID must be a valid positive integer.");
    }

    [Test]
    public void Validate_WithNoCredentials_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitHubToken = null;
        request.DevopsPat = null;
        request.AzureDevOpsOrgUrl = null;
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.Setup(f => f.CreateClient(It.IsAny<AnalysisRequest>()))
            .Returns(_mockGitClient.Object);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("No Git provider credentials provided. Please provide either Azure DevOps PAT and Org URL, or GitHub token.");
    }

    [Test]
    public void Validate_WithNoFeaturesEnabled_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableSummaryComment = false;
        request.EnableDescriptionSummary = false;
        request.EnableInlineSuggestions = false;
        request.EnableAzureDevopsCodeOwners = false;
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.Setup(f => f.CreateClient(It.IsAny<AnalysisRequest>()))
            .Returns(_mockGitClient.Object);

        _mockGitClient.Setup(c => c.HasSufficientPermissionsAsync(It.IsAny<AnalysisRequest>()))
            .ReturnsAsync(new List<CheckPermissionResult> { new(true) });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("At least one AI analysis feature must be enabled (EnableAzureDevopsCodeOwners, EnableSummaryComment, EnableDescriptionSummary, or EnableInlineSuggestions).");
    }

    [Test]
    public void Validate_WithInvalidAzureDevOpsUrl_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitHubToken = null;
        request.DevopsPat = "test-pat";
        request.AzureDevOpsOrgUrl = "invalid-url";
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.AzureDevOpsOrgUrl)
            .WithErrorMessage("AzureDevOpsOrgUrl must be a valid absolute URI (e.g., https://dev.azure.com/yourorg).");
    }

    [Test]
    public void Validate_WithTooManyFileExclusions_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.FileExclusions = Enumerable.Range(1, 51).Select(i => $"file{i}.cs").ToList();
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.FileExclusions)
            .WithErrorMessage("File exclusions must not exceed 50 items.");
    }
}
