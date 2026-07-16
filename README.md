# Lintellect

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/DawnDevelop/Lintellect/workflows/CI/badge.svg)](https://github.com/DawnDevelop/Lintellect/actions)
[![API Version](https://img.shields.io/badge/API-v1.0.0-blue.svg)](https://github.com/DawnDevelop/Lintellect/releases)
[![CLI Version](https://img.shields.io/badge/CLI-v1.0.0-green.svg)](https://www.nuget.org/packages/Lintellect.Cli)

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
git clone https://github.com/DawnDevelop/Lintellect.git
cd lintellect
```

### 2. Run with .NET Aspire (Recommended)

The application is designed to run with .NET Aspire for optimal development experience:

```bash
# Start the Aspire AppHost
cd src/Lintellect.AppHost
dotnet run
```

This will:

- Start PostgreSQL in a container
- Launch the API service
- Provide Aspire dashboard at https://localhost:15000
- Handle service discovery and configuration

### 3. Configure Environment

The Aspire AppHost will automatically configure the environment. For custom configuration, modify `src/Lintellect.AppHost/appsettings.json`:

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
- **API Documentation**: https://localhost:7000/scalar/v1 (Scalar UI, Development only)
- **Health Check**: https://localhost:7000/health (readiness: /health/ready)

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
# Basic C# analysis with AI features (Semgrep runs by default)
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

### Re-triggered analyses

The API deduplicates analysis per pull request:

- **First trigger** → full analysis (summary comment, description summary, inline suggestions).
- **Re-trigger with new commits** → incremental analysis that posts **inline suggestions only**, computed from the diff between the previously analyzed commit and the new source head (falls back to the full PR diff if that range can't be resolved, e.g. after a force-push).
- **Re-trigger without new commits**, or while a previous job is still running → no new job; the existing job's id is returned.
- A **failed** job never blocks re-analysis — the next trigger runs a full analysis again.

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
          dotnet tool install --global Lintellect.Cli

      - name: Basic C# Analysis
        run: |
          Lintellect analyze \
            --language "csharp" \
            --enable-summary-comment \
            --enable-inline-suggestions \
            --enable-description-summary
        env:
          # The CLI only needs the Lintellect API URL + key. Git provider tokens
          # (GITHUB_TOKEN / AZURE_DEVOPS_PAT) are configured on the Lintellect API, not here.
          LINTELLECT_API_URL: ${{ secrets.LINTELLECT_API_URL }}
          LINTELLECT_API_KEY: ${{ secrets.LINTELLECT_API_KEY }}
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
# Note: git provider credentials (AZURE_DEVOPS_PAT / GITHUB_TOKEN) are configured on the
#       Lintellect API server, not in this pipeline. The CLI only talks to the Lintellect API.

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
              arguments: "install --global Lintellect.Cli"

          - task: DotNetCoreCLI@2
            displayName: "Basic C# Analysis"
            inputs:
              command: "custom"
              custom: "Lintellect"
              arguments: 'analyze --language "csharp" --enable-summary-comment --enable-inline-suggestions --enable-description-summary'
            env:
              # The CLI only needs the Lintellect API URL + key. Git provider tokens
              # (AZURE_DEVOPS_PAT / GITHUB_TOKEN) are configured on the Lintellect API, not here.
              LINTELLECT_API_URL: $(LINTELLECT_API_URL)
              LINTELLECT_API_KEY: $(LINTELLECT_API_KEY)
```

## API Reference

### Authentication

All `api/analysis/*` endpoints require an API key, passed via the `Api-Key` request header:

```bash
Api-Key: your-api-key
```

Requests with a missing or incorrect key receive `401 Unauthorized`.

### Endpoints

| Method   | Route                              | Description                                          |
| -------- | ---------------------------------- | ---------------------------------------------------- |
| `POST`   | `/api/analysis/analyze`            | Submit a new analysis job for background processing. |
| `GET`    | `/api/analysis/status/{jobId}`     | Get the status and results of an analysis job.       |
| `GET`    | `/api/analysis/history`            | List analysis jobs (supports optional filtering).    |
| `DELETE` | `/api/analysis/history`            | Delete analysis history (all, or a single `jobId`).  |
| `POST`   | `/api/azuredevops/webhooks/pr/commented-on` | Azure DevOps PR-comment webhook.            |
| `POST`   | `/api/azuredevops/webhooks/pr/updated`      | Azure DevOps PR-updated webhook.            |
| `GET`    | `/health`, `/health/ready`         | Liveness / readiness health checks (no auth).        |

In Development, an interactive OpenAPI reference (Scalar) is served at `/scalar/v1`, backed by the OpenAPI document at `/openapi/v1.json`.

The CLI submits jobs to `POST /api/analysis/analyze`; the API persists the job, queues it, and processes it asynchronously in the background. `GET /api/analysis/status/{jobId}` returns the response below once processing completes.

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
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 40960,
    "Temperature": 0.5
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
   git clone https://github.com/DawnDevelop/Lintellect.git
   cd lintellect
   dotnet restore
   ```

2. **Start with Aspire (Recommended)**

   ```bash
   # Start the Aspire AppHost - this handles everything automatically
   cd src/Lintellect.AppHost
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

### Git Provider Credentials

For configuring GitHub / Azure DevOps credentials on the API server, see [Git Credentials Configuration](docs/GIT_CREDENTIALS_CONFIGURATION.md).

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

- [Documentation](https://github.com/DawnDevelop/Lintellect/wiki)
- [Issue Tracker](https://github.com/DawnDevelop/Lintellect/issues)
- [Discussions](https://github.com/DawnDevelop/Lintellect/discussions)
- [Email Support](mailto:support@lintellect.ai)

---

<div align="center">

[Star this repo](https://github.com/DawnDevelop/Lintellect) • [Report Bug](https://github.com/DawnDevelop/Lintellect/issues) • [Request Feature](https://github.com/DawnDevelop/Lintellect/issues)

</div>
