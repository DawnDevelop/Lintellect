#!/bin/bash

# create-release-api.sh
# Creates a new API release branch and prepares for release

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 <version>"
    echo ""
    echo "Creates a new API release branch and prepares for release"
    echo ""
    echo "Arguments:"
    echo "  version    The version number (e.g., 1.2.0)"
    echo ""
    echo "Examples:"
    echo "  $0 1.2.0"
    echo "  $0 2.0.0"
    echo ""
    echo "The script will:"
    echo "  1. Create a release branch: release/api/v<version>"
    echo "  2. Update version numbers in project files (including Docker tags)"
    echo "  3. Update CHANGELOG.md with API section"
    echo "  4. Create a commit with the changes"
    echo "  5. Push the branch to remote"
    echo "  6. Provide instructions for creating a PR and GitHub Container Registry publishing"
}

# Function to validate version format
validate_version() {
    local version=$1
    
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format: $version"
        print_error "Version must be in format: MAJOR.MINOR.PATCH (e.g., 1.2.0)"
        exit 1
    fi
    
    # Extract major, minor, patch
    IFS='.' read -r major minor patch <<< "$version"
    
    if [ "$major" -lt 0 ] || [ "$minor" -lt 0 ] || [ "$patch" -lt 0 ]; then
        print_error "Version components must be non-negative integers"
        exit 1
    fi
    
    print_success "Version format is valid: $version"
}

# Function to check if we're in a git repository
check_git_repo() {
    if [ ! -d ".git" ]; then
        print_error "Not in a git repository"
        exit 1
    fi
    
    print_success "Git repository detected"
}

# Function to check if we're on main branch
check_main_branch() {
    local current_branch=$(git branch --show-current)
    
    if [ "$current_branch" != "main" ]; then
        print_error "Not on main branch (current: $current_branch)"
        print_error "Please switch to main branch first: git checkout main"
        exit 1
    fi
    
    print_success "On main branch"
}

# Function to check if working directory is clean
check_clean_working_directory() {
    if [ -n "$(git status --porcelain)" ]; then
        print_error "Working directory is not clean"
        print_error "Please commit or stash your changes first"
        git status --short
        exit 1
    fi
    
    print_success "Working directory is clean"
}

# Function to check if main is up to date
check_main_up_to_date() {
    print_status "Checking if main branch is up to date..."
    
    git fetch origin
    
    local local_commit=$(git rev-parse HEAD)
    local remote_commit=$(git rev-parse origin/main)
    
    if [ "$local_commit" != "$remote_commit" ]; then
        print_error "Main branch is not up to date with remote"
        print_error "Please pull latest changes: git pull origin main"
        exit 1
    fi
    
    print_success "Main branch is up to date"
}

# Function to create release branch
create_release_branch() {
    local version=$1
    local branch_name="release/api/v$version"
    
    print_status "Creating release branch: $branch_name"
    
    if git show-ref --verify --quiet "refs/heads/$branch_name"; then
        print_error "Release branch already exists: $branch_name"
        print_error "Please delete it first or use a different version"
        exit 1
    fi
    
    git checkout -b "$branch_name"
    print_success "Created release branch: $branch_name"
}

# Function to update version in project file
update_api_version() {
    local version=$1
    local project_file="src/Lintellect.Api/Lintellect.Api.csproj"
    
    print_status "Updating API version to $version in $project_file"
    
    if [ ! -f "$project_file" ]; then
        print_error "Project file not found: $project_file"
        exit 1
    fi
    
    # Create backup
    cp "$project_file" "$project_file.bak"
    
    # Update Version tag
    if grep -q "<Version>" "$project_file"; then
        sed -i "s/<Version>.*<\/Version>/<Version>$version<\/Version>/" "$project_file"
        print_status "Updated Version to $version"
    else
        print_warning "Version tag not found"
    fi
    
    # Update AssemblyVersion tag
    if grep -q "<AssemblyVersion>" "$project_file"; then
        sed -i "s/<AssemblyVersion>.*<\/AssemblyVersion>/<AssemblyVersion>$version.0<\/AssemblyVersion>/" "$project_file"
        print_status "Updated AssemblyVersion to $version.0"
    else
        print_warning "AssemblyVersion tag not found"
    fi
    
    # Update FileVersion tag
    if grep -q "<FileVersion>" "$project_file"; then
        sed -i "s/<FileVersion>.*<\/FileVersion>/<FileVersion>$version.0<\/FileVersion>/" "$project_file"
        print_status "Updated FileVersion to $version.0"
    else
        print_warning "FileVersion tag not found"
    fi
    
    # Update InformationalVersion tag
    if grep -q "<InformationalVersion>" "$project_file"; then
        sed -i "s/<InformationalVersion>.*<\/InformationalVersion>/<InformationalVersion>$version<\/InformationalVersion>/" "$project_file"
        print_status "Updated InformationalVersion to $version"
    else
        print_warning "InformationalVersion tag not found"
    fi
    
    # Update ContainerImageTag tag
    if grep -q "<ContainerImageTag>" "$project_file"; then
        sed -i "s/<ContainerImageTag>.*<\/ContainerImageTag>/<ContainerImageTag>$version<\/ContainerImageTag>/" "$project_file"
        print_status "Updated ContainerImageTag to $version"
    else
        print_warning "ContainerImageTag tag not found"
    fi
    
    # Remove backup
    rm "$project_file.bak"
    
    print_success "Updated API version to $version"
}

# Function to update CHANGELOG.md
update_changelog() {
    local version=$1
    local changelog_file="CHANGELOG.md"
    
    print_status "Updating CHANGELOG.md with API v$version"
    
    if [ ! -f "$changelog_file" ]; then
        print_error "CHANGELOG.md not found"
        exit 1
    fi
    
    # Get current date
    local date=$(date +%Y-%m-%d)
    
    # Create temporary file with new API section
    local temp_file=$(mktemp)
    
    # Add new API section after [Unreleased]
    awk -v version="$version" -v date="$date" '
    /^## \[Unreleased\]/ {
        print $0
        print ""
        print "## [API v" version "] - " date
        print ""
        print "### Added"
        print "- TBD"
        print ""
        print "### Changed"
        print "- TBD"
        print ""
        print "### Fixed"
        print "- TBD"
        print ""
        print "### Security"
        print "- TBD"
        print ""
        next
    }
    { print }
    ' "$changelog_file" > "$temp_file"
    
    # Replace original file
    mv "$temp_file" "$changelog_file"
    
    print_success "Updated CHANGELOG.md with API v$version section"
    print_warning "Please update the changelog content with actual changes"
}

# Function to create commit
create_commit() {
    local version=$1
    
    print_status "Creating commit for API v$version release preparation"
    
    git add src/Lintellect.Api/Lintellect.Api.csproj CHANGELOG.md
    
    git commit -m "chore(api): prepare release v$version

- Update API version to $version
- Add API v$version section to CHANGELOG.md

This commit prepares the API for release v$version.
Please review and update the changelog content before merging."
    
    print_success "Created commit for API v$version"
}

# Function to push branch
push_branch() {
    local version=$1
    local branch_name="release/api/v$version"
    
    print_status "Pushing release branch to remote"
    
    git push -u origin "$branch_name"
    
    print_success "Pushed release branch to remote"
}

# Function to show next steps
show_next_steps() {
    local version=$1
    local branch_name="release/api/v$version"
    
    echo ""
    print_success "API release preparation completed!"
    echo ""
    echo "Next steps:"
    echo "1. Update the CHANGELOG.md with actual changes for API v$version"
    echo "2. Review the changes: git diff main..$branch_name"
    echo "3. Create a Pull Request from $branch_name to main"
    echo "4. After PR is approved and merged, create the release tag:"
    echo "   git tag -a api/v$version -m \"Release API v$version\""
    echo "   git push origin api/v$version"
    echo "5. The release workflow will automatically:"
    echo "   - Build and push Docker images to GitHub Container Registry"
    echo "   - Create GitHub Release with binary archives"
    echo "   - Publish container images with tags:"
    echo "     - ghcr.io/[REPO_OWNER]/lintellect-api:$version"
    echo "     - ghcr.io/[REPO_OWNER]/lintellect-api:latest"
    echo "     - ghcr.io/[REPO_OWNER]/lintellect-api:$(echo $version | cut -d. -f1)"
    echo "     - ghcr.io/[REPO_OWNER]/lintellect-api:$(echo $version | cut -d. -f1-2)"
    echo ""
    echo "Docker Usage:"
    echo "  docker pull ghcr.io/[REPO_OWNER]/lintellect-api:$version"
    echo "  docker run -p 7000:7000 ghcr.io/[REPO_OWNER]/lintellect-api:$version"
    echo ""
    echo "Branch: $branch_name"
    echo "Version: $version"
    echo "Container Registry: ghcr.io/[REPO_OWNER]/lintellect-api"
    echo ""
}

# Main function
main() {
    local version=$1
    
    # Check arguments
    if [ $# -ne 1 ]; then
        print_error "Invalid number of arguments"
        echo ""
        show_usage
        exit 1
    fi
    
    # Validate version
    validate_version "$version"
    
    # Check git repository
    check_git_repo
    
    # Check if on main branch
    check_main_branch
    
    # Check if working directory is clean
    check_clean_working_directory
    
    # Check if main is up to date
    check_main_up_to_date
    
    # Create release branch
    create_release_branch "$version"
    
    # Update API version
    update_api_version "$version"
    
    # Update CHANGELOG.md
    update_changelog "$version"
    
    # Create commit
    create_commit "$version"
    
    # Push branch
    push_branch "$version"
    
    # Show next steps
    show_next_steps "$version"
}

# Run main function with all arguments
main "$@"
