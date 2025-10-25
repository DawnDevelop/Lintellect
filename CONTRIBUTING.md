# Contributing to DevOps PR Analyzer

Thank you for your interest in contributing to DevOps PR Analyzer! This document provides guidelines for contributing to the project.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How to Contribute

### Reporting Issues

1. **Check Existing Issues**: Search existing issues before creating new ones
2. **Use Issue Templates**: Use the provided issue templates for bugs and feature requests
3. **Provide Details**: Include steps to reproduce, expected behavior, and actual behavior
4. **Include Environment**: Specify OS, .NET version, and other relevant details

### Suggesting Enhancements

1. **Check Roadmap**: Review the project roadmap before suggesting features
2. **Provide Use Cases**: Explain why the enhancement would be valuable
3. **Consider Implementation**: Think about how the feature could be implemented
4. **Discuss First**: Consider discussing major features in issues before implementing

### Pull Request Process

1. **Fork Repository**: Fork the repository to your GitHub account
2. **Create Branch**: Create a feature branch from `main`
3. **Make Changes**: Implement your changes following coding standards
4. **Add Tests**: Include tests for new functionality
5. **Update Documentation**: Update relevant documentation
6. **Submit PR**: Create a pull request with a clear description

## Development Setup

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022 or VS Code with C# extension
- PostgreSQL 15+
- Git
- Docker (optional)

### Getting Started

1. **Fork and Clone**

   ```bash
   git clone https://github.com/your-username/devops-pr-analyzer.git
   cd devops-pr-analyzer
   ```

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Setup Database**

   ```bash
   # Using Docker
   docker run --name postgres-dev -e POSTGRES_PASSWORD=password -p 5432:5432 -d postgres:15

   # Run migrations
   cd src/API
   dotnet ef database update
   ```

4. **Run Tests**

   ```bash
   dotnet test
   ```

5. **Start Development Server**
   ```bash
   dotnet run --project src/API
   ```

## Coding Standards

### C# Conventions

- Follow Microsoft C# coding conventions
- Use `var` for local variables when type is obvious
- Use `readonly` for immutable fields
- Prefer expression-bodied members for simple implementations
- Use primary constructors for simple classes
- Follow async/await patterns consistently

### Naming Conventions

- **Classes**: PascalCase (e.g., `AnalysisJob`)
- **Methods**: PascalCase (e.g., `ProcessJobAsync`)
- **Properties**: PascalCase (e.g., `JobId`)
- **Fields**: camelCase with underscore prefix (e.g., `_logger`)
- **Constants**: PascalCase (e.g., `MaxRetryAttempts`)
- **Namespaces**: Follow folder structure

### File Organization

- One class per file
- Use namespaces that match folder structure
- Group related classes in same namespace
- Use partial classes for code generation

### Code Quality

- Write clean, readable code
- Add XML documentation for public APIs
- Use meaningful variable and method names
- Keep methods focused and small
- Avoid deep nesting and complex conditionals

## Testing Requirements

### Unit Tests

- Write unit tests for all business logic
- Mock external dependencies
- Use FluentAssertions for readable assertions
- Aim for high code coverage (80%+)
- Test edge cases and error conditions

### Integration Tests

- Write integration tests for API endpoints
- Use Testcontainers for database testing
- Test complete request/response cycles
- Verify external service integrations

### Test Structure

```csharp
[TestFixture]
public class AnalysisJobTests
{
    [SetUp]
    public void Setup()
    {
        // Arrange
    }

    [Test]
    public void ProcessJob_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

## Documentation Requirements

### Code Documentation

- Add XML documentation for all public APIs
- Include parameter descriptions
- Document return values and exceptions
- Provide usage examples where helpful

### README Updates

- Update README.md for significant changes
- Include new features in documentation
- Update installation and usage instructions
- Add troubleshooting information

### API Documentation

- Update API.md for endpoint changes
- Include request/response examples
- Document new parameters and options
- Update error response documentation

## Pull Request Guidelines

### Before Submitting

1. **Run Tests**: Ensure all tests pass

   ```bash
   dotnet test
   ```

2. **Check Code Style**: Run code analysis

   ```bash
   dotnet build --verbosity normal
   ```

3. **Update Documentation**: Update relevant documentation
4. **Test Manually**: Test your changes thoroughly
5. **Squash Commits**: Clean up commit history

### PR Description

Include the following in your PR description:

1. **Summary**: Brief description of changes
2. **Type**: Bug fix, feature, documentation, etc.
3. **Testing**: How you tested the changes
4. **Breaking Changes**: Any breaking changes
5. **Related Issues**: Link to related issues

### PR Template

```markdown
## Description

Brief description of changes

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Breaking Changes

- [ ] No breaking changes
- [ ] Breaking changes documented

## Related Issues

Fixes #(issue number)
```

## Review Process

### For Contributors

1. **Address Feedback**: Respond to review comments
2. **Make Changes**: Update code based on feedback
3. **Test Again**: Re-test after making changes
4. **Update PR**: Push changes to your branch

### For Reviewers

1. **Check Code Quality**: Review code style and patterns
2. **Verify Tests**: Ensure tests are adequate
3. **Test Functionality**: Test the changes manually
4. **Provide Feedback**: Give constructive feedback
5. **Approve**: Approve when ready to merge

## Release Process

### Version Numbering

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

### Release Checklist

- [ ] All tests pass
- [ ] Documentation updated
- [ ] Changelog updated
- [ ] Version numbers updated
- [ ] Release notes prepared
- [ ] Tag created
- [ ] Release published

## Community Guidelines

### Communication

- Be respectful and constructive
- Use clear and concise language
- Provide context for questions
- Help others when possible

### Issue Discussions

- Keep discussions focused
- Provide relevant information
- Be patient with responses
- Follow up on resolved issues

### Code Reviews

- Be constructive and helpful
- Focus on code quality
- Explain reasoning for suggestions
- Be open to different approaches

## Getting Help

### Documentation

- Check existing documentation first
- Look for similar issues or discussions
- Review code examples and samples

### Community Support

- GitHub Discussions for questions
- GitHub Issues for bugs and features
- Pull requests for code contributions

### Contact Maintainers

- Use GitHub issues for project-related questions
- Use discussions for general questions
- Follow the code of conduct

## Recognition

Contributors will be recognized in:

- CHANGELOG.md
- README.md contributors section
- Release notes
- Project documentation

Thank you for contributing to DevOps PR Analyzer!
