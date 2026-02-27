# Changelog

All notable changes to Lintellect will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [API v0.0.15] - 2026-02-27

### Added

- Global Cap for comments

### Changed

- TBD

### Fixed

- TBD

### Security

- Updated Dependencies

## [API v0.0.14] - 2025-11-18

### Added

- TBD

### Changed

- TBD

### Fixed

- Fixed Mentions of bot

### Security

- TBD

## [API v0.0.13] - 2025-11-18

### Added

- Enhanced webhook comment processing for Azure DevOps PRs with question answering
- Thread context support for webhook comment responses

### Changed

- Simplified analyzer service architecture by removing IAnalyzerServiceResolver
- Updated prompt templates for better AI responses

### Fixed

- Docker build pipeline

## [API v0.0.11] - 2025-11-17

### Fixed

- Fixed GitHub Actions workflow permissions for pushing Docker images to GHCR and creating releases

## [API v0.0.10] - 2025-11-17

### Added

- DELETE endpoint for analysis history (`/api/analysis/history`) to allow deletion of analysis jobs by job ID
- Deep copy snapshot method for AnalysisRequest to prevent mutation during background processing

### Changed

- **BREAKING**: Removed credential fields (`AccessToken`, `AzureDevOpsOrgUrl`) from `AnalysisRequest` model. Credentials are now only configured at the application level via configuration or environment variables
- **BREAKING**: Changed `AnalysisRequest` storage from `JsonDocument` to EF Core owned entity for better type safety and queryability
- Improved exception handling with comprehensive error responses for unhandled exceptions
- Updated Claude analyzer default settings: `MaxTokens` increased to 40960, `Temperature` set to 0.5
- Removed custom instructions placeholder (`{{customInstructions}}`) from all prompt templates
- Simplified `IMcpServiceResolver` interface by removing `GetAvailableMcpServices()` method
- Updated validation to remove checks for removed credential fields
- Improved query performance by using direct property access instead of JSON path queries

### Fixed

- Fixed exception handler to properly handle all exceptions, not just registered types
- Fixed analysis request snapshot creation to prevent shared reference mutations
- Fixed query filtering to use strongly-typed properties instead of JSON navigation

### Removed

- `ICredentialResolver` interface and `CredentialResolver` implementation (credentials now resolved from configuration only)
- `SanitizedAnalysisRequest` class (no longer needed with owned entity approach)
- `AnalysisPromptBuilder` class (renamed to `PromptBuilder`)
- Per-request credential override support (credentials must be configured at application level)

## [CLI v0.0.11] - 2025-11-17

### Changed

- Version bump for compatibility with API v0.0.10

## [CLI v0.0.10] - 2025-10-28

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [CLI v0.0.9] - 2025-10-28

## [API v0.0.9] - 2025-10-28

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [CLI v0.0.8] - 2025-10-27

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [API v0.0.7] - 2025-10-27

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [CLI v0.0.7] - 2025-10-27

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [API v0.0.6] - 2025-10-27

### Added

- TBD

### Changed

- TBD

### Fixed

- TBD

### Security

- TBD

## [CLI v0.0.6] - 2025-10-27

### Changed

- Updated commands to match proper patterns

## [CLI v0.0.5] - 2025-10-27

### Added

- semgrep
- added static csharp analyzer

### Changed

- Removed CodeQL because of licensing issues

### Fixed

- cli.csproj

## [CLI v0.0.4] - 2025-10-27

### Added

- CodeQL Analyzers
- More Tests

### Changed

### Fixed

### Security

### API

- No unreleased API changes

### CLI

- No unreleased CLI changes

## [API v0.0.3] - 2024-01-15

### Added

- Initial API release
- REST API endpoints for analysis job management
- Background job processing with queue
- Docker container support
- PostgreSQL database integration
- Health check endpoints
- OpenAPI documentation
- Authentication with API keys
- Integration with Claude AI analyzer
- Integration with Azure AI Foundry
- GitHub and Azure DevOps integration
- Comprehensive error handling
- Structured logging
- Metrics and telemetry

### Security

- API key authentication
- Input validation and sanitization
- SQL injection protection
- XSS protection

## [CLI v0.0.3] - 2024-01-15

### Added

- Initial CLI release
- C# static analysis using Roslyn
- Command-line interface with System.CommandLine
- Support for .sln and .slnx files
- File exclusion patterns
- API integration for AI analysis
- Multiple output formats
- Progress indicators
- Configuration file support
- Cross-platform support (Windows, Linux, macOS)
- NuGet package distribution

### Security

- Secure credential handling
- Input validation
- Safe file path handling

---

## Release Notes Format

### API Release Notes

```markdown
## [API vX.Y.Z] - YYYY-MM-DD

### Added

- New features

### Changed

- Changes to existing functionality

### Deprecated

- Soon-to-be removed features

### Removed

- Removed features

### Fixed

- Bug fixes

### Security

- Security improvements
```

### CLI Release Notes

```markdown
## [CLI vX.Y.Z] - YYYY-MM-DD

### Added

- New features

### Changed

- Changes to existing functionality

### Deprecated

- Soon-to-be removed features

### Removed

- Removed features

### Fixed

- Bug fixes

### Security

- Security improvements
```

## Versioning

- **API**: Independent versioning with `api/vX.Y.Z` tags
- **CLI**: Independent versioning with `cli/vX.Y.Z` tags
- **Shared**: Version follows API releases

## Breaking Changes

Breaking changes are clearly marked and include migration instructions when applicable.

## Migration Guides

Migration guides are provided for major version changes that include breaking changes.
