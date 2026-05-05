# Lintellect

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/your-org/lintellect/workflows/CI/badge.svg)](https://github.com/your-org/lintellect/actions)
[![API Version](https://img.shields.io/badge/API-v1.0.0-blue.svg)](https://github.com/your-org/lintellect/releases)
[![CLI Version](https://img.shields.io/badge/CLI-v1.0.0-green.svg)](https://www.nuget.org/packages/lintellect)

> **AI-powered code review assistant that enhances pull request analysis with intelligent insights and automated suggestions.**

## Features

- **AI-Powered Analysis**: Integrates with Claude and Azure AI Foundry for intelligent code review
- **Multi-Platform Support**: Works with GitHub and Azure DevOps
- **Real-time Processing**: Asynchronous job processing with background services
- **Enterprise Ready**: Clean Architecture, comprehensive testing, and production-grade features
- **Rich Analytics**: Detailed analysis reports with metrics and telemetry
- **CLI Integration**: Command-line tool for CI/CD pipeline integration
- **RESTful API**: Complete REST API with OpenAPI documentation

## Table of Contents

- [Quick Start](#quick-start)
- [Installation](#installation)
- [Usage](#usage)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [Development](#development)
- [Git Workflow](#git-workflow)
- [Release Process](#release-process)
- [Contributing](#contributing)
- [License](#license)

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Aspire orchestration)

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/lintellect.git
cd lintellect
```

### 2. Run with .NET Aspire (Recommended)

The application is designed to run with .NET Aspire for optimal development experience:

```bash
# Start the Aspire AppHost
cd src/AppHost
dotnet run
```

This will:

- Start PostgreSQL in a container
- Launch the API service
- Provide Aspire dashboard at https://localhost:15000
- Handle service discovery and configuration

### 3. Configure Environment

The Aspire AppHost will automatically configure the environment. For custom configuration, modify `src/AppHost/appsettings.json`:

```json
{
  "ClaudeAnalyzer": {
    "ApiKey": "your-claude-api-key"
  },
  "GitCredentials": {
    "GitHub": {
      "Token": "your-github-token"
    },
    "AzureDevOps": {
      "Pat": "your-pat-token",
      "OrgUrl": "https://dev.azure.com/your-org"
    }
  }
}
```

### 4. Access the Application

- **Aspire Dashboard**: https://localhost:15000
- **API**: https://localhost:7000
- **API Documentation**: https://localhost:7000/scalar-api-reference
- **Health Check**: https://localhost:7000/health

## Installation

### API Service

```bash
# Build and run
cd src/Lintellect.Api
dotnet run

# Or with Docker
docker build -t lintellect:latest -f src/Lintellect.Api/Dockerfile .
docker run -p 7000:7000 lintellect:latest
```

### CLI Tool

```bash
# Install globally
dotnet tool install --global Lintellect.Cli

# Verify installation
Lintellect --help
```

## Usage

### CLI Analysis

```bash
# Basic C# analysis with AI features (Semgrep disabled by default)
Lintellect analyze \
  --language "csharp" \
  --enable-summary-comment \
  --enable-inline-suggestions \
  --enable-description-summary

# C# analysis with Semgrep (MIT-licensed security analysis)
Lintellect analyze \
  --language "csharp" \
  --enable-semgrep \
  --enable-summary-comment \
  --enable-inline-suggestions

# C# analysis WITHOUT Semgrep (AI features only)
Lintellect analyze \
  --language "csharp" \
  --enable-semgrep false \
  --enable-summary-comment \
  --enable-inline-suggestions \
  --enable-description-summary

# Multi-language analysis with exclusions
Lintellect analyze \
  --language "csharp" \
  --exclude "**/bin/**" \
  --exclude "**/obj/**" \
  --exclude "**/test/**" \
  --exclude "**/Generated/**" \
  --enable-summary-comment \
  --enable-inline-suggestions \
  --enable-azure-devops-code-owners

# Linked work items / issues are used as PR context by default.
# Azure DevOps: linked work items resolved server-side via the WIT REST API.
# GitHub: PR body parsed for "Closes/Fixes/Resolves #N" keywords.
# To opt out:
Lintellect analyze \
  --language "csharp" \
  --enable-summary-comment \
  --enable-work-item-context false

# Python analysis with Semgrep
Lintellect analyze \
  --language "python" \
  --enable-semgrep \
  --exclude "**/__pycache__/**" \
  --exclude "**/venv/**" \
  --exclude "**/node_modules/**" \
  --enable-summary-comment

# JavaScript/TypeScript analysis
Lintellect analyze \
  --language "javascript" \
  --enable-semgrep \
  --exclude "**/node_modules/**" \
  --exclude "**/dist/**" \
  --exclude "**/build/**" \
  --enable-summary-comment \
  --enable-inline-suggestions
```

### CI/CD Integration

#### GitHub Actions

```yaml
name: PR Analysis

# Environment Variables Required:
# - LINTELLECT_API_URL: Your Lintellect API endpoint URL
# - LINTELLECT_API_KEY: Your Lintellect API key
#
# Optional Environment Variables:
# - LINTELLECT_API_URL and LINTELLECT_API_KEY can be provided via command line arguments instead

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0 # Fetch full history for better analysis

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Install DevOps PR Analyzer
        run: |
          dotnet tool install --global Lintellect

      - name: Basic C# Analysis
        run: |
          Lintellect analyze \
            --language "csharp" \
            --enable-summary-comment \
            --enable-inline-suggestions \
            --enable-description-summary
        env:
          LINTELLECT_API_URL: ${{ secrets.LINTELLECT_API_URL }}
          LINTELLECT_API_KEY: ${{ secrets.LINTELLECT_API_KEY }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

#### Azure DevOps Pipelines

```yaml
trigger: none # Trigger on every pull request by setting build validation inside the options

# Environment Variables Required:
# - LINTELLECT_API_URL: Your Lintellect API endpoint URL
# - LINTELLECT_API_KEY: Your Lintellect API key
#
# Optional Environment Variables:
# - LINTELLECT_API_URL and LINTELLECT_API_KEY can be provided via command line arguments instead
# - AZURE_DEVOPS_PAT: Azure DevOps Personal Access Token (for Azure DevOps integration)

pool:
  vmImage: "ubuntu-latest"

variables:
  buildConfiguration: "Release"

stages:
  - stage: Analyze
    displayName: "Analyze PR"
    jobs:
      - job: AnalyzePR
        displayName: "Analyze Pull Request"
        steps:
          - task: UseDotNet@2
            displayName: "Use .NET 10"
            inputs:
              packageType: "sdk"
              version: "10.0.x"

          - task: DotNetCoreCLI@2
            displayName: "Install DevOps PR Analyzer"
            inputs:
              command: "custom"
              custom: "tool"
              arguments: "install --global Lintellect"

          - task: DotNetCoreCLI@2
            displayName: "Basic C# Analysis"
            inputs:
              command: "custom"
              custom: "Lintellect"
              arguments: 'analyze --language "csharp" --enable-summary-comment --enable-inline-suggestions --enable-description-summary'
            env:
              LINTELLECT_API_URL: $(LINTELLECT_API_URL)
              LINTELLECT_API_KEY: $(LINTELLECT_API_KEY)
              GITHUB_TOKEN: $(GITHUB_TOKEN)
```

## API Reference

### Authentication

All API endpoints require authentication using an API key:

```bash
API-Key: your-api-key
```

### Response Format

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "startedAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:35:00Z",
  "summary": "Analysis completed successfully",
  "detailedAnalysis": "Detailed analysis results...",
  "analyzerUsed": "Claude"
}
```

## Configuration

### Required Settings

```json
{
  "ConnectionStrings": {
    "postgresdb": "Host=localhost;Database=lintellect;Username=postgres;Password=password"
  },
  "ApiKey": "your-secure-api-key"
}
```

### AI Analyzer Settings

#### Claude Analyzer

```json
{
  "ClaudeAnalyzer": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4000,
    "Temperature": 0.1
  }
}
```

#### Azure AI Foundry

```json
{
  "AzureOpenAIAnalyzer": {
    "ApiKey": "your-azure-ai-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  }
}
```

### Git Provider Settings

Git provider credentials are configured at the application level and used for all analysis requests.

#### GitHub

```json
{
  "GitCredentials": {
    "GitHub": {
      "Token": "ghp_..."
    }
  }
}
```

Or via environment variable:
```bash
export GITHUB_TOKEN="ghp_..."
```

#### Azure DevOps

```json
{
  "GitCredentials": {
    "AzureDevOps": {
      "Pat": "your-pat-token",
      "OrgUrl": "https://dev.azure.com/your-org"
    }
  }
}
```

Or via environment variables:
```bash
export AZURE_DEVOPS_PAT="your-pat-token"
export AZURE_DEVOPS_ORG_URL="https://dev.azure.com/your-org"
```

### Key Components

- **Domain Layer**: Core business logic, entities, and domain events
- **Application Layer**: CQRS with Mediator pattern, commands, queries, and handlers
- **Infrastructure Layer**: Database, external services, Git clients, and AI integrations
- **API Layer**: REST endpoints, authentication, and middleware

### Technology Stack

- **.NET 10**: Primary framework with C# 14.0
- **PostgreSQL**: Database with JSONB support
- **Mediator**: Source-generator based CQRS implementation
- **FluentValidation**: Input validation
- **Polly**: Resilience patterns for external API calls
- **OpenTelemetry**: Metrics and observability
- **Testcontainers**: Integration testing with real database
- **NSubstitute**: Mocking framework for unit tests

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker](https://www.docker.com/get-started) (optional)

### Getting Started

1. **Clone and Setup**

   ```bash
   git clone https://github.com/your-org/lintellect.git
   cd lintellect
   dotnet restore
   ```

2. **Start with Aspire (Recommended)**

   ```bash
   # Start the Aspire AppHost - this handles everything automatically
   cd src/AppHost
   dotnet run
   ```

   The Aspire AppHost will:

   - Start PostgreSQL in a container
   - Launch the API service
   - Provide the Aspire dashboard at https://localhost:15000
   - Handle all service discovery and configuration

3. **Alternative: Manual Setup**

   ```bash
   # Database setup (if not using Aspire)
   docker run --name postgres-dev -e POSTGRES_PASSWORD=password -p 5432:5432 -d postgres:15
   createdb lintellect

   # Run migrations
   cd src/Lintellect.Api
   dotnet ef database update

   # Start API manually
   dotnet run
   ```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run integration tests
dotnet test tests/Lintellect.Api.FunctionalTests/
```

### Building

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build src/Lintellect.Api/Lintellect.Api.csproj

# Publish for production
dotnet publish src/Lintellect.Api/Lintellect.Api.csproj -c Release -o ./publish
```

## Git Workflow

Lintellect uses **GitHub Flow** with release branches. See [Git Workflow Documentation](docs/GIT_WORKFLOW.md) for complete details.

### Branch Strategy

- **`main`** - Production-ready code, always stable
- **`feature/*`** - New features
- **`bugfix/*`** - Bug fixes
- **`hotfix/api/*`** or **`hotfix/cli/*`** - Critical fixes
- **`release/api/v*`** or **`release/cli/v*`** - Release preparation

## Release Process

Lintellect has **two independent releases**:

- **API** - Docker images tagged as `api/v1.2.3`
- **CLI** - NuGet package tagged as `cli/v2.1.0`

### Quick Release

```bash
# API Release
./scripts/create-release-api.sh 1.2.0

# CLI Release
./scripts/create-release-cli.sh 2.1.0
```

See [Git Workflow Documentation](docs/GIT_WORKFLOW.md) for detailed release process.

### GitHub Configuration

For setting up the repository on GitHub, see [GitHub Setup Guide](docs/GITHUB_SETUP.md).

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Quick Start

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes using conventional commits
4. Push to the branch and open a Pull Request

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Add XML documentation for public APIs
- Include unit tests for new functionality
- Ensure all tests pass before submitting PR

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Acknowledgments

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) for application orchestration
- [Mediator](https://github.com/martinothamar/Mediator) for CQRS implementation
- [Polly](https://github.com/App-vNext/Polly) for resilience patterns
- [OpenTelemetry](https://opentelemetry.io/) for observability

## Support

- [Documentation](https://github.com/your-org/lintellect/wiki)
- [Issue Tracker](https://github.com/your-org/lintellect/issues)
- [Discussions](https://github.com/your-org/lintellect/discussions)
- [Email Support](mailto:support@lintellect.ai)

---

<div align="center">

[Star this repo](https://github.com/your-org/lintellect) • [Report Bug](https://github.com/your-org/lintellect/issues) • [Request Feature](https://github.com/your-org/lintellect/issues)

</div>
