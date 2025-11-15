# Git Provider Credentials Configuration

This document explains how to configure Git provider credentials for the DevOps PR Analyzer.

## Overview

The DevOps PR Analyzer uses application-level Git provider credentials configured via configuration files or environment variables. This approach is designed for single-tenant deployments where all analysis requests use the same Git provider credentials.

The system uses a factory pattern (`IGitClientFactory`) to create Git clients with credentials resolved from application configuration, providing a secure and centralized credential management approach.

## Supported Git Providers

- **Azure DevOps**: Requires Personal Access Token (PAT) and Organization URL
- **GitHub**: Requires Personal Access Token (PAT)

## Configuration

Git provider credentials must be configured at the application level. The API service will use these credentials for all analysis requests.

### Application Configuration (appsettings.json)

```json
{
  "GitCredentials": {
    "AzureDevOps": {
      "Pat": "your-azure-devops-pat",
      "OrgUrl": "https://dev.azure.com/yourorg"
    },
    "GitHub": {
      "Token": "your-github-pat"
    }
  }
}
```

### Environment Variables

You can also configure credentials using environment variables:

```bash
export AZURE_DEVOPS_PAT="your-azure-devops-pat"
export AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg"
export GITHUB_TOKEN="your-github-pat"
```

The API service will automatically read these environment variables and use them if not specified in `appsettings.json`.

## CLI Usage

The CLI no longer accepts credential parameters. Credentials must be configured on the API service side.

```bash
dotnet run -- analyze \
  --solution ./MyProject.sln \
  --language CSharp \
  --api-url https://your-api-url.com \
  --api-key your-api-key
```

## API Usage

When submitting analysis requests via the API, credentials are automatically resolved from the application configuration. Do not include credentials in the request body:

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
  "enableSummaryComment": true,
  "enableInlineSuggestions": true,
  "enableDescriptionSummary": true,
  "enableAzureDevopsCodeOwners": false
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

## Migration from Per-Request Credentials

If you were previously passing credentials per request (via CLI arguments or in the API request body), you must now:

1. Configure credentials in the API service's `appsettings.json` or via environment variables
2. Remove credential arguments from CLI commands
3. Remove credential fields from API request bodies
4. Ensure the API service has access to the required Git provider credentials

## Troubleshooting

### Common Issues

1. **Invalid credentials**: Ensure the PAT has the required permissions
2. **Invalid organization URL**: For Azure DevOps, use the full organization URL
3. **Missing credentials**: Ensure at least one Git provider credential is provided when `gitInfo` is specified

### Error Messages

- `"Azure DevOps credentials are not configured. Populate GitCredentials:AzureDevOps in configuration."`: Configure Azure DevOps credentials in `appsettings.json` or via `AZURE_DEVOPS_PAT` and `AZURE_DEVOPS_ORG_URL` environment variables
- `"GitHub token is not configured. Populate GitCredentials:GitHub in configuration."`: Configure GitHub credentials in `appsettings.json` or via `GITHUB_TOKEN` environment variable
