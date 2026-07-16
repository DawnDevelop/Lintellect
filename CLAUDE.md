# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Working Style & Relationship

- We are colleagues. No hierarchy, no formalities.
- Do NOT flatter me. Never agree just to be agreeable.
- ALWAYS call out bad ideas, mistakes, and poor decisions — I depend on this.
- When you disagree, push back clearly. Cite technical reasons if you have them.
  If it's a gut feeling, say so explicitly.

# Clarification Protocol

When anything is unclear or would require you to make assumptions:

- STOP. Do NOT guess or assume.
- Ask me one focused question at a time (interview style).
- Wait for my answer before proceeding.
- State explicitly what you still don't know before writing any code.
- "I assumed X" is never acceptable. Uncertainty must surface before action.

# Writing Code

- YAGNI. The best code is no code.
- Make the SMALLEST reasonable change that solves the problem.
- Prefer simple, clean, and maintainable over clever or complex.
- NEVER throw away or rewrite existing implementations without my explicit permission.
- MATCH the style of the surrounding code.

# Code Style

- No nesting beyond 2 levels inside a function body.
- Do NOT use `// Act`, `// Arrange`, `// Assert` comments in unit tests.

## C# Specifics

- Always use `var` instead of explicit types (int, float, etc.) where possible.
- Max 7 parameters per function/method. Use record classes when exceeded.
- Always use simplified collection initializations.
- Prefer LINQ over manual loops.

# Boundaries — NEVER do these without explicit permission

- NEVER throw away or rewrite existing implementations.
- NEVER delete a failing test — fix it or flag it.
- NEVER skip a pre-commit hook.
- NEVER run `git add -A` without checking `git status` first.
- NEVER implement backward compatibility without approval.

## Commands

```bash
# Local development (recommended) — starts PostgreSQL container + API via Aspire
cd src/Lintellect.AppHost && dotnet run
# Aspire dashboard: https://localhost:15000  |  API: https://localhost:7000

# Build
dotnet build
dotnet build src/Lintellect.Api/Lintellect.Api.csproj

# Tests
dotnet test                                                    # all tests
dotnet test tests/Lintellect.Api.UnitTests/                   # unit tests only
dotnet test tests/Lintellect.Api.FunctionalTests/             # functional (requires Docker for Testcontainers)
dotnet test tests/Lintellect.Cli.UnitTests/                   # CLI unit tests
dotnet test --logger "console;verbosity=detailed"             # verbose output

# Database migrations (without Aspire)
cd src/Lintellect.Api && dotnet ef database update

# Pack CLI as NuGet tool
dotnet pack src/Lintellect.Cli/Lintellect.Cli.csproj -c Release
```

## Architecture

Lintellect is an AI-powered PR code review assistant. There are two runtime components:

**CLI** (`Lintellect.Cli`) — stateless, runs in CI/CD pipelines. Reads PR context from CI environment variables, performs Roslyn-based C# analysis, and POSTs an `AnalysisRequest` to the API.

**API** (`Lintellect.Api`) — ASP.NET Core service. Receives the request, persists it as an `AnalysisJob`, enqueues it onto a channel-based `AnalysisJobQueue`, and a background service processes it: calls Claude or Azure OpenAI (via Microsoft Agent Framework), then posts review comments back to GitHub/Azure DevOps.

**Data flow:**

```
CI/CD → CLI (Roslyn analysis + Git context extraction)
      → POST /analysis to API
      → AnalysisJob persisted in PostgreSQL
      → Background service dequeues + calls AI (Claude / Azure OpenAI via Microsoft Agent Framework)
      → Results stored (Summary, DetailedAnalysis, InlineSuggestions)
      → Comments posted to PR via Octokit / TFS client
```

**Shared** (`Lintellect.Shared`) — data contracts (`AnalysisRequest`, `GitInfo`, `AnalyzerFindings`) used by both CLI and API.

**AppHost / ServiceDefaults** — .NET Aspire orchestration for local dev; not deployed.

### API layer structure (Clean Architecture)

```
Domain/          → Entities (AnalysisJob, WebhookEvent), domain events, enums
Application/     → CQRS: commands, queries, handlers (source-generated Mediator), FluentValidation
Infrastructure/  → EF Core + PostgreSQL, AI services, Git clients, background services
Apis/            → Minimal API endpoints, API key auth filter
```

### Key interfaces

| Interface             | Purpose                                                              |
| --------------------- | -------------------------------------------------------------------- |
| `IAnalyzerService`    | AI service contract (ClaudeAnalyzerService, AzureOpenAIAnalyzerService) |
| `IGitInfoExtractor`   | Extract PR context from CI env vars                                  |
| `IGitClientFactory`   | Create GitHub/Azure DevOps clients dynamically                       |
| `IPullRequestService` | Fetch diffs, post comments                                           |
| `IMcpServiceResolver` | Resolve MCP servers for AI context                                   |
| `IWorkItemService`    | Resolve linked work items / issues for a PR (per-provider)           |

Factories (`GitInfoExtractorFactory`, `GitClientFactory`) select implementations based on `EGitProvider` at runtime.

### AI prompt pipeline

`PromptBuilder` assembles prompts from templates in `Infrastructure/Services/AI/Prompts/Templates/{Language}/`. `TokenAwareChunker` splits large diffs to stay within model token limits; `TokenEstimator` estimates token counts without calling the API.

Before a diff is embedded, `DiffGenerationHelper.AnnotateWithLineNumbers` prefixes each line with its new-file line number (`<line>|<marker><code>`); the prompts tell the model to read that number for `lineFrom`/`lineTo` instead of computing it from hunk headers.

`InlineSuggestionLimiter` (`Application/Services`) is the shared, provider-agnostic policy for bounding inline suggestions — `ComputeMaxSuggestionsPerFile` (per-file budget) and `ApplyGlobalCap` (global cap, highest-severity first). Both `ClaudeAnalyzerService` and `AzureOpenAIAnalyzerService` use it, so neither analyzer depends on the other.

`JsonExtensions.DeserializeModelJson<T>` strips Markdown code fences before parsing model output. Claude inline parsing (`ClaudeAnalyzerService.ParseInlineSuggestions`) tolerates both a `{ "suggestions": [...] }` wrapper and a bare `[...]` array.

### Work-item context (on by default)

When `AnalysisRequest.EnableWorkItemContext` is true (CLI flag `--enable-work-item-context` / `-ewi`, defaults to true; pass `--enable-work-item-context false` to disable), the orchestrator resolves linked work items via `IWorkItemService` and injects them verbatim — there is deliberately no AI summarization pass (it was an extra call and failure point). `WorkItemPromptFormatter` (`Application/Services`) renders the context: the full block (per-item heading, type/state, body truncated at 4000 chars, plus a directive to flag missing scope) goes into the Summary and Detailed-Analysis prompts via `{{workItemContext}}`; only a one-line title list goes into the per-file Inline-Suggestion prompts (per-file calls multiply tokens by file count, so the inline cost stays bounded). Empty context renders as an empty string, never a dangling heading. Failures during fetch log + continue with no context. Azure DevOps work items are resolved server-side via the WIT REST API; the body is composed from the fields in `Analysis:WorkItemBodyFields` (ordered, labeled, HTML-stripped; defaults: Description, Acceptance Criteria, Repro Steps — fields absent on a type are skipped, so bugs and stories both work, and custom process templates only need a config override). GitHub uses PR-body parsing for `Closes/Fixes/Resolves #N` keywords.

### Per-PR dedupe and incremental re-analysis

`SubmitAnalysisCommandHandler` deduplicates per PR (provider + project + repo + PR id; `Failed` jobs don't count). Every submission resolves the PR's source-branch head server-side (`PullRequest.SourceCommit`: ADO `LastMergeSourceCommit`, GitHub `pr.Head.Sha`) and stores it as `AnalysisJob.SourceCommitId` — the CLI's `GitInfo.CommitId` is a transient *merge* commit on both providers and must not be used as a diff baseline. Re-trigger rules: previous job Pending/Running or same source head or inline suggestions disabled → skip, return the existing job id; previous job Completed + new commits → new job with `ReanalysisBaseCommitId` = previous head and summary/description/initial-comment/code-owners flags forced off (inline suggestions only). Processing fetches the compact diff `ReanalysisBaseCommitId..SourceCommitId` via `IGitClient.GetCompactDiffsBetweenCommitsAsync`, falling back to the full PR diff on failure (e.g. force-push). Inline-only jobs bypass the batched analyzer path so a re-run costs one AI call. The dedupe is race-safe: a partial unique index (`IX_AnalysisJobs_ActiveJobPerPullRequest`, at most one Pending/Running job per provider+project+repo+PR) backs the check-then-insert, and the submit handler catches the unique violation and returns the winning job. The job queue is in-memory; on startup the background service re-enqueues Pending jobs and fails orphaned Running jobs ("Orphaned by service restart") so a restart neither loses queued work nor leaves a PR permanently blocked by a stuck job. Result posting is per-step fault-tolerant (a read-only PR doesn't fail a completed analysis), failed jobs update the placeholder comment, and the review comment carries a context footer (work items, custom instructions, diff mode).

### Configuration

Settings fall back to environment variables via `PostConfigure<>()`:

| Key                                 | Env var fallback       |
| ----------------------------------- | ---------------------- |
| `ApiKey`                            | —                      |
| `ConnectionStrings:postgresdb`      | —                      |
| `ClaudeAnalyzer:ApiKey`             | `CLAUDE_API_KEY`       |
| `AzureOpenAIAnalyzer:ApiKey`        | `AZURE_OPENAI_API_KEY` |
| `AzureOpenAIAnalyzer:Endpoint`      | `AZURE_OPENAI_ENDPOINT` |
| `AzureOpenAIAnalyzer:DeploymentName`| `AZURE_OPENAI_DEPLOYMENT_NAME` |
| `GitCredentials:GitHub:Token`       | `GITHUB_TOKEN`         |
| `GitCredentials:AzureDevOps:Pat`    | `AZURE_DEVOPS_PAT`     |
| `GitCredentials:AzureDevOps:OrgUrl` | `AZURE_DEVOPS_ORG_URL` |

For Aspire local dev, configure credentials in `src/Lintellect.AppHost/appsettings.json` (user secrets are also wired up).

### Package management

All package versions are centralized in `Directory.Packages.props` — do not set `Version=` attributes in `.csproj` files; use `<PackageReference Include="..." />` only.

## Code conventions

- **Enums** prefixed with `E` (`EGitProvider`, `EProgrammingLanguage`)
- **Interfaces** prefixed with `I`
- **Sealed classes** by default for non-inheritable types
- **Records** for immutable data (`GitInfo`)
- `internal` for implementation types, `public` only for APIs
- File-scoped namespaces, collection expressions `[]`, primary constructors, `ConfigureAwait(false)` in library code
- `InvariantGlobalization` is enabled in the CLI — avoid culture-sensitive operations there

## Testing

- **Unit tests**: NUnit + NSubstitute + Shouldly. CLI exposes internals via `InternalsVisibleTo`.
- **Functional tests**: Testcontainers spins up a real PostgreSQL instance; Respawn resets data between tests. Requires Docker.
- **Test data**: Bogus for generation; fluent `TestDataBuilder` helpers in `Builders/`.
- Functional tests run against the full application stack — do not mock the database in functional tests.
