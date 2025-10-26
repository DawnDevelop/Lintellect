using FluentValidation;
using FluentValidation.Results;
using Lintellect.Api.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;

namespace Lintellect.Api.UnitTests.Application.Behaviors;

[TestFixture]
public class ValidationBehaviorTests
{
    private List<IValidator<TestRequest>> _validators = null!;
    private Mock<ILogger<TestRequest>> _mockLogger = null!;
    private ValidationBehavior<TestRequest, TestResponse> _behavior = null!;

    [SetUp]
    public void SetUp()
    {
        _validators = [];
        _mockLogger = new Mock<ILogger<TestRequest>>();
        _behavior = new ValidationBehavior<TestRequest, TestResponse>(
            _validators,
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

        _validators.Clear();

        ValueTask<TestResponse> next(TestRequest req, CancellationToken ct)
        {
            nextCalled = true;
            return ValueTask.FromResult(expectedResponse);
        }

        // Act
        var result = await _behavior.Handle(request, next, cancellationToken);

        // Assert
        nextCalled.ShouldBeTrue();
        result.ShouldBe(expectedResponse);
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

        _validators.Clear();
        _validators.Add(validator.Object);

        ValueTask<TestResponse> next(TestRequest req, CancellationToken ct)
        {
            nextCalled = true;
            return ValueTask.FromResult(expectedResponse);
        }

        // Act
        var result = await _behavior.Handle(request, next, cancellationToken);

        // Assert
        nextCalled.ShouldBeTrue();
        result.ShouldBe(expectedResponse);
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

        _validators.Clear();
        _validators.Add(validator.Object);

        ValueTask<TestResponse> next(TestRequest req, CancellationToken ct)
        {
            nextCalled = true;
            return ValueTask.FromResult(new TestResponse());
        }

        // Act & Assert
        async Task<TestResponse> act()
        {
            return await _behavior.Handle(request, next, cancellationToken);
        }

        await Should.ThrowAsync<Api.Application.Common.Exceptions.ValidationException>((Func<Task<TestResponse>>)act);
        nextCalled.ShouldBeFalse();
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

        _validators.Clear();
        _validators.Add(validator1.Object);
        _validators.Add(validator2.Object);

        static ValueTask<TestResponse> next(TestRequest req, CancellationToken ct)
        {
            return ValueTask.FromResult(new TestResponse());
        }

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
