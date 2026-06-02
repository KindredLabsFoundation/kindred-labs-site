# Integration and E2E Testing Infrastructure

This directory contains the infrastructure for running integration and end-to-end (E2E) tests.

## 1. Test Database Setup

Integration tests run against a real PostgreSQL database. To set up the test database:

1. Ensure your Docker PostgreSQL instance is running.
2. Run the `create_test_db.sql` script against the Docker instance to create the `kindredlabs_test` database.
   ```bash
   psql -h localhost -U postgres -f KindredLabs.Tests/Infrastructure/create_test_db.sql
   ```
3. The connection string is configured in `KindredLabs.Tests/appsettings.test.json`.

## 2. Integration Tests

Integration tests use `WebApplicationFactory` to host the application in-memory and `HttpClient` to make requests.

- **Base Class**: `IntegrationTestBase`
- **Features**:
  - Automatically applies EF Core migrations to the test database on startup.
  - `CleanDatabaseAsync()` resets the database state between tests.
  - `LoginAsTestUserAsync()` and `GetAntiForgeryTokenAsync()` helpers for authenticated scenarios.
- **How to run**:
  ```bash
  dotnet test --filter Category=Integration
  ```

## 3. E2E Tests (Playwright)

E2E tests use Playwright to automate a real browser.

- **Base Class**: `PlaywrightTestBase` (extends `PageTest` from `Microsoft.Playwright.NUnit`)
- **How to run**:
  1. Start the application locally:
     ```bash
     dotnet watch --project KindredLabs.Web
     ```
  2. Run the E2E tests:
     ```bash
     dotnet test --filter Category=E2E
     ```
- **Note**: Ensure Playwright browsers are installed:
  ```bash
  pwsh KindredLabs.Tests/bin/Debug/net10.0/playwright.ps1 install
  ```

## 4. Test Categories

- `[Xunit.Categories.Category("Integration")]`: For integration tests.
- `[NUnit.Framework.Category("E2E")]`: For Playwright E2E tests.
