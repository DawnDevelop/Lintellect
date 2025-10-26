#!/bin/bash

# create-hotfix-api.sh
# Creates a new API hotfix branch for critical fixes

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
    echo "Usage: $0 <version> [description]"
    echo ""
    echo "Creates a new API hotfix branch for critical fixes"
    echo ""
    echo "Arguments:"
    echo "  version     The hotfix version number (e.g., 1.2.1)"
    echo "  description Optional description of the hotfix"
    echo ""
    echo "Examples:"
    echo "  $0 1.2.1"
    echo "  $0 1.2.1 \"Fix critical security vulnerability\""
    echo ""
    echo "The script will:"
    echo "  1. Create a hotfix branch: hotfix/api/<description>"
    echo "  2. Update version numbers in project files"
    echo "  3. Update CHANGELOG.md with hotfix entry"
    echo "  4. Create a commit with the changes"
    echo "  5. Push the branch to remote"
    echo "  6. Provide instructions for creating a PR"
}

# Function to validate version format
validate_version() {
    local version=$1
    
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format: $version"
        print_error "Version must be in format: MAJOR.MINOR.PATCH (e.g., 1.2.1)"
        exit 1
    fi
    
    # Extract major, minor, patch
    IFS='.' read -r major minor patch <<< "$version"
    
    if [ "$major" -lt 0 ] || [ "$minor" -lt 0 ] || [ "$patch" -lt 0 ]; then
        print_error "Version components must be non-negative integers"
        exit 1
    fi
    
    # Check if this is a patch version (hotfix)
    if [ "$patch" -eq 0 ]; then
        print_warning "Patch version is 0. Hotfixes typically increment the patch version."
        print_warning "Are you sure this is a hotfix? (y/N)"
        read -r response
        if [[ ! "$response" =~ ^[Yy]$ ]]; then
            print_error "Hotfix cancelled"
            exit 1
        fi
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

# Function to create hotfix branch
create_hotfix_branch() {
    local version=$1
    local description=$2
    local branch_name="hotfix/api/v$version"
    
    if [ -n "$description" ]; then
        # Create a safe branch name from description
        local safe_description=$(echo "$description" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | sed 's/^-\|-$//g')
        branch_name="hotfix/api/v$version-$safe_description"
    fi
    
    print_status "Creating hotfix branch: $branch_name"
    
    if git show-ref --verify --quiet "refs/heads/$branch_name"; then
        print_error "Hotfix branch already exists: $branch_name"
        print_error "Please delete it first or use a different version"
        exit 1
    fi
    
    git checkout -b "$branch_name"
    print_success "Created hotfix branch: $branch_name"
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
    
    # Update Version tag
    if grep -q "<Version>" "$project_file"; then
        sed -i.bak "s/<Version>.*<\/Version>/<Version>$version<\/Version>/" "$project_file"
        rm "$project_file.bak"
    else
        print_error "Version tag not found in $project_file"
        exit 1
    fi
    
    print_success "Updated API version to $version"
}

# Function to update CHANGELOG.md
update_changelog() {
    local version=$1
    local description=$2
    local changelog_file="CHANGELOG.md"
    
    print_status "Updating CHANGELOG.md with API v$version hotfix"
    
    if [ ! -f "$changelog_file" ]; then
        print_error "CHANGELOG.md not found"
        exit 1
    fi
    
    # Get current date
    local date=$(date +%Y-%m-%d)
    
    # Create temporary file with new hotfix section
    local temp_file=$(mktemp)
    
    # Add new hotfix section after [Unreleased]
    awk -v version="$version" -v date="$date" -v description="$description" '
    /^## \[Unreleased\]/ {
        print $0
        print ""
        print "## [API v" version "] - " date
        print ""
        if (description != "") {
            print "### Fixed"
            print "- " description
        } else {
            print "### Fixed"
            print "- TBD"
        }
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
    
    print_success "Updated CHANGELOG.md with API v$version hotfix section"
    print_warning "Please update the changelog content with actual changes"
}

# Function to create commit
create_commit() {
    local version=$1
    local description=$2
    
    print_status "Creating commit for API v$version hotfix"
    
    git add src/Lintellect.Api/Lintellect.Api.csproj CHANGELOG.md
    
    local commit_message="hotfix(api): prepare hotfix v$version"
    if [ -n "$description" ]; then
        commit_message="$commit_message

- $description"
    fi
    
    commit_message="$commit_message

- Update API version to $version
- Add API v$version hotfix section to CHANGELOG.md

This commit prepares the API for hotfix v$version.
Please review and update the changelog content before merging."
    
    git commit -m "$commit_message"
    
    print_success "Created commit for API v$version hotfix"
}

# Function to push branch
push_branch() {
    local version=$1
    local description=$2
    local branch_name="hotfix/api/v$version"
    
    if [ -n "$description" ]; then
        local safe_description=$(echo "$description" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | sed 's/^-\|-$//g')
        branch_name="hotfix/api/v$version-$safe_description"
    fi
    
    print_status "Pushing hotfix branch to remote"
    
    git push -u origin "$branch_name"
    
    print_success "Pushed hotfix branch to remote"
}

# Function to show next steps
show_next_steps() {
    local version=$1
    local description=$2
    local branch_name="hotfix/api/v$version"
    
    if [ -n "$description" ]; then
        local safe_description=$(echo "$description" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | sed 's/^-\|-$//g')
        branch_name="hotfix/api/v$version-$safe_description"
    fi
    
    echo ""
    print_success "API hotfix preparation completed!"
    echo ""
    echo "Next steps:"
    echo "1. Implement the hotfix in the branch: $branch_name"
    echo "2. Update the CHANGELOG.md with actual changes for API v$version"
    echo "3. Test the hotfix thoroughly"
    echo "4. Review the changes: git diff main..$branch_name"
    echo "5. Create a Pull Request from $branch_name to main with [HOTFIX] label"
    echo "6. After PR is approved and merged, create the release tag:"
    echo "   git tag -a api/v$version -m \"Hotfix API v$version\""
    echo "   git push origin api/v$version"
    echo "7. The release workflow will automatically:"
    echo "   - Build and push Docker images"
    echo "   - Create GitHub Release"
    echo "   - Publish binary archives"
    echo ""
    echo "Branch: $branch_name"
    echo "Version: $version"
    if [ -n "$description" ]; then
        echo "Description: $description"
    fi
    echo ""
    print_warning "This is a hotfix - please prioritize review and testing!"
}

# Main function
main() {
    local version=$1
    local description=$2
    
    # Check arguments
    if [ $# -lt 1 ] || [ $# -gt 2 ]; then
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
    
    # Create hotfix branch
    create_hotfix_branch "$version" "$description"
    
    # Update API version
    update_api_version "$version"
    
    # Update CHANGELOG.md
    update_changelog "$version" "$description"
    
    # Create commit
    create_commit "$version" "$description"
    
    # Push branch
    push_branch "$version" "$description"
    
    # Show next steps
    show_next_steps "$version" "$description"
}

# Run main function with all arguments
main "$@"
