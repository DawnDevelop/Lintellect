refactor: Clean up GitHub Actions workflows and reorganize test structure

## Workflow Improvements

### Fixed Redundant Testing
- **ci.yml**: Separated unit tests from functional tests to eliminate redundancy
  - `build-and-test` job now runs only unit tests (excludes functional tests)
  - Renamed `integration-tests` to `functional-tests` for clarity
  - Functional tests now run in a separate job with proper dependencies
  - Code coverage job now collects results from both test types

### Removed Duplicate Jobs
- Removed redundant `main-branch-build` job from `ci.yml`
  - This job duplicated functionality already handled by `main-build.yml`
  - Prevents duplicate builds and tests on main branch pushes

### Enhanced Test Coverage
- **main-build.yml**: Added functional tests (previously excluded)
  - Now runs both unit and functional tests for complete coverage
- **release-api.yml**: Streamlined test execution
  - Separated unit and functional test steps for better clarity
- **release-cli.yml**: Streamlined test execution
  - Focused on CLI-specific tests only

### Improved Code Coverage
- Code coverage job now properly merges results from:
  - Unit test results
  - Functional test results
- Both test result artifacts are downloaded and combined for accurate coverage reporting

## Test Structure Reorganization

### Utilities Reorganization
- Moved test utilities into domain-specific folders:
  - `Utilities/Analysis/TestDataBuilder.cs` - Analysis test data
  - `Utilities/Webhooks/WebhookTestDataBuilder.cs` - Webhook test data
  - `Utilities/Http/HttpClientExtensions.cs` - HTTP client extensions

### Command Tests Reorganization
- Organized by domain into subfolders:
  - `CommandTests/Analysis/` - Analysis command tests
  - `CommandTests/Webhooks/` - Webhook command tests

### API Tests Reorganization
- Organized by domain into subfolders:
  - `ApiTests/Analysis/` - Analysis API tests
  - `ApiTests/Webhooks/` - Webhook API tests

### Added Missing Tests
- `UpdateAnalysisJobStatusCommandTests` - Tests for status updates
- `DeleteAnalysisHistoryCommandTests` - Tests for history deletion
- `UpdateWebhookStatusCommandTests` - Tests for webhook status updates
- `DeleteAnalysisHistoryApiTests` - API endpoint tests

## Benefits

1. **Eliminated Redundancy**: No duplicate test execution
2. **Better Organization**: Tests organized by domain for easier maintenance
3. **Complete Coverage**: All test types run in appropriate workflows
4. **Faster CI**: Parallel execution of unit and functional tests
5. **Accurate Coverage**: Combined coverage from all test types

## Testing

- All 41 functional tests pass
- All unit tests pass
- Workflows validated for correct job dependencies
- Code coverage collection verified

