# Git Provider Credentials Configuration

This document explains how to configure Git provider credentials for the DevOps PR Analyzer.

## Overview

The DevOps PR Analyzer now supports dynamic Git provider credentials that are passed via the `AnalysisRequest` instead of being configured at the application level. This allows users to configure different credentials per analysis request, making it suitable for CI/CD pipelines where different repositories may use different Git providers.

The system uses a factory pattern (`IGitClientFactory`) to create Git clients with dynamic credentials, eliminating the need for static configuration and providing better security and flexibility.

## Supported Git Providers

- **Azure DevOps**: Requires Personal Access Token (PAT) and Organization URL
- **GitHub**: Requires Personal Access Token (PAT)

## CLI Usage

### Azure DevOps

```bash
dotnet run -- analyze \
  --solution ./MyProject.sln \
  --language CSharp \
  --api-url https://your-api-url.com \
  --api-key your-api-key \
  --devops-pat your-azure-devops-pat \
  --azure-devops-org-url https://dev.azure.com/yourorg
```

### GitHub

```bash
dotnet run -- analyze \
  --solution ./MyProject.sln \
  --language CSharp \
  --api-url https://your-api-url.com \
  --api-key your-api-key \
  --github-token your-github-pat
```

### Environment Variables

You can also use environment variables for security:

```bash
export DEVOPS_PAT="your-azure-devops-pat"
export AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg"
export GITHUB_TOKEN="your-github-pat"

dotnet run -- analyze --solution ./MyProject.sln --language CSharp
```

## API Usage

When submitting analysis requests via the API, include the Git provider credentials in the request:

```json
{
  "language": "CSharp",
  "findings": [...],
  "gitInfo": {
    "projectName": "my-project",
    "repositoryName": "my-repo",
    "pullRequestId": 123
  },
  "gitProvider": "GitHub",
  "devopsPat": "your-azure-devops-pat",
  "azureDevOpsOrgUrl": "https://dev.azure.com/yourorg",
  "githubToken": "your-github-pat",
  "enableSummaryComment": true,
  "enableInlineSuggestions": true,
  "enableDescriptionSummary": true,
  "enableCodeOwners": false
}
```

## Security Considerations

1. **Never commit credentials to source control**
2. **Use environment variables or secure secret management**
3. **Use least-privilege tokens with minimal required permissions**
4. **Rotate tokens regularly**

## Credential Validation

The system performs comprehensive validation of Git provider credentials:

### ✅ **What Gets Validated**

1. **Format Validation**: Ensures credentials are properly formatted
2. **Connection Testing**: Actually tests the connection to the Git provider
3. **Permission Verification**: Validates that the credentials have sufficient permissions
4. **Repository Access**: Confirms access to the specific repository and pull request

### 🔍 **Validation Process**

1. **Format Check**: Validates PAT/Token format and organization URL format
2. **Connection Test**: Attempts to connect to the Git provider
3. **Permission Test**: Tries to read pull request information
4. **Repository Access**: Verifies access to the specific repository
5. **File Access**: Tests read permissions on repository files

### ❌ **Common Validation Failures**

- **Invalid Credentials**: PAT/Token is expired, revoked, or malformed
- **Insufficient Permissions**: Credentials don't have required read/write access
- **Wrong Organization**: Azure DevOps URL points to wrong organization
- **Repository Access**: No access to the specific repository
- **Network Issues**: Connectivity problems to Git provider

## Required Permissions

### Azure DevOps PAT

- **Code (Read)**: To read repository contents and pull request diffs
- **Pull Requests (Read & Write)**: To read PR details and post comments

### GitHub PAT

- **repo**: To read repository contents and pull request diffs
- **pull_requests**: To read and write pull request comments

## GitHub Code Owners

GitHub natively supports CODEOWNERS files, so no additional implementation is needed. The `AddCodeOwnersToPr` method will log that GitHub handles this natively and return successfully.

## Migration from Static Configuration

If you were previously using static configuration in `appsettings.json`, you can now:

1. Remove the static Git provider configuration from your API settings
2. Pass credentials via the CLI or API request
3. Use environment variables for secure credential management

## Troubleshooting

### Common Issues

1. **Invalid credentials**: Ensure the PAT has the required permissions
2. **Invalid organization URL**: For Azure DevOps, use the full organization URL
3. **Missing credentials**: Ensure at least one Git provider credential is provided when `gitInfo` is specified

### Error Messages

- `"DevopsPAT is required for Azure DevOps provider"`: Provide a valid Azure DevOps PAT
- `"AzureDevOpsOrgUrl is required for Azure DevOps provider"`: Provide a valid organization URL
- `"GitHubToken is required for GitHub provider"`: Provide a valid GitHub PAT
- `"AzureDevOpsOrgUrl must be a valid absolute URI"`: Ensure the URL is properly formatted
