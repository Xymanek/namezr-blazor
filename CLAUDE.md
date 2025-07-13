# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a **Blazor Web App** solution using .NET 9 with a hybrid SSR/WebAssembly approach:

- **Namezr** - Main server project using static rendering only (no `InteractiveServer` or `InteractiveAuto`)
- **Namezr.Client** - Interactive WebAssembly client for components requiring interactivity 
- **Namezr.BackendServiceDefaults** - Shared backend configuration
- **Namzer.BlazorPortals** - Custom portal component library

### Key Technologies
- **Entity Framework Core** with PostgreSQL and NodaTime
- **ASP.NET Core Identity** with external auth (Twitch, Discord, Patreon, Google)
- **Havit.Blazor** UI library (prefer over raw Bootstrap)
- **Immediate.Apis** for HTTP endpoints
- **OpenTelemetry** and **Sentry** for observability
- **AutoConstructor** and **AutoRegisterInject** for dependency injection

### Code Organization
The **Namezr** project follows feature-based organization:
- `Features/[Feature]/[Type]/[ClassName].cs` - Feature-specific code
- `Features/[Feature]/[Type]/[ClassName].razor` - Blazor components
- `Infrastructure/[Category]/` - Cross-cutting concerns
- `Helpers/[Category]/` - Miscellaneous utilities

Key features: Consumers, Creators, Eligibility, Files, Identity, Notifications, Polls, Questionnaires, SelectionSeries.

## Development Commands

### Build and Run
```bash
# Build solution
dotnet build

# Run main application (with database migration if needed)
dotnet run --project Namezr

# Database migration only
dotnet run --project Namezr migrate-db
```

### Database Operations
EF Core migrations are in `Namezr/Infrastructure/Data/Migrations/`. The connection string is configured via `postgresdb` in configuration.

## Configuration
Requires configuration for:
- PostgreSQL connection string (`postgresdb`)
- External OAuth providers (Twitch, Discord, Patreon, Google)
- Email sender (SMTP)
- File storage path
- Sentry DSN (optional)

See README.md for complete configuration example.

## Development Guidelines

### Rendering Modes
- **Namezr project**: Static rendering ONLY
- **Namezr.Client project**: `InteractiveWebAssembly` for interactive components

### UI Components
- Prefer **Havit.Blazor** components over raw HTML
- Fall back to **Bootstrap 5.3** CSS classes
- Use Bootstrap CSS helpers instead of custom CSS files

### Code Style
- Use `[AutoConstructor]` or primary constructors for DI
- Use `[RegisterSingleton/Scoped/Transient]` attributes for service registration
- Prefer explicit types over `var` (unless type name > 30 chars)
- Use trailing commas in initializers
- Use trailing commas where possible and it's the last character of the line

### Data Access
- Use `ApplicationDbContext` with feature-specific partial classes
- Entity configurations follow the established patterns
- Use `IDbContextFactory<ApplicationDbContext>` for background services

### Authentication
- External providers are configured with proper scopes
- Custom authentication handlers for Discord and Twitch mock server
- Token refreshing is handled automatically

## Special Features

### Mock Server Support
Development environment supports Twitch mock server via `Twitch:MockServerUrl` configuration.

### File Upload System
Uses ticket-based file uploads with storage in configured path. Files are referenced by GUID.

### Command Line Tools
```bash
# Print third-party tokens for debugging
dotnet run --project Namezr print-third-party-token [args]
```

## Database Guidelines
- Enums should be stored as numbers in the database. Such enums should have all values explicitly specified with a comment that the values must never change

## API Development Guidelines
- **API Route Management**: 
  - All API routes need to be constants in @Namezr.Client\ApiEndpointPaths.cs 

### Command Line Startup Validation
- To validate that the application runs, use `validate-startup` launch profile