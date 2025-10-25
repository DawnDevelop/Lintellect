using devops_pr_analyzer.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace devops_pr_analyzer.api.unittests.Application.Behaviors;

[TestFixture]
public class ValidationBehaviorTests
{
    private Mock<IEnumerable<IValidator<TestRequest>>> _mockValidators = null!;
    private Mock<ILogger<TestRequest>> _mockLogger = null!;
    private ValidationBehavior<TestRequest, TestResponse> _behavior = null!;

    [SetUp]
    public void SetUp()
    {
        _mockValidators = new Mock<IEnumerable<IValidator<TestRequest>>>();
        _mockLogger = new Mock<ILogger<TestRequest>>();
        _behavior = new ValidationBehavior<TestRequest, TestResponse>(
            _mockValidators.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var cancellationToken = CancellationToken.None;
        var nextCalled = false;
        var expectedResponse = new TestResponse { Result = "success" };

        _mockValidators.Setup(v => v.GetEnumerator())
            .Returns(new List<IValidator<TestRequest>>().GetEnumerator());

        MessageHandlerDelegate<TestRequest, TestResponse> next = (req, ct) =>
        {
            nextCalled = true;
            return ValueTask.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, cancellationToken);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);
    }

    [Test]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var cancellationToken = CancellationToken.None;
        var nextCalled = false;
        var expectedResponse = new TestResponse { Result = "success" };

        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockValidators.Setup(v => v.GetEnumerator())
            .Returns(new List<IValidator<TestRequest>> { validator.Object }.GetEnumerator());

        MessageHandlerDelegate<TestRequest, TestResponse> next = (req, ct) =>
        {
            nextCalled = true;
            return ValueTask.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, cancellationToken);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);
    }

    [Test]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var request = new TestRequest { Value = "" };
        var cancellationToken = CancellationToken.None;
        var nextCalled = false;

        var validator = new Mock<IValidator<TestRequest>>();
        var validationResult = new ValidationResult();
        validationResult.Errors.Add(new ValidationFailure("Value", "Value is required"));

        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
            .ReturnsAsync(validationResult);

        _mockValidators.Setup(v => v.GetEnumerator())
            .Returns(new List<IValidator<TestRequest>> { validator.Object }.GetEnumerator());

        MessageHandlerDelegate<TestRequest, TestResponse> next = (req, ct) =>
        {
            nextCalled = true;
            return ValueTask.FromResult(new TestResponse());
        };

        // Act & Assert
        var act = async () => await _behavior.Handle(request, next, cancellationToken);
        await act.Should().ThrowAsync<devops_pr_analyzer.Application.Common.Exceptions.ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Test]
    public async Task Handle_WithMultipleValidators_ValidatesAll()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var cancellationToken = CancellationToken.None;

        var validator1 = new Mock<IValidator<TestRequest>>();
        var validator2 = new Mock<IValidator<TestRequest>>();

        validator1.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult());
        validator2.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockValidators.Setup(v => v.GetEnumerator())
            .Returns(new List<IValidator<TestRequest>> { validator1.Object, validator2.Object }.GetEnumerator());

        MessageHandlerDelegate<TestRequest, TestResponse> next = (req, ct) =>
            ValueTask.FromResult(new TestResponse());

        // Act
        await _behavior.Handle(request, next, cancellationToken);

        // Assert
        validator1.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken), Times.Once);
        validator2.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken), Times.Once);
    }
}

// Test classes for the behavior tests
public record TestRequest : IRequest<TestResponse>
{
    public string Value { get; init; } = string.Empty;
}

public record TestResponse
{
    public string Result { get; init; } = string.Empty;
}
