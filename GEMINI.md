# Gemini Agent Guidance — TaskTracker

This document provides architectural context, coding standards, and operational guidelines for AI agents working on the TaskTracker project.

## Project Overview
TaskTracker is a .NET 8.0 MVC application for managing clients, projects, time entries, expenses, and invoices. It uses EF Core with SQL Server (and support for SQLite in dev) and ASP.NET Core Identity.

## Core Mandates
- **Allman Style Braces**: Place braces on a new line for classes, methods, and control flow blocks.
- **Service Layer Pattern**: Business logic should reside in the `Services/` directory. Controllers should remain thin and delegate to services.
- **Global Context**: The `GlobalClientFilter` manages the active client context via `ViewData`. Respect the `GlobalClientId` when filtering data in services and controllers.
- **Frontend Bundling**: The project uses Gulp for asset management. After modifying files in `wwwroot/js/` or `wwwroot/css/`, run `gulp` to update minified bundles.

## Architecture & Patterns
- **Controllers**: Plural nouns (e.g., `InvoicesController`). Use constructor injection for services.
- **Models**: Singular nouns (e.g., `Invoice`). Data models are located in subfolders within `Models/` (e.g., `Models/Invoice/Invoice.cs`).
- **Services**: All business logic and database access (via `AppDbContext`) should be encapsulated here.
- **Migrations**: Use EF Core migrations for schema changes (`dotnet ef migrations add <Name>`). Always verify the generated migration before applying.
- **Identity**: Uses `ApplicationUser` extending `IdentityUser`.
- **Global Filters**: `GlobalClientFilter` is registered in `Program.cs` and injects `ViewData` for client selection and record limits.

## Coding Standards
### C# Conventions
- **Naming**: PascalCase for public types/methods; camelCase with underscore prefix (`_`) for private fields; camelCase for locals/parameters.
- **Typing**: Use explicit types (e.g., `int i = 0`) instead of `var` when the type is not immediately obvious from the assignment.
- **Async/Await**: Use asynchronous programming for all I/O-bound operations (DB, File, Network).
- **Dependency Injection**: Use constructor injection. Prefer interfaces where abstraction is beneficial, but direct service injection is common in this project.
- **Nullability**: Follow .NET 8 nullable reference type settings. Be explicit about intended nullability.

### Razor & Frontend
- **Views**: Organized by controller name. Shared components go in `Views/Shared/`.
- **Styling**: Vanilla CSS. Bundled via Gulp.
- **JavaScript**: Modular JS files in `wwwroot/js/`, bundled via Gulp into `site.min.js`.

### Testing
- **Structure**: Arrange, Act, Assert.
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior`.
- **Coverage**: Prioritize business logic in Services for unit testing. Use integration tests for data-heavy operations.

## Key Components & Configuration
- **Settings**: The `Settings` model stores global configuration (SMTP, BCC, Company Info, Invoice Template) in the database. Managed via `SettingsController` and `SettingsService`.
- **User Preferences**: `IUserPreferenceService` tracks per-user settings like preferred client and UI record limits.
- **Data Protection**: Managed via `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, keys stored in the database.
- **Logging**: Serilog is used for structured logging, configured in `Program.cs`.

## Security
- **Secrets**: Never hardcode secrets. Use `appsettings.json`, environment variables, or User Secrets for local development.
- **PII**: Avoid logging personally identifiable information.
- **Validation**: Always validate user input at both the UI (Razor/JS) and Service levels.

## Operational Guidelines
### Git Conventions
- **Commit Messages**: Follow the format `scope: short description` (e.g., `invoices: fix null ref in Create`).
- **Commits**: Provide a one-line summary and a short body describing the "why" and "what".
- **Migrations**: When adding a migration, include the command used (`dotnet ef migrations add <Name>`) in the commit description.

### Interaction Style
- **Proactive suggestions**: When asked for architectural changes, provide 2-3 alternatives with pros/cons.
- **Incremental changes**: Prefer small, testable diffs over large rewrites.
- **Verification**: Always suggest or run tests after making changes.

## Tooling Commands
- **Migrations**: `dotnet ef migrations add <Name>`
- **Database Update**: `dotnet ef database update`
- **Asset Bundling**: `npx gulp` (or `gulp` if globally installed)
- **Run Tests**: `dotnet test`
