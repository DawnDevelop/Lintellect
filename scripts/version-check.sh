#!/bin/bash

# version-check.sh
# Verifies version consistency across project files

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
    echo "Usage: $0 [--fix]"
    echo ""
    echo "Verifies version consistency across project files"
    echo ""
    echo "Options:"
    echo "  --fix    Automatically fix version inconsistencies (use with caution)"
    echo ""
    echo "The script will:"
    echo "  1. Check version consistency across all project files"
    echo "  2. Verify CHANGELOG.md has corresponding sections"
    echo "  3. Report any inconsistencies found"
    echo "  4. Optionally fix inconsistencies if --fix is specified"
}

# Function to extract version from project file
extract_version() {
    local file=$1
    local tag=$2
    
    if [ ! -f "$file" ]; then
        echo ""
        return
    fi
    
    if [ "$tag" = "Version" ]; then
        grep -oP '<Version>\K[^<]+' "$file" 2>/dev/null || echo ""
    elif [ "$tag" = "PackageVersion" ]; then
        grep -oP '<PackageVersion>\K[^<]+' "$file" 2>/dev/null || echo ""
    else
        echo ""
    fi
}

# Function to check API version
check_api_version() {
    local api_file="src/Lintellect.Api/Lintellect.Api.csproj"
    local version=$(extract_version "$api_file" "Version")
    
    if [ -z "$version" ]; then
        print_error "No Version tag found in $api_file"
        return 1
    fi
    
    print_success "API version: $version"
    echo "$version"
}

# Function to check CLI version
check_cli_version() {
    local cli_file="src/Lintellect.Cli/Lintellect.Cli.csproj"
    local version=$(extract_version "$cli_file" "Version")
    local package_version=$(extract_version "$cli_file" "PackageVersion")
    
    if [ -z "$version" ]; then
        print_error "No Version tag found in $cli_file"
        return 1
    fi
    
    if [ -z "$package_version" ]; then
        print_error "No PackageVersion tag found in $cli_file"
        return 1
    fi
    
    if [ "$version" != "$package_version" ]; then
        print_error "Version mismatch in $cli_file: Version=$version, PackageVersion=$package_version"
        return 1
    fi
    
    print_success "CLI version: $version (Version and PackageVersion match)"
    echo "$version"
}

# Function to check Shared version
check_shared_version() {
    local shared_file="src/Lintellect.Shared/Lintellect.Shared.csproj"
    local version=$(extract_version "$shared_file" "Version")
    
    if [ -z "$version" ]; then
        print_warning "No Version tag found in $shared_file"
        return 0
    fi
    
    print_success "Shared version: $version"
    echo "$version"
}

# Function to check CHANGELOG.md
check_changelog() {
    local changelog_file="CHANGELOG.md"
    
    if [ ! -f "$changelog_file" ]; then
        print_error "CHANGELOG.md not found"
        return 1
    fi
    
    print_status "Checking CHANGELOG.md..."
    
    # Check for proper format
    if ! grep -q "^# Changelog" "$changelog_file"; then
        print_error "CHANGELOG.md should start with '# Changelog'"
        return 1
    fi
    
    # Check for unreleased section
    if ! grep -q "## \[Unreleased\]" "$changelog_file"; then
        print_warning "CHANGELOG.md should have an [Unreleased] section"
    fi
    
    # Extract API versions from changelog
    local api_versions=$(grep -oE "## \[API v[0-9]+\.[0-9]+\.[0-9]+\]" "$changelog_file" | sed 's/## \[API v//' | sed 's/\]//' || true)
    
    # Extract CLI versions from changelog
    local cli_versions=$(grep -oE "## \[CLI v[0-9]+\.[0-9]+\.[0-9]+\]" "$changelog_file" | sed 's/## \[CLI v//' | sed 's/\]//' || true)
    
    if [ -n "$api_versions" ]; then
        print_success "Found API versions in CHANGELOG.md: $api_versions"
    else
        print_warning "No API versions found in CHANGELOG.md"
    fi
    
    if [ -n "$cli_versions" ]; then
        print_success "Found CLI versions in CHANGELOG.md: $cli_versions"
    else
        print_warning "No CLI versions found in CHANGELOG.md"
    fi
    
    echo "$api_versions|$cli_versions"
}

# Function to check version consistency
check_consistency() {
    local api_version=$1
    local cli_version=$2
    local shared_version=$3
    local changelog_info=$4
    
    print_status "Checking version consistency..."
    
    local issues=0
    
    # Check if API and Shared versions match (Shared should follow API)
    if [ -n "$api_version" ] && [ -n "$shared_version" ] && [ "$api_version" != "$shared_version" ]; then
        print_warning "API version ($api_version) and Shared version ($shared_version) don't match"
        print_warning "Shared version typically follows API version"
        ((issues++))
    fi
    
    # Check if versions are in CHANGELOG.md
    local api_versions=$(echo "$changelog_info" | cut -d'|' -f1)
    local cli_versions=$(echo "$changelog_info" | cut -d'|' -f2)
    
    if [ -n "$api_version" ] && [ -n "$api_versions" ]; then
        if echo "$api_versions" | grep -q "$api_version"; then
            print_success "API version $api_version found in CHANGELOG.md"
        else
            print_warning "API version $api_version not found in CHANGELOG.md"
            ((issues++))
        fi
    fi
    
    if [ -n "$cli_version" ] && [ -n "$cli_versions" ]; then
        if echo "$cli_versions" | grep -q "$cli_version"; then
            print_success "CLI version $cli_version found in CHANGELOG.md"
        else
            print_warning "CLI version $cli_version not found in CHANGELOG.md"
            ((issues++))
        fi
    fi
    
    return $issues
}

# Function to fix version inconsistencies
fix_versions() {
    local api_version=$1
    local cli_version=$2
    local shared_version=$3
    
    print_status "Fixing version inconsistencies..."
    
    # Fix Shared version to match API version
    if [ -n "$api_version" ] && [ -n "$shared_version" ] && [ "$api_version" != "$shared_version" ]; then
        local shared_file="src/Lintellect.Shared/Lintellect.Shared.csproj"
        print_status "Updating Shared version from $shared_version to $api_version"
        
        if grep -q "<Version>" "$shared_file"; then
            sed -i.bak "s/<Version>.*<\/Version>/<Version>$api_version<\/Version>/" "$shared_file"
            rm "$shared_file.bak"
            print_success "Updated Shared version to $api_version"
        else
            print_error "No Version tag found in $shared_file"
        fi
    fi
}

# Function to generate summary
generate_summary() {
    local api_version=$1
    local cli_version=$2
    local shared_version=$3
    local issues=$4
    
    echo ""
    echo "## Version Check Summary"
    echo "========================"
    echo ""
    echo "API Version:    ${api_version:-"Not found"}"
    echo "CLI Version:    ${cli_version:-"Not found"}"
    echo "Shared Version: ${shared_version:-"Not found"}"
    echo ""
    
    if [ $issues -eq 0 ]; then
        print_success "All version checks passed!"
        echo ""
        echo "✅ All project files have consistent versions"
        echo "✅ CHANGELOG.md format is valid"
        echo "✅ No version conflicts detected"
    else
        print_warning "Found $issues issue(s) that need attention"
        echo ""
        echo "⚠️  Please review the warnings above"
        echo "⚠️  Consider running with --fix to automatically resolve some issues"
    fi
}

# Main function
main() {
    local fix_mode=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --fix)
                fix_mode=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    print_status "Starting version consistency check..."
    echo ""
    
    # Check versions
    local api_version=$(check_api_version)
    local cli_version=$(check_cli_version)
    local shared_version=$(check_shared_version)
    local changelog_info=$(check_changelog)
    
    echo ""
    
    # Check consistency
    local issues=0
    check_consistency "$api_version" "$cli_version" "$shared_version" "$changelog_info"
    issues=$?
    
    # Fix versions if requested
    if [ "$fix_mode" = true ] && [ $issues -gt 0 ]; then
        echo ""
        fix_versions "$api_version" "$cli_version" "$shared_version"
    fi
    
    # Generate summary
    generate_summary "$api_version" "$cli_version" "$shared_version" "$issues"
    
    # Exit with appropriate code
    if [ $issues -eq 0 ]; then
        exit 0
    else
        exit 1
    fi
}

# Run main function with all arguments
main "$@"
