# Publish Lintellect CLI tool to NuGet feed (Azure DevOps or NuGet.org)
# Usage: .\publish-cli-tool.ps1 -NuGetSource "https://pkgs.dev.azure.com/{organization}/_packaging/{feedName}/nuget/v3/index.json" -ApiKey "your-pat" [-Version "1.0.0"]
# Or:    .\publish-cli-tool.ps1 -NuGetSource "https://pkgs.dev.azure.com/{organization}/{project}/_packaging/{feedName}/nuget/v3/index.json" -ApiKey "your-pat" [-Version "1.0.0"]

param(
    [Parameter(Mandatory = $true)]
    [string]$NuGetSource,
    
    [Parameter(Mandatory = $true)]
    [string]$ApiKey,
    
    [Parameter(Mandatory = $false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing Lintellect CLI tool v$Version" -ForegroundColor Green
Write-Host "Target: $NuGetSource" -ForegroundColor Cyan

# Validate NuGet source URL
if ([string]::IsNullOrWhiteSpace($NuGetSource)) {
    Write-Error "NuGetSource parameter cannot be empty"
    exit 1
}

# Detect if this is an Azure DevOps feed
$isAzureDevOpsFeed = $NuGetSource -match "pkgs\.dev\.azure\.com|\.visualstudio\.com.*_packaging"

if ($isAzureDevOpsFeed) {
    Write-Host "Detected Azure DevOps Artifacts feed" -ForegroundColor Yellow
    
    # Validate Azure DevOps feed URL format
    if (-not ($NuGetSource -match "nuget/v3/index\.json$")) {
        Write-Warning "Azure DevOps feed URL should end with '/nuget/v3/index.json'"
        Write-Host "Example: https://pkgs.dev.azure.com/{organization}/_packaging/{feedName}/nuget/v3/index.json" -ForegroundColor Cyan
    }
    
    # Validate PAT is not empty
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Error "API Key (PAT) is required for Azure DevOps feeds"
        Write-Host ""
        Write-Host "To create a PAT:" -ForegroundColor Cyan
        Write-Host "1. Go to Azure DevOps ? User Settings ? Personal Access Tokens" -ForegroundColor White
        Write-Host "2. Click 'New Token'" -ForegroundColor White
        Write-Host "3. Set Scopes: 'Packaging (Read, Write & Manage)'" -ForegroundColor White
        Write-Host "4. Copy the token and use it as -ApiKey parameter" -ForegroundColor White
        exit 1
    }
}

# Set the project path
$projectPath = "src\Lintellect.Cli\Lintellect.Cli.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found at: $projectPath"
    exit 1
}

# Update version in project file if specified
if ($Version) {
    Write-Host "Setting version to $Version..." -ForegroundColor Yellow
    $content = Get-Content $projectPath -Raw
    
    if ($content -match '<Version>.*?</Version>') {
        $content = $content -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
        Set-Content $projectPath $content -NoNewline
        Write-Host "Version updated successfully" -ForegroundColor Green
    }
    else {
        Write-Warning "No <Version> element found in project file"
    }
}

# Clean previous packages
Write-Host "Cleaning previous packages..." -ForegroundColor Yellow
$packagesPath = "src\Lintellect.Cli\nupkg"
if (Test-Path $packagesPath) {
    Remove-Item $packagesPath -Recurse -Force
    Write-Host "Cleaned $packagesPath" -ForegroundColor Green
}

# Restore dependencies first
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $projectPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore dependencies"
    exit 1
}

# Pack the project
Write-Host "Packing project..." -ForegroundColor Yellow
dotnet pack $projectPath --configuration $Configuration --output $packagesPath --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack project"
    exit 1
}

# Find the generated package
$package = Get-ChildItem "$packagesPath\*.nupkg" | Select-Object -First 1

if (-not $package) {
    Write-Error "Package file not found in $packagesPath"
    exit 1
}

Write-Host "Package created: $($package.Name)" -ForegroundColor Green
Write-Host "Package size: $([math]::Round($package.Length / 1MB, 2)) MB" -ForegroundColor Cyan

# For Azure DevOps, configure authentication properly
if ($isAzureDevOpsFeed) {
    Write-Host "Configuring Azure DevOps authentication..." -ForegroundColor Yellow
    
    # Extract feed name from URL for source name
    $feedName = "AzureDevOpsFeed"
    if ($NuGetSource -match "/_packaging/([^/]+)/") {
        $feedName = $matches[1]
    }
    
    Write-Host "Feed name: $feedName" -ForegroundColor Cyan
    
    # Remove existing source if it exists to avoid conflicts
    $existingSources = dotnet nuget list source 2>&1
    if ($existingSources -match $feedName) {
        Write-Host "Removing existing source '$feedName'..." -ForegroundColor Yellow
        dotnet nuget remove source $feedName 2>&1 | Out-Null
    }
    
    # Add source with credentials
    Write-Host "Adding authenticated NuGet source..." -ForegroundColor Yellow
    dotnet nuget add source $NuGetSource `
        --name $feedName `
        --username "AzureDevOps" `
        --password $ApiKey `
        --store-password-in-clear-text `
        --configfile nuget.config
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to add NuGet source to config, will try direct push..."
    }
    else {
        Write-Host "NuGet source configured successfully" -ForegroundColor Green
    }
}

# Push to NuGet feed
Write-Host "" 
Write-Host "Publishing to NuGet feed..." -ForegroundColor Yellow
Write-Host "This may take a moment..." -ForegroundColor Cyan

try {
    if ($isAzureDevOpsFeed) {
        # For Azure DevOps, push using the configured source with credentials
        # The password/PAT is provided via the --password parameter
        dotnet nuget push $package.FullName `
            --source $NuGetSource `
            --api-key "AzureDevOps" `
            --skip-duplicate `
            --interactive
    }
    else {
        # For other feeds (like nuget.org), use the API key directly
        dotnet nuget push $package.FullName `
            --source $NuGetSource `
            --api-key $ApiKey `
            --skip-duplicate
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Push command failed. Trying alternative authentication method..." -ForegroundColor Yellow
        
        # Alternative: Use VSS_NUGET_EXTERNAL_FEED_ENDPOINTS environment variable
        $feedEndpoint = @{
            endpointCredentials = @(
                @{
                    endpoint = $NuGetSource
                    username = "AzureDevOps" 
                    password = $ApiKey
                }
            )
        } | ConvertTo-Json -Compress
        
        $env:VSS_NUGET_EXTERNAL_FEED_ENDPOINTS = $feedEndpoint
        
        dotnet nuget push $package.FullName `
            --source $NuGetSource `
            --api-key "AzureDevOps" `
            --skip-duplicate
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to publish package using alternative authentication"
        }
    }
}
catch {
    Write-Error "Failed to publish package to feed: $_"
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Verify your PAT has 'Packaging (Read, Write & Manage)' scope" -ForegroundColor White
    Write-Host "2. Check if the feed URL is correct" -ForegroundColor White
    Write-Host "3. Ensure you have permission to push to this feed" -ForegroundColor White
    Write-Host "4. Try running: dotnet nuget list source" -ForegroundColor White
    Write-Host "5. Test authentication: dotnet nuget push --help" -ForegroundColor White
    exit 1
}

Write-Host "" 
Write-Host "? Successfully published $($package.Name) to $NuGetSource" -ForegroundColor Green
Write-Host ""

# Clean up - remove the temporary source
if ($isAzureDevOpsFeed) {
    Write-Host "Cleaning up temporary NuGet source..." -ForegroundColor Yellow
    dotnet nuget remove source $feedName 2>&1 | Out-Null
}

# Provide installation instructions
if ($isAzureDevOpsFeed) {
    Write-Host "To install the tool globally:" -ForegroundColor Cyan
    Write-Host "  dotnet nuget add source $NuGetSource --name DevOpsFeed --username AzureDevOps --password YOUR_PAT --store-password-in-clear-text" -ForegroundColor White
    Write-Host "  dotnet tool install -g lintellect.cli --add-source DevOpsFeed" -ForegroundColor White
    Write-Host ""
    Write-Host "Or in Azure DevOps pipeline:" -ForegroundColor Cyan
    Write-Host "  - task: NuGetAuthenticate@1" -ForegroundColor White
    Write-Host "  - task: DotNetCoreCLI@2" -ForegroundColor White
    Write-Host "    inputs:" -ForegroundColor White
    Write-Host "      command: 'custom'" -ForegroundColor White
    Write-Host "      custom: 'tool'" -ForegroundColor White
    Write-Host "      arguments: 'install lintellect.cli --tool-path ./tools'" -ForegroundColor White
    Write-Host ""
    Write-Host "Note: In Azure Pipelines, NuGetAuthenticate@1 task handles authentication automatically" -ForegroundColor Yellow
}
else {
    Write-Host "To install the tool globally:" -ForegroundColor Cyan
    Write-Host "  dotnet tool install -g lintellect.cli --add-source $NuGetSource" -ForegroundColor White
    Write-Host ""
    Write-Host "Or in a CI/CD pipeline:" -ForegroundColor Cyan
    Write-Host "  dotnet tool install lintellect.cli --tool-path ./tools --add-source $NuGetSource" -ForegroundColor White
}

Write-Host ""
Write-Host "Package details:" -ForegroundColor Cyan
Write-Host "  Name: lintellect.cli" -ForegroundColor White
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Command: lintellect" -ForegroundColor White
