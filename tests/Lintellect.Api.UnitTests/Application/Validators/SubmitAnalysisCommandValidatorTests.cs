using FluentValidation.TestHelper;
using Lintellect.Api.Application.Interfaces;

namespace Lintellect.Api.UnitTests.Application.Validators;

[TestFixture]
public class SubmitAnalysisCommandValidatorTests
{
    private IGitClientFactory _mockGitClientFactory = null!;
    private IGitClient _mockGitClient = null!;
    private SubmitAnalysisCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockGitClientFactory = Substitute.For<IGitClientFactory>();
        _mockGitClient = Substitute.For<IGitClient>();
        _validator = new SubmitAnalysisCommandValidator(_mockGitClientFactory);
    }

    [Test]
    public async Task Validate_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.CreateClient(Arg.Any<AnalysisRequest>())
            .Returns(_mockGitClient);

        _mockGitClient.HasSufficientPermissionsAsync(Arg.Any<AnalysisRequest>())
            .Returns([new(true)]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_WithNullAnalysisRequest_ReturnsError()
    {
        // Arrange
        var command = new SubmitAnalysisCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("Analysis request is required.");
    }

    [Test]
    public async Task Validate_WithNullGitInfo_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = null;
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo)
            .WithErrorMessage("Git information is required.");
    }

    [Test]
    public async Task Validate_WithEmptyProjectName_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { ProjectName = "" };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.ProjectName)
            .WithErrorMessage("'Analysis Request Git Info Project Name' must not be empty.");
    }

    [Test]
    public async Task Validate_WithEmptyRepositoryName_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { RepositoryName = "" };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.RepositoryName)
            .WithErrorMessage("'Analysis Request Git Info Repository Name' must not be empty.");
    }

    [Test]
    public async Task Validate_WithZeroPullRequestId_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitInfo = request.GitInfo! with { PullRequestId = 0 };
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.GitInfo!.PullRequestId)
            .WithErrorMessage("Pull request ID must be a valid positive integer.");
    }

    [Test]
    public async Task Validate_WithNoCredentials_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitHubToken = null;
        request.DevopsPat = null;
        request.AzureDevOpsOrgUrl = null;
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.CreateClient(Arg.Any<AnalysisRequest>())
            .Returns(_mockGitClient);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("No Git provider credentials provided. Please provide either Azure DevOps PAT and Org URL, or GitHub token.");
    }

    [Test]
    public async Task Validate_WithNoFeaturesEnabled_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableSummaryComment = false;
        request.EnableDescriptionSummary = false;
        request.EnableInlineSuggestions = false;
        request.EnableAzureDevopsCodeOwners = false;
        var command = new SubmitAnalysisCommand(request);

        _mockGitClientFactory.CreateClient(Arg.Any<AnalysisRequest>())
            .Returns(_mockGitClient);

        _mockGitClient.HasSufficientPermissionsAsync(Arg.Any<AnalysisRequest>())
            .Returns([new(true)]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest)
            .WithErrorMessage("At least one AI analysis feature must be enabled (EnableAzureDevopsCodeOwners, EnableSummaryComment, EnableDescriptionSummary, or EnableInlineSuggestions).");
    }

    [Test]
    public async Task Validate_WithInvalidAzureDevOpsUrl_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.GitHubToken = null;
        request.DevopsPat = "test-pat";
        request.AzureDevOpsOrgUrl = "invalid-url";
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.AzureDevOpsOrgUrl)
            .WithErrorMessage("AzureDevOpsOrgUrl must be a valid absolute URI (e.g., https://dev.azure.com/yourorg).");
    }

    [Test]
    public async Task Validate_WithTooManyFileExclusions_ReturnsError()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.FileExclusions = Enumerable.Range(1, 51).Select(i => $"file{i}.cs").ToList();
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisRequest.FileExclusions)
            .WithErrorMessage("File exclusions must not exceed 50 items.");
    }
}
