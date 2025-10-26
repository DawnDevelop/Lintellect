# GitHub Copilot Instructions for Lintellect

## Architecture Overview

This is a **multi-project .NET 10.0 solution** for AI-powered static code analysis on pull requests, designed to run in CI/CD pipelines (GitHub Actions, Azure DevOps) or via API. The architecture follows **Clean Architecture** and **CQRS** patterns with **separation of concerns**:

- **CLI** (`Lintellect.Cli`): Stateless analyzer that runs in CI/CD, extracts PR context from environment variables, performs Roslyn-based analysis, and optionally posts results to API
- **API** (`Lintellect.Api`): ASP.NET Core service that receives analysis results, processes PR events via background services, and integrates with DevOps platforms using AI
- **Shared** (`Lintellect.Shared`): Data contracts (`AnalysisRequest`, `GitInfo`, `AnalyzerFindings`) shared between CLI and API
- **AppHost** (`Lintellect.AppHost`) & **ServiceDefaults** (`Lintellect.ServiceDefaults`): .NET Aspire orchestration for local development with OpenTelemetry, health checks, and service discovery

**Key Data Flow**: CI/CD runs CLI → Roslyn analyzes code → CLI detects Git context → Results posted to API → API processes with AI (Claude/Semantic Kernel) → DevOps integration

## Solution Structure

### Core Projects

- **Lintellect.Cli**: Command-line interface for running static code analysis locally or in CI/CD pipelines
- **Lintellect.Api**: ASP.NET Core Web API that handles analysis requests and integrates with DevOps platforms
- **Lintellect.Shared**: Shared models and contracts used across all projects
- **Lintellect.ServiceDefaults**: Service defaults for Aspire-based configuration
- **Lintellect.AppHost**: Aspire app host for orchestration

### Test Projects

- **Lintellect.Api.FunctionalTests**: Functional tests using Testcontainers, Respawn, and Shouldly
- **Lintellect.Api.UnitTests**: Unit tests using NUnit, NSubstitute, and Shouldly
- **Lintellect.Cli.UnitTests**: Unit tests for the CLI project using NUnit and Shouldly

## Technology Stack

- **.NET Version**: .NET 10.0
- **C# Version**: 14.0 (latest)
- **Key Technologies**:
  - **Roslyn** (Microsoft.CodeAnalysis.CSharp.Workspaces) for code analysis
  - **AI Integration**: Anthropic Claude API and Microsoft Semantic Kernel
  - **Database**: PostgreSQL with Entity Framework Core
  - **Messaging**: Channel-based job queue (no Azure Service Bus currently)
  - **CLI**: System.CommandLine for command-line interface
  - **API**: ASP.NET Core with minimal APIs and controllers
  - **Orchestration**: .NET Aspire for local development
  - **Testing**: NUnit, Shouldly, NSubstitute, Testcontainers, Respawn
  - **Validation**: FluentValidation
  - **CQRS**: Mediator pattern with source generators
  - **Git Integration**: Octokit (GitHub), Microsoft.TeamFoundationServer.Client (Azure DevOps)

## Code Style and Conventions

### General Guidelines

1. **Nullable Reference Types**: Enabled across all projects - always use nullable annotations appropriately
2. **Implicit Usings**: Enabled - avoid redundant using statements that are globally imported
3. **File-Scoped Namespaces**: Use file-scoped namespaces (`namespace X;` instead of `namespace X { }`)
4. **Sealed Classes**: Prefer `sealed` for classes not intended for inheritance (see `GitHubInfoExtractor`)
5. **Records**: Use records for immutable data structures (see `GitInfo`)
6. **Access Modifiers**: Use `internal` for implementation classes, public for APIs

### Naming Conventions

- **Projects**: Use PascalCase (e.g., `Lintellect.Cli`, `Lintellect.Api`)
- **Namespaces**: Use PascalCase matching project names (e.g., `Lintellect.Cli`, `Lintellect.Api`)
- **Enums**: Prefix with 'E' (e.g., `EGitProvider`, `EProgrammingLanguage`, `EAnalyzers`)
- **Interfaces**: Prefix with 'I' (e.g., `IGitInfoExtractor`, `ICodeAnalyzer`, `IAnalyzerService`)
- **Files**: Match the primary type name
- **Classes**: Use PascalCase, prefer `sealed` for non-inheritable classes

### Code Organization

- **API Project Structure**:
  - `Apis/`: Controllers and API endpoints
  - `Application/`: CQRS commands, queries, handlers, and interfaces
  - `Domain/`: Entities, events, enums, and domain logic
  - `Infrastructure/`: Services, persistence, external integrations
- **CLI Project Structure**:
  - `Commands/`: System.CommandLine command definitions
  - `Services/`: Analysis orchestrators and analyzers
  - `Interfaces/`: Service contracts
  - `Extensions/`: Utility extensions
- **Shared Project**:
  - `Models/`: Data contracts and DTOs
- **Test Projects**:
  - `Tests/`: Test classes
  - `Builders/`: Test data builders
  - `Mocks/`: Mock implementations
  - `Setup/`: Test infrastructure

### Patterns and Practices

1. **Clean Architecture**: Domain → Application → Infrastructure layers
2. **CQRS**: Commands and queries with Mediator pattern
3. **Dependency Injection**: Constructor injection for dependencies
4. **Factory Pattern**: Use factories for creating instances based on configuration (see `GitInfoExtractorFactory`, `GitClientFactory`)
5. **Repository Pattern**: Entity Framework DbContext as repository
6. **Domain Events**: BaseEntity with domain events for side effects
7. **Background Services**: Channel-based job queue for async processing
8. **Static Methods**: Use for utility functions that don't require state (e.g., `Env()`, `ExtractPullRequestNumber()`)
9. **Early Returns**: Return `null` or throw early for invalid states
10. **String Comparisons**: Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons
11. **Collection Initialization**: Use collection expressions `[]` instead of `new List<>()`
12. **Primary Constructors**: Use C# 12+ primary constructors where appropriate
13. **Sealed Classes**: Prefer sealed for classes not intended for inheritance

## Testing Guidelines

- **Test Framework**: NUnit 4.4.0
- **Assertion Libraries**: Shouldly
- **Mocking**: NSubstitute for unit tests
- **Integration Testing**: Testcontainers for PostgreSQL
- **Database Reset**: Respawn for cleaning test data
- **Test Data**: Bogus for generating test data
- **Test Naming**: Use descriptive method names that explain the scenario
- **Test Organization**: Group related tests in the same class
- **Test Categories**:
  - **Unit Tests**: Fast, isolated tests with mocks
  - **Integration Tests**: Test with real dependencies (CLI with real solution files)
  - **Functional Tests**: End-to-end API tests with test database
- **InternalsVisibleTo**: The CLI project exposes internals to the test project
- **Test Data Builders**: Use fluent builders for creating test data (see `TestDataBuilder`)

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

1. **Architecture**: Follow Clean Architecture with Domain → Application → Infrastructure layers
2. **CQRS**: Use Mediator pattern for commands and queries
3. **AI Integration**: Support both Anthropic Claude and Microsoft Semantic Kernel
4. **Background Processing**: Use Channel-based job queue for async analysis processing
5. **Database**: PostgreSQL with Entity Framework Core and JSONB for flexible data storage
6. **API Design**: Use minimal APIs and controllers as appropriate
7. **Documentation**: Scalar for OpenAPI documentation
8. **Validation**: FluentValidation for request validation
9. **Health Checks**: Comprehensive health checks for database and external services
10. **Resilience**: Polly for HTTP client resilience policies

## Roslyn Analysis

When working with code analysis:

1. **Roslyn Integration**: Use `Microsoft.CodeAnalysis.CSharp.Workspaces` for syntax analysis
2. **MSBuild Integration**: Use `Microsoft.Build.Locator` to locate MSBuild
3. **Language Support**: Currently supports C# with extensible architecture for other languages
4. **Analysis Pipeline**:
   - Load solution using MSBuildWorkspace
   - Get compilation for each project
   - Extract compiler diagnostics and analyzer results
   - Convert to standardized `AnalyzerFindings` format
5. **Language Detection**: Use `LanguageMapper` for file extension to language mapping
6. **Analysis Orchestration**: `LanguageAnalysisOrchestrator` coordinates analysis workflow

## Important Notes

1. **AnalysisMode**: The CLI project has `AllEnabledByDefault` analysis mode - ensure code meets strict analyzer rules
2. **InvariantGlobalization**: Enabled in CLI - avoid culture-specific operations
3. **AOT Compilation**: Commented out but considered - write AOT-friendly code
4. **Docker**: API project supports Docker with Linux target OS
5. **User Secrets**: Both API and AppHost have user secrets configured
6. **Package Management**: Centralized package version management via `Directory.Packages.props`
7. **AI Providers**: Support for both Anthropic Claude and Microsoft Semantic Kernel
8. **Database**: PostgreSQL with JSONB for flexible data storage
9. **Background Processing**: Channel-based job queue instead of Azure Service Bus
10. **Testing**: Comprehensive test coverage with unit, integration, and functional tests

## Common Tasks

### Adding a New Git Provider

1. Create a new extractor in `Services/Git/` implementing `IGitInfoExtractor`
2. Add provider enum value to `EGitProvider`
3. Update `GitInfoExtractorFactory` to handle the new provider
4. Add environment variable extraction logic
5. Create corresponding API client in `Infrastructure/Services/Git/`
6. Update `GitClientFactory` to support the new provider
7. Add unit tests and integration tests

### Adding a New Language Analyzer

1. Create analyzer in `Services/Analyzers/{Language}/` implementing `ICodeAnalyzer`
2. Add language to `EProgrammingLanguage` enum
3. Update `LanguageMapper` for file extension mapping
4. Update `LanguageAnalysisOrchestrator` to handle the new language
5. Add AI prompt templates in `Infrastructure/Services/AI/Prompts/Templates/{Language}/`
6. Add integration tests with sample code

### Adding New Models

1. Place in `Lintellect.Shared/Models/`
2. Use records for immutable data (e.g., `GitInfo`)
3. Use classes for mutable data with init-only properties where appropriate
4. Add XML documentation for public APIs
5. Use `JsonStringEnumConverter` for enum serialization
6. Consider nullable reference types for optional properties

## When Suggesting Code

1. **Language Features**: Always use .NET 10 and C# 14 features
2. **Architecture**: Follow Clean Architecture and CQRS patterns
3. **Code Style**: Respect existing patterns (sealed classes, file-scoped namespaces, etc.)
4. **Null Safety**: Use nullable reference types correctly
5. **Organization**: Follow the established folder structure
6. **Error Handling**: Add appropriate error handling and validation
7. **Testability**: Consider testability in design, use dependency injection
8. **Naming**: Use descriptive variable and parameter names
9. **Documentation**: Add XML documentation for public APIs
10. **Comments**: Add comments for complex logic, especially in analysis code
11. **Expressions**: Prefer expression-bodied members for simple properties and methods
12. **Constructors**: Use primary constructors where appropriate (C# 12+)
13. **Collections**: Use collection expressions `[]` instead of `new List<>()`
14. **Async/Await**: Use `ConfigureAwait(false)` in library code
15. **Disposal**: Implement proper disposal patterns for disposable resources

## Resources

- [Microsoft.CodeAnalysis Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Anthropic Claude API Documentation](https://docs.anthropic.com/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Mediator Pattern Documentation](https://github.com/martinothamar/Mediator)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [NUnit Documentation](https://docs.nunit.org/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
