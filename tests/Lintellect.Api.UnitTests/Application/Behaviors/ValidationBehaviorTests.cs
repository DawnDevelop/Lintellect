using FluentValidation;
using FluentValidation.Results;
using Lintellect.Api.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;

namespace Lintellect.Api.UnitTests.Application.Behaviors;

[TestFixture]
public class ValidationBehaviorTests
{
    private List<IValidator<TestRequest>> _validators = null!;
    private ILogger<TestRequest> _mockLogger = null!;
    private ValidationBehavior<TestRequest, TestResponse> _behavior = null!;

    [SetUp]
    public void SetUp()
    {
        _validators = [];
        _mockLogger = Substitute.For<ILogger<TestRequest>>();
        _behavior = new ValidationBehavior<TestRequest, TestResponse>(
            _validators,
            _mockLogger);
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

        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken)
            .Returns(new ValidationResult());

        _validators.Clear();
        _validators.Add(validator);

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

        var validator = Substitute.For<IValidator<TestRequest>>();
        var validationResult = new ValidationResult();
        validationResult.Errors.Add(new ValidationFailure("Value", "Value is required"));

        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken)
            .Returns(validationResult);

        _validators.Clear();
        _validators.Add(validator);

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

        var validator1 = Substitute.For<IValidator<TestRequest>>();
        var validator2 = Substitute.For<IValidator<TestRequest>>();

        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken)
            .Returns(new ValidationResult());
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken)
            .Returns(new ValidationResult());

        _validators.Clear();
        _validators.Add(validator1);
        _validators.Add(validator2);

        static ValueTask<TestResponse> next(TestRequest req, CancellationToken ct)
        {
            return ValueTask.FromResult(new TestResponse());
        }

        // Act
        await _behavior.Handle(request, next, cancellationToken);

        // Assert
        await validator1.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken);
        await validator2.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), cancellationToken);
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
