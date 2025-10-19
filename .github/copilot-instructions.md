# GitHub Copilot Instructions for devops-pr-analyzer

## Project Overview

This is a DevOps Pull Request Analyzer tool that performs static code analysis on pull requests from GitHub and Azure DevOps. The solution consists of multiple projects that work together to analyze code changes, extract diagnostics, and provide feedback through an API.

## Solution Structure

### Core Projects

- **devops-pr-analyzer.cli**: Command-line interface for running static code analysis locally or in CI/CD pipelines
- **devops-pr-analyzer.api**: ASP.NET Core Web API that handles analysis requests and integrates with DevOps platforms
- **devops-pr-analyzer.shared**: Shared models and contracts used across all projects
- **devops-pr-analyzer.ServiceDefaults**: Service defaults for Aspire-based configuration
- **devops-pr-analyzer.AppHost**: Aspire app host for orchestration

### Test Projects

- **devops-pr-analyzer.cli.unittests**: Unit tests for the CLI project using NUnit

## Technology Stack

- **.NET Version**: .NET 10.0
- **C# Version**: 14.0
- **Key Technologies**:
  - Roslyn (Microsoft.CodeAnalysis) for code analysis
  - Microsoft SemanticKernel for AI integration
  - Azure Service Bus for messaging
  - System.CommandLine for CLI
  - ASP.NET Core for API
  - NUnit for testing with FluentAssertions

## Code Style and Conventions

### General Guidelines

1. **Nullable Reference Types**: Enabled across all projects - always use nullable annotations appropriately
2. **Implicit Usings**: Enabled - avoid redundant using statements that are globally imported
3. **File-Scoped Namespaces**: Use file-scoped namespaces (`namespace X;` instead of `namespace X { }`)
4. **Sealed Classes**: Prefer `sealed` for classes not intended for inheritance (see `GitHubInfoExtractor`)
5. **Records**: Use records for immutable data structures (see `GitInfo`)
6. **Access Modifiers**: Use `internal` for implementation classes, public for APIs

### Naming Conventions

- **Projects**: Use kebab-case with dots (e.g., `devops-pr-analyzer.cli`)
- **Namespaces**: Use underscores to match project names (e.g., `devops_pr_analyzer.cli`)
- **Enums**: Prefix with 'E' (e.g., `EGitProvider`, `EProgrammingLanguage`)
- **Interfaces**: Prefix with 'I' (e.g., `IGitInfoExtractor`, `ICodeAnalyzer`)
- **Files**: Match the primary type name

### Code Organization

- Place services in `Services/` folder with subdirectories for domain areas (e.g., `Services/Git/`)
- Place interfaces in `Interfaces/` folder
- Place models in `Models/` folder (in shared project)
- Place extensions in `Extensions/` folder
- Place commands in `Commands/` folder (CLI project)

### Patterns and Practices

1. **Dependency Injection**: Use constructor injection for dependencies
2. **Factory Pattern**: Use factories for creating instances based on configuration (see `GitInfoExtractorFactory`)
3. **Static Methods**: Use for utility functions that don't require state (e.g., `Env()`, `ExtractPullRequestNumber()`)
4. **Early Returns**: Return `null` or throw early for invalid states
5. **String Comparisons**: Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons
6. **Collection Initialization**: Use collection expressions `[]` instead of `new List<>()`

## Testing Guidelines

- **Test Framework**: NUnit
- **Assertion Library**: FluentAssertions
- **Test Naming**: Use descriptive method names that explain the scenario
- **Test Organization**: Group related tests in the same class
- **InternalsVisibleTo**: The CLI project exposes internals to the test project

## CI/CD Integration

The CLI tool is designed to run in CI/CD pipelines:

### GitHub Actions

The tool extracts information from these environment variables:
- `GITHUB_REF`: Format `refs/pull/{pr_number}/merge` or `refs/pull/{pr_number}/head`
- `GITHUB_SHA`: Commit SHA
- `GITHUB_REPOSITORY`: Repository name (owner/repo)

### Azure DevOps

The tool supports Azure DevOps through:
- `AzureDevOpsInfoExtractor` for extracting pipeline information
- Integration with Azure Service Bus for messaging
- TFS/Azure DevOps client libraries

## API Development

When working on the API project:

1. Use ASP.NET Core minimal APIs or controllers as appropriate
2. Leverage `Microsoft.SemanticKernel` for AI-powered analysis features
3. Use Azure Service Bus for asynchronous processing
4. Enable OpenAPI/Swagger documentation
5. Follow REST conventions

## Roslyn Analysis

When working with code analysis:

1. Use `Microsoft.CodeAnalysis.CSharp.Workspaces` for syntax analysis
2. Use `Microsoft.Build.Locator` to locate MSBuild
3. Parse SARIF output for analyzer results (see `SarifParser`)
4. Support multiple programming languages through `EProgrammingLanguage` enum
5. Use `LanguageMapper` for language detection

## Important Notes

1. **AnalysisMode**: The CLI project has `AllEnabledByDefault` analysis mode - ensure code meets strict analyzer rules
2. **InvariantGlobalization**: Enabled in CLI - avoid culture-specific operations
3. **AOT Compilation**: Commented out but considered - write AOT-friendly code
4. **Docker**: API project supports Docker with Linux target OS
5. **User Secrets**: Both API and AppHost have user secrets configured

## Common Tasks

### Adding a New Git Provider

1. Create a new extractor in `Services/Git/` implementing `IGitInfoExtractor`
2. Add provider enum value to `EGitProvider`
3. Update `GitInfoExtractorFactory` to handle the new provider
4. Add environment variable extraction logic
5. Add unit tests

### Adding a New Language Analyzer

1. Create analyzer in `Services/Analyzers/{Language}/` implementing `ICodeAnalyzer`
2. Add language to `EProgrammingLanguage` enum
3. Update `LanguageMapper` for file extension mapping
4. Update `LanguageAnalysisOrchestrator` to handle the new language
5. Add integration tests

### Adding New Models

1. Place in `devops-pr-analyzer.shared/Models/`
2. Use records for immutable data
3. Use classes for mutable data with init-only properties where appropriate
4. Add XML documentation for public APIs

## Performance Considerations

- The test project copies analyzer DLLs during build (see custom MSBuild target)
- Use `CopyLocalLockFileAssemblies` in API project for proper assembly loading
- Consider memory usage when analyzing large repositories

## When Suggesting Code

1. Always use .NET 10 and C# 14 features
2. Respect existing patterns (sealed classes, file-scoped namespaces, etc.)
3. Use nullable reference types correctly
4. Follow the established folder structure
5. Add appropriate error handling
6. Consider testability in design
7. Use descriptive variable and parameter names
8. Add comments for complex logic, especially in analysis code
9. Prefer expression-bodied members for simple properties and methods
10. Use primary constructors where appropriate (C# 12+)

## Resources

- [Microsoft.CodeAnalysis Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
