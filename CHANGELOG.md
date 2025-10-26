# Changelog

All notable changes to Lintellect will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
