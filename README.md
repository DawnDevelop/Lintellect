# Lintellect

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/DawnDevelop/Lintellect/workflows/CI/badge.svg)](https://github.com/DawnDevelop/Lintellect/actions)
[![API Version](https://img.shields.io/badge/API-v1.0.0-blue.svg)](https://github.com/DawnDevelop/Lintellect/releases)
[![CLI Version](https://img.shields.io/badge/CLI-v1.0.0-green.svg)](https://www.nuget.org/packages/Lintellect.Cli)

> **AI-powered code review assistant that enhances pull request analysis with intelligent insights and automated suggestions.**

Lintellect reviews your pull requests with AI. A small CLI runs in your CI pipeline and sends the PR context to a self-hosted Lintellect API, which calls Claude or Azure AI Foundry and posts the review back to GitHub or Azure DevOps:

```
Your CI pipeline (Lintellect CLI)
      → your Lintellect API (self-hosted, one instance per organization)
      → AI analysis (Claude / Azure AI Foundry)
      → review comment, inline suggestions and description summary on the PR
```

The review is work-item aware (linked work items / issues, including acceptance criteria and repro steps, are fed into the analysis) and respects repo-level instruction files (`copilot-instructions.md`, `AGENTS.md`, `CLAUDE.md`).

## Table of Contents

- [Quick Start](#quick-start)
- [Usage](#usage)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Development & Contributing](#development--contributing)
- [License](#license)

## Quick Start

Setting up Lintellect for your organization takes three steps: run the API, add the CLI to your pipeline, open a pull request.

### 1. Run the API

The API is a single container plus PostgreSQL. Database migrations run automatically on startup.

```bash
# Build the image from this repository
docker build -t lintellect-api -f src/Lintellect.Api/Dockerfile .

# Start PostgreSQL and the API
docker network create lintellect
docker run -d --name lintellect-db --network lintellect \
  -e POSTGRES_PASSWORD=change-me -e POSTGRES_DB=lintellect postgres:17

docker run -d --name lintellect-api --network lintellect -p 7000:8080 \
  -e ApiKey="choose-a-secure-api-key" \
  -e ConnectionStrings__postgresdb="Host=lintellect-db;Database=lintellect;Username=postgres;Password=change-me" \
  -e CLAUDE_API_KEY="sk-ant-..." \
  -e AZURE_DEVOPS_PAT="your-pat" \
  -e AZURE_DEVOPS_ORG_URL="https://dev.azure.com/your-org" \
  lintellect-api
```

Minimum configuration:

| Setting | Purpose |
| --- | --- |
| `ApiKey` | The key your pipelines use to call this API |
| `ConnectionStrings__postgresdb` | PostgreSQL connection string |
| `CLAUDE_API_KEY` **or** `AZURE_OPENAI_API_KEY` + `AZURE_OPENAI_ENDPOINT` + `AZURE_OPENAI_DEPLOYMENT_NAME` | At least one AI provider |
| `AZURE_DEVOPS_PAT` + `AZURE_DEVOPS_ORG_URL` **and/or** `GITHUB_TOKEN` | Credentials for the Git provider(s) that host your PRs |

The Azure DevOps PAT needs **Code (Read & Write)**, **Pull Request (Read & Write)** and **Work Items (Read)** scopes; the GitHub token needs the `repo` scope. Everything else has sensible defaults — see [Configuration](#configuration).

### 2. Add the CLI to your pipeline

The pipeline only needs the API URL and key — Git provider credentials stay on the API server.

**Azure DevOps** (trigger via build validation policy on your target branch):

```yaml
trigger: none

pool:
  vmImage: "ubuntu-latest"

steps:
  - task: UseDotNet@2
    inputs: { packageType: "sdk", version: "10.0.x" }

  - script: dotnet tool install --global Lintellect.Cli
    displayName: "Install Lintellect CLI"

  - script: >
      Lintellect analyze --language "csharp"
      --enable-summary-comment --enable-inline-suggestions --enable-description-summary
    displayName: "Analyze PR"
    env:
      LINTELLECT_API_URL: $(LINTELLECT_API_URL)
      LINTELLECT_API_KEY: $(LINTELLECT_API_KEY)
```

**GitHub Actions:**

```yaml
name: PR Analysis

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - run: dotnet tool install --global Lintellect.Cli

      - run: >
          Lintellect analyze --language "csharp"
          --enable-summary-comment --enable-inline-suggestions --enable-description-summary
        env:
          LINTELLECT_API_URL: ${{ secrets.LINTELLECT_API_URL }}
          LINTELLECT_API_KEY: ${{ secrets.LINTELLECT_API_KEY }}
```

### 3. Open a pull request

On the next PR you'll see a "🔄 Lintellect analysis is in progress" placeholder, which is replaced by the review once the analysis completes — typically a detailed review comment, inline code suggestions and a summary appended to the PR description.

## Usage

### CLI options

```bash
# Full analysis (summary comment, inline suggestions, description summary; Semgrep runs by default)
Lintellect analyze \
  --language "csharp" \
  --enable-summary-comment \
  --enable-inline-suggestions \
  --enable-description-summary

# Exclude paths, add Azure DevOps code-owner assignment
Lintellect analyze \
  --language "csharp" \
  --exclude "**/bin/**" \
  --exclude "**/obj/**" \
  --exclude "**/Generated/**" \
  --enable-summary-comment \
  --enable-inline-suggestions \
  --enable-azure-devops-code-owners

# Opt out of individual features
Lintellect analyze \
  --language "csharp" \
  --enable-summary-comment \
  --enable-semgrep false \
  --enable-static-analysis false \
  --enable-work-item-context false
```

Supported languages: `csharp` (with Roslyn static analysis), `javascript`, `typescript`, `python`, `java` and others (AI review of the diff; Semgrep where applicable).

### What gets posted

- **Review comment** — the detailed analysis, posted into (and resolving) the placeholder comment. It ends with a context footer showing what the review actually saw: linked work items, whether custom instructions were found, and whether the diff was full or incremental.
- **Description summary** — appended to the PR description.
- **Inline suggestions** — one suggestion comment per finding, capped per file and globally.
- **Code owners** (Azure DevOps, opt-in) — reviewers added from CODEOWNERS rules matching the changed files.

Posting is fault-tolerant per step: if the PR becomes read-only mid-analysis, the remaining steps still run and the job still completes. If a job fails outright, the placeholder comment is updated with a failure note instead of staying "in progress" forever.

### Work-item context

Linked work items / issues are used as PR context by default (`--enable-work-item-context false` to opt out):

- **Azure DevOps**: linked work items are resolved server-side via the WIT REST API. The context includes description, acceptance criteria and repro steps (configurable via `Analysis:WorkItemBodyFields`), so the review can flag work-item scope the PR doesn't cover.
- **GitHub**: the PR body is parsed for `Closes/Fixes/Resolves #N` keywords.

### Custom review instructions

Lintellect looks for a repo-level instruction file on the pull request's source branch and injects it into every review prompt. The first match wins:

```
/.github/copilot-instructions.md   (+ uppercase and /.copilot, /docs, / variants)
/AGENTS.md
/CLAUDE.md                          (+ /CLAUDE, /.claude/CLAUDE.md, /.github/CLAUDE.md)
/.github/AGENTS.md
/docs/AGENTS.md
```

Use it for project conventions the reviewer should enforce ("we ban AutoMapper", "public APIs need XML docs"). No file → no custom instructions; analysis proceeds normally.

### Re-triggered analyses

The API deduplicates analysis per pull request:

- **First trigger** → full analysis (summary comment, description summary, inline suggestions).
- **Re-trigger with new commits** → incremental analysis that posts **inline suggestions only**, computed from the diff between the previously analyzed commit and the new source head (falls back to the full PR diff if that range can't be resolved, e.g. after a force-push).
- **Re-trigger without new commits**, or while a previous job is still running → no new job; the existing job's id is returned.
- A **failed** job never blocks re-analysis — the next trigger runs a full analysis again.

## Configuration

All settings can be provided via `appsettings.json` or environment variables (`Section__Key` form); the env vars shown in Quick Start are dedicated fallbacks for the most common secrets.

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

Git provider credentials are configured on the API server and used for all analysis requests.

```json
{
  "GitCredentials": {
    "GitHub": {
      "Token": "ghp_..."
    },
    "AzureDevOps": {
      "Pat": "your-pat-token",
      "OrgUrl": "https://dev.azure.com/your-org"
    }
  }
}
```

Or via environment variables: `GITHUB_TOKEN`, `AZURE_DEVOPS_PAT`, `AZURE_DEVOPS_ORG_URL`.

### Analysis Settings

```json
{
  "Analysis": {
    "SynchronousAnalysis": false,
    "WorkItemBodyFields": [
      "System.Description",
      "Microsoft.VSTS.Common.AcceptanceCriteria",
      "Microsoft.VSTS.TCM.ReproSteps"
    ]
  }
}
```

- **`SynchronousAnalysis`** (default `false`): when `false`, Claude analyses run through the Message Batches API (~50% cheaper, but no completion-time guarantee — a busy batch queue can delay a review by many minutes). Set to `true` (env: `Analysis__SynchronousAnalysis`) to run direct parallel API calls with predictable latency at full token price.
- **`WorkItemBodyFields`**: the Azure DevOps work item fields composed (in order, labeled) into the work-item context. Fields a work item type doesn't have are skipped, so the defaults cover both stories/PBIs (acceptance criteria) and bugs (repro steps) on the standard Agile/Scrum/CMMI process templates. Override for custom process templates.

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

## Development & Contributing

Everything below is for working **on** Lintellect itself, not for using it.

### Local development

```bash
git clone https://github.com/DawnDevelop/Lintellect.git
cd lintellect

# Recommended: .NET Aspire starts PostgreSQL + API and wires everything up
cd src/Lintellect.AppHost
dotnet run
# Aspire dashboard: https://localhost:15000 | API: https://localhost:7000
```

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), [Docker](https://www.docker.com/get-started). Configure API keys and Git credentials in `src/Lintellect.AppHost/appsettings.json` (user secrets are also wired up).

Manual setup without Aspire:

```bash
docker run --name postgres-dev -e POSTGRES_PASSWORD=password -p 5432:5432 -d postgres:17
cd src/Lintellect.Api
dotnet ef database update
dotnet run
```

### Testing and building

```bash
dotnet test                                          # all tests
dotnet test tests/Lintellect.Api.FunctionalTests/    # integration tests (requires Docker)
dotnet build
dotnet publish src/Lintellect.Api/Lintellect.Api.csproj -c Release -o ./publish
```

### Architecture

Clean Architecture in a single API project:

- **Domain**: entities (`AnalysisJob`, `WebhookEvent`), domain events, enums
- **Application**: CQRS with source-generated [Mediator](https://github.com/martinothamar/Mediator), FluentValidation
- **Infrastructure**: EF Core + PostgreSQL (JSONB), AI services, Git clients, background services
- **Apis**: minimal API endpoints, API-key auth

Plus **Lintellect.Cli** (stateless pipeline tool), **Lintellect.Shared** (contracts) and **AppHost/ServiceDefaults** (.NET Aspire, local dev only).

Tech stack: .NET 10 / C# 14, PostgreSQL, Polly (resilience), OpenTelemetry (observability), Testcontainers + NSubstitute (testing).

### Git workflow and releases

Lintellect uses **GitHub Flow** with release branches and two independent releases — the API (Docker image, `api/v1.2.3`) and the CLI (NuGet package, `cli/v2.1.0`):

```bash
./scripts/create-release-api.sh 1.2.0
./scripts/create-release-cli.sh 2.1.0
```

See the [Git Workflow Documentation](docs/GIT_WORKFLOW.md) for branch strategy and the release process, and [Git Credentials Configuration](docs/GIT_CREDENTIALS_CONFIGURATION.md) for provider credential details.

### Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes using conventional commits
4. Push to the branch and open a Pull Request

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
