# Lintellect

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/your-org/lintellect/actions)
[![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen.svg)](https://github.com/your-org/lintellect/actions)

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
  "GitHub": {
    "Token": "your-github-token"
  },
  "AzureDevOps": {
    "Pat": "your-pat-token",
    "OrgUrl": "https://dev.azure.com/your-org"
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
dotnet tool install --global lintellect

# Verify installation
lintellect --help
```

## Usage

### CLI Analysis

```bash
# Basic analysis
lintellect analyze --solution ./MyProject.sln

# With API integration
lintellect analyze \
  --solution ./MyProject.sln \
  --api-url "https://api.example.com" \
  --api-key "your-api-key" \
  --language CSharp

# With exclusions
lintellect analyze \
  --solution ./MyProject.sln \
  --exclude "**/bin/**" "**/obj/**" "**/node_modules/**"
```

### API Integration

```bash
# Submit analysis job
curl -X POST "https://api.example.com/api/analysis/submit" \
  -H "API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "gitInfo": {
      "projectName": "my-org",
      "repositoryName": "my-repo",
      "pullRequestId": 123
    },
    "language": "csharp",
    "enableSummaryComment": true,
    "enableInlineSuggestions": true
  }'

# Check job status
curl -X GET "https://api.example.com/api/analysis/status/{jobId}" \
  -H "API-Key: your-api-key"
```

### CI/CD Integration

#### GitHub Actions

```yaml
name: PR Analysis
on:
  pull_request:
    types: [opened, synchronize]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Install Lintellect
        run: dotnet tool install --global lintellect
      - name: Analyze PR
        run: |
          lintellect analyze \
            --solution ./MyProject.sln \
            --api-url "${{ secrets.ANALYZER_API_ENDPOINT }}" \
            --api-key "${{ secrets.ANALYZER_API_KEY }}"
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
  "SemanticAnalyzer": {
    "ApiKey": "your-azure-ai-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4o"
  }
}
```

### Git Provider Settings

#### GitHub

```json
{
  "GitHub": {
    "Token": "ghp_..."
  }
}
```

#### Azure DevOps

```json
{
  "AzureDevOps": {
    "Pat": "your-pat-token",
    "OrgUrl": "https://dev.azure.com/your-org"
  }
}
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

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include unit tests for new functionality
- Ensure all tests pass before submitting PR

### Issue Reporting

- Use the provided issue templates
- Include steps to reproduce
- Specify environment details
- Add relevant labels

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
