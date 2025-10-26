# CodeQL Integration with Lintellect CLI

This document explains how to use CodeQL analysis with the Lintellect CLI for comprehensive security and quality analysis.

## Overview

CodeQL is GitHub's semantic code analysis engine that treats code as data, allowing for sophisticated vulnerability detection and code quality analysis. The Lintellect CLI now integrates CodeQL alongside its existing Roslyn-based analysis to provide comprehensive code analysis.

## Features

- **Security Analysis**: Detect security vulnerabilities using CodeQL's security queries
- **Quality Analysis**: Identify code quality issues and best practice violations
- **Multiple Query Suites**: Support for different CodeQL query suites
- **Flexible Output**: Console, JSON, and SARIF output formats
- **Integration**: Works alongside existing Roslyn analyzers
- **Cross-Platform**: Works on Windows, Linux, and macOS

## Prerequisites

### 1. Install CodeQL CLI

CodeQL CLI is automatically installed via GitHub CLI extension. The Lintellect CLI will handle the installation automatically, but you can also install it manually.

#### Automatic Installation

The Lintellect CLI will automatically:

1. Install GitHub CLI if not present
2. Install the CodeQL extension
3. Verify the installation

#### Manual Installation

#### Windows

```powershell
# Install GitHub CLI
winget install GitHub.cli

# Install CodeQL extension
gh extension install github/gh-codeql

# Verify installation
gh codeql version
```

#### macOS

```bash
# Install GitHub CLI
brew install gh

# Install CodeQL extension
gh extension install github/gh-codeql

# Verify installation
gh codeql version
```

#### Linux

```bash
# Install GitHub CLI
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update && sudo apt install gh

# Install CodeQL extension
gh extension install github/gh-codeql

# Verify installation
gh codeql version
```

#### Verify Installation

```bash
gh codeql version
```

## Usage

### 1. Basic CodeQL Analysis

Run CodeQL analysis on a C# solution:

```bash
# Basic analysis with default security-and-quality queries
dotnet run -- codeql --solution ./MySolution.sln

# Specify custom query suites
dotnet run -- codeql --solution ./MySolution.sln --queries security-and-quality security-extended

# Set timeout and output format
dotnet run -- codeql --solution ./MySolution.sln --timeout 60 --format json --output results.json
```

### 2. Integrated Analysis

Run both Roslyn and CodeQL analysis together:

```bash
# Enable CodeQL in the main analyze command
dotnet run -- analyze --solution ./MySolution.sln --enable-codeql

# With custom CodeQL settings
dotnet run -- analyze --solution ./MySolution.sln --enable-codeql --codeql-queries security-and-quality --codeql-timeout 45
```

### 3. Command Options

#### CodeQL Command Options

| Option       | Description                               | Default                |
| ------------ | ----------------------------------------- | ---------------------- |
| `--solution` | Path to .sln or .slnx file                | Current directory      |
| `--queries`  | CodeQL query suites to run                | `security-and-quality` |
| `--timeout`  | Analysis timeout in minutes               | 30                     |
| `--output`   | Output file path (optional)               | Console                |
| `--format`   | Output format: `console`, `json`, `sarif` | `console`              |
| `--verbose`  | Enable verbose output                     | `false`                |

#### Integrated Analysis Options

| Option             | Description               | Default                |
| ------------------ | ------------------------- | ---------------------- |
| `--enable-codeql`  | Enable CodeQL analysis    | `false`                |
| `--codeql-queries` | CodeQL query suites       | `security-and-quality` |
| `--codeql-timeout` | CodeQL timeout in minutes | 30                     |

## Query Suites

### Available Query Suites

- **`security-and-quality`**: Default suite with security and quality queries
- **`security-extended`**: Extended security queries for deeper analysis
- **`security`**: Security-focused queries only
- **`quality`**: Code quality queries only

### Custom Queries

You can also run specific CodeQL queries by providing the query path:

```bash
dotnet run -- codeql --solution ./MySolution.sln --queries "C:\codeql\ql\csharp\ql\src\Security\CWE-079\Xss.ql"
```

## Output Formats

### 1. Console Output (Default)

```
========================================
CodeQL Security & Quality Analysis
========================================

Configuration:
  Solution Path: ./MySolution.sln
  Query Suites: security-and-quality
  Timeout: 30 minutes
  Output: Console
  Format: console
  Verbose: false

Starting CodeQL analysis...
✓ CodeQL analysis completed: 5 finding(s)

Analysis completed: 5 finding(s) detected
  Errors: 2
  Warnings: 3
  Info: 0

CodeQL Analysis Results:
========================

🚨 Errors (2)
--------------------
  CodeQL-csharp/sql-injection: Potential SQL injection vulnerability
    📁 src/Controllers/UserController.cs:45

⚠️ Warnings (3)
--------------------
  CodeQL-csharp/unused-local-variable: Unused local variable 'temp'
    📁 src/Services/UserService.cs:23
```

### 2. JSON Output

```bash
dotnet run -- codeql --solution ./MySolution.sln --format json --output results.json
```

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "totalFindings": 5,
  "findingsBySeverity": {
    "Error": 2,
    "Warning": 3,
    "Info": 0
  },
  "findings": [
    {
      "ruleId": "CodeQL-csharp/sql-injection",
      "message": "[CodeQL] Potential SQL injection vulnerability",
      "filePath": "src/Controllers/UserController.cs",
      "line": 45,
      "severity": "Error"
    }
  ]
}
```

### 3. SARIF Output

```bash
dotnet run -- codeql --solution ./MySolution.sln --format sarif --output results.sarif
```

## Integration with CI/CD

### GitHub Actions

```yaml
name: Code Analysis
on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Install CodeQL
        run: |
          wget https://github.com/github/codeql-cli-binaries/releases/download/v2.15.4/codeql-linux64.zip
          unzip codeql-linux64.zip
          echo "$(pwd)/codeql" >> $GITHUB_PATH

      - name: Run Analysis
        run: |
          dotnet run -- analyze --solution ./MySolution.sln --enable-codeql --codeql-queries security-and-quality
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: "ubuntu-latest"

steps:
  - task: UseDotNet@2
    inputs:
      packageType: "sdk"
      version: "10.0.x"

  - script: |
      wget https://github.com/github/codeql-cli-binaries/releases/download/v2.15.4/codeql-linux64.zip
      unzip codeql-linux64.zip
      echo "$(pwd)/codeql" >> $PATH
    displayName: "Install CodeQL"

  - script: |
      dotnet run -- analyze --solution ./MySolution.sln --enable-codeql
    displayName: "Run Analysis"
```

## Troubleshooting

### Common Issues

1. **CodeQL not found**

   ```
   ❌ CodeQL is not installed or not in PATH
   ```

   **Solution**: Install CodeQL CLI and add it to your PATH

2. **Database creation failed**

   ```
   CodeQL database creation failed: No source files found
   ```

   **Solution**: Ensure the solution path is correct and contains C# source files

3. **Query execution timeout**
   ```
   CodeQL query execution failed: Timeout
   ```
   **Solution**: Increase the timeout value with `--timeout` option

### Performance Tips

1. **Use specific query suites**: Only run the queries you need
2. **Set appropriate timeouts**: Large codebases may need longer timeouts
3. **Exclude unnecessary files**: Use file exclusions to skip generated files
4. **Run in parallel**: Use the integrated analysis for better performance

## Examples

### Example 1: Security Analysis

```bash
# Run only security queries
dotnet run -- codeql --solution ./MySolution.sln --queries security --format json --output security-results.json
```

### Example 2: Quality Analysis

```bash
# Run only quality queries
dotnet run -- codeql --solution ./MySolution.sln --queries quality --verbose
```

### Example 3: Comprehensive Analysis

```bash
# Run both Roslyn and CodeQL analysis
dotnet run -- analyze --solution ./MySolution.sln --enable-codeql --codeql-queries security-and-quality security-extended --codeql-timeout 60
```

## Best Practices

1. **Start with default queries**: Use `security-and-quality` for most cases
2. **Gradually add more queries**: Add `security-extended` for deeper analysis
3. **Use appropriate timeouts**: Set timeouts based on codebase size
4. **Integrate with existing workflows**: Use the integrated analysis command
5. **Review results carefully**: CodeQL findings may need manual verification
6. **Keep CodeQL updated**: Use the latest version for best results

## Resources

- [CodeQL Documentation](https://codeql.github.com/docs/)
- [CodeQL CLI Reference](https://codeql.github.com/docs/codeql-cli/)
- [CodeQL Query Writing](https://codeql.github.com/docs/writing-codeql-queries/)
- [GitHub CodeQL Repository](https://github.com/github/codeql)
