# create-release-api.ps1
# Creates a new API release branch and prepares for release

param(
  [Parameter(Mandatory = $true)]
  [string]$Version
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
  Red    = "Red"
  Green  = "Green"
  Yellow = "Yellow"
  Blue   = "Cyan"
}

# Function to print colored output
function Write-Status {
  param([string]$Message)
  Write-Host "[INFO] $Message" -ForegroundColor $Colors.Blue
}

function Write-Success {
  param([string]$Message)
  Write-Host "[SUCCESS] $Message" -ForegroundColor $Colors.Green
}

function Write-Warning {
  param([string]$Message)
  Write-Host "[WARNING] $Message" -ForegroundColor $Colors.Yellow
}

function Write-Error {
  param([string]$Message)
  Write-Host "[ERROR] $Message" -ForegroundColor $Colors.Red
}

# Function to show usage
function Show-Usage {
  Write-Host "Usage: .\create-release-api.ps1 -Version <version>"
  Write-Host ""
  Write-Host "Creates a new API release branch and prepares for release"
  Write-Host ""
  Write-Host "Parameters:"
  Write-Host "  -Version    The version number (e.g., 1.2.0)"
  Write-Host ""
  Write-Host "Examples:"
  Write-Host "  .\create-release-api.ps1 -Version 1.2.0"
  Write-Host "  .\create-release-api.ps1 -Version 2.0.0"
  Write-Host ""
  Write-Host "The script will:"
  Write-Host "  1. Create a release branch: release/api/v<version>"
  Write-Host "  2. Update version numbers in project files"
  Write-Host "  3. Update CHANGELOG.md with API section"
  Write-Host "  4. Create a commit with the changes"
  Write-Host "  5. Push the branch to remote"
  Write-Host "  6. Provide instructions for creating a PR"
}

# Function to validate version format
function Test-VersionFormat {
  param([string]$Version)
    
  if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Invalid version format: $Version"
    Write-Error "Version must be in format: MAJOR.MINOR.PATCH (e.g., 1.2.0)"
    exit 1
  }
    
  $parts = $Version -split '\.'
  $major, $minor, $patch = [int]$parts[0], [int]$parts[1], [int]$parts[2]
    
  if ($major -lt 0 -or $minor -lt 0 -or $patch -lt 0) {
    Write-Error "Version components must be non-negative integers"
    exit 1
  }
    
  Write-Success "Version format is valid: $Version"
}

# Function to check if we're in a git repository
function Test-GitRepository {
  if (-not (Test-Path ".git")) {
    Write-Error "Not in a git repository"
    exit 1
  }
    
  Write-Success "Git repository detected"
}

# Function to check if we're on main branch
function Test-MainBranch {
  $currentBranch = git branch --show-current
    
  if ($currentBranch -ne "main") {
    Write-Error "Not on main branch (current: $currentBranch)"
    Write-Error "Please switch to main branch first: git checkout main"
    exit 1
  }
    
  Write-Success "On main branch"
}

# Function to check if working directory is clean
function Test-CleanWorkingDirectory {
  $status = git status --porcelain
    
  if ($status) {
    Write-Error "Working directory is not clean"
    Write-Error "Please commit or stash your changes first"
    git status --short
    exit 1
  }
    
  Write-Success "Working directory is clean"
}

# Function to check if main is up to date
function Test-MainUpToDate {
  Write-Status "Checking if main branch is up to date..."
    
  git fetch origin
    
  $localCommit = git rev-parse HEAD
  $remoteCommit = git rev-parse origin/main
    
  if ($localCommit -ne $remoteCommit) {
    Write-Error "Main branch is not up to date with remote"
    Write-Error "Please pull latest changes: git pull origin main"
    exit 1
  }
    
  Write-Success "Main branch is up to date"
}

# Function to create release branch
function New-ReleaseBranch {
  param([string]$Version)
    
  $branchName = "release/api/v$Version"
    
  Write-Status "Creating release branch: $branchName"
    
  $branchExists = git show-ref --verify --quiet "refs/heads/$branchName"
  if ($LASTEXITCODE -eq 0) {
    Write-Error "Release branch already exists: $branchName"
    Write-Error "Please delete it first or use a different version"
    exit 1
  }
    
  git checkout -b $branchName
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create release branch"
    exit 1
  }
    
  Write-Success "Created release branch: $branchName"
}

# Function to update version in project file
function Update-ApiVersion {
  param([string]$Version)
    
  $projectFile = "src/Lintellect.Api/Lintellect.Api.csproj"
    
  Write-Status "Updating API version to $Version in $projectFile"
    
  if (-not (Test-Path $projectFile)) {
    Write-Error "Project file not found: $projectFile"
    exit 1
  }
    
  # Read the file content
  $content = Get-Content $projectFile -Raw
    
  # Update Version tag
  if ($content -match '<Version>.*</Version>') {
    $content = $content -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    Set-Content $projectFile -Value $content -NoNewline
  }
  else {
    Write-Error "Version tag not found in $projectFile"
    exit 1
  }
    
  Write-Success "Updated API version to $Version"
}

# Function to update CHANGELOG.md
function Update-Changelog {
  param([string]$Version)
    
  $changelogFile = "CHANGELOG.md"
    
  Write-Status "Updating CHANGELOG.md with API v$Version"
    
  if (-not (Test-Path $changelogFile)) {
    Write-Error "CHANGELOG.md not found"
    exit 1
  }
    
  # Get current date
  $date = Get-Date -Format "yyyy-MM-dd"
    
  # Read the file content
  $content = Get-Content $changelogFile -Raw
    
  # Create new API section
  $newSection = @"

## [API v$Version] - $date

### Added
- TBD

### Changed
- TBD

### Fixed
- TBD

### Security
- TBD

"@
    
  # Insert new section after [Unreleased]
  $content = $content -replace '(\[Unreleased\])', "`$1$newSection"
    
  # Write back to file
  Set-Content $changelogFile -Value $content -NoNewline
    
  Write-Success "Updated CHANGELOG.md with API v$Version section"
  Write-Warning "Please update the changelog content with actual changes"
}

# Function to create commit
function New-Commit {
  param([string]$Version)
    
  Write-Status "Creating commit for API v$Version release preparation"
    
  git add src/Lintellect.Api/Lintellect.Api.csproj CHANGELOG.md
    
  $commitMessage = @"
chore(api): prepare release v$Version

- Update API version to $Version
- Add API v$Version section to CHANGELOG.md

This commit prepares the API for release v$Version.
Please review and update the changelog content before merging.
"@
    
  git commit -m $commitMessage
    
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create commit"
    exit 1
  }
    
  Write-Success "Created commit for API v$Version"
}

# Function to push branch
function Push-Branch {
  param([string]$Version)
    
  $branchName = "release/api/v$Version"
    
  Write-Status "Pushing release branch to remote"
    
  git push -u origin $branchName
    
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push branch to remote"
    exit 1
  }
    
  Write-Success "Pushed release branch to remote"
}

# Function to show next steps
function Show-NextSteps {
  param([string]$Version)
    
  $branchName = "release/api/v$Version"
    
  Write-Host ""
  Write-Success "API release preparation completed!"
  Write-Host ""
  Write-Host "Next steps:"
  Write-Host "1. Update the CHANGELOG.md with actual changes for API v$Version"
  Write-Host "2. Review the changes: git diff main..$branchName"
  Write-Host "3. Create a Pull Request from $branchName to main"
  Write-Host "4. After PR is approved and merged, create the release tag:"
  Write-Host "   git tag -a api/v$Version -m `"Release API v$Version`""
  Write-Host "   git push origin api/v$Version"
  Write-Host "5. The release workflow will automatically:"
  Write-Host "   - Build and push Docker images"
  Write-Host "   - Create GitHub Release"
  Write-Host "   - Publish binary archives"
  Write-Host ""
  Write-Host "Branch: $branchName"
  Write-Host "Version: $Version"
  Write-Host ""
}

# Main execution
try {
  # Validate version
  Test-VersionFormat $Version
    
  # Check git repository
  Test-GitRepository
    
  # Check if on main branch
  Test-MainBranch
    
  # Check if working directory is clean
  Test-CleanWorkingDirectory
    
  # Check if main is up to date
  Test-MainUpToDate
    
  # Create release branch
  New-ReleaseBranch $Version
    
  # Update API version
  Update-ApiVersion $Version
    
  # Update CHANGELOG.md
  Update-Changelog $Version
    
  # Create commit
  New-Commit $Version
    
  # Push branch
  Push-Branch $Version
    
  # Show next steps
  Show-NextSteps $Version
}
catch {
  Write-Error "An error occurred: $($_.Exception.Message)"
  exit 1
}
