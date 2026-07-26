# StockHub API Agent Guide

## Purpose
This repository contains StockHub's .NET backend, PostgreSQL persistence, crawler services, and .NET Aspire host. The API targets .NET 10 and uses ASP.NET Core, Entity Framework Core, and Npgsql.

## Primary Commands
Run commands from the repository root unless noted:

- `dotnet build api/StockHub.csproj` - build the API and its project references.
- `dotnet test --project tests/UnitTests/UnitTests.csproj` - run the unit test project. This repository uses .NET 10 with Microsoft.Testing.Platform; pass the project using `--project`.
- For xUnit v3 filters, use `--filter-class` or `--filter-method`, for example: `dotnet test --project tests/UnitTests/UnitTests.csproj --filter-class UnitTests.FoobarTest`.
- `dotnet test tests/DevTests/UnitTestsDev.csproj` - DO NOT RUN, intended for human manual testing; run development/integration tests; 
- `cd AppHost && dotnet run --launch-profile http` - start the Aspire development environment (requires Docker and the companion `stockhub-app` checkout as described in `README.md`).

## Key Architecture
- `api/Program.cs` configures the API and its services.
- `api/Controllers/` contains HTTP endpoints.
- `api/Services/` contains application services; `api/Repositories/` contains persistence access.
- `api/Database/`, `api/Models/`, and `api/Migrations/` contain database configuration, domain/persistence models, and EF Core migrations.
- `api/Crawlers/` and `api/Exchanges/` contain market-data crawling and exchange-related code.
- `ServiceDefaults/` contains shared Aspire service configuration.
- `AppHost/` orchestrates local services.
- `tests/UnitTests/` and `tests/DevTests/` contain automated tests.

Read `CONTEXT.md` for the project domain glossary and preserve those definitions when changing domain behavior.

## Data and Configuration
- The API uses PostgreSQL through EF Core and Npgsql. Keep schema changes in EF Core migrations; do not make an untracked manual schema change.
- Local connection strings and credentials belong in .NET user-secrets or environment configuration, never committed `appsettings` files.
- The application reads only the single connection string `ConnectionStrings:StockHubDatabase` (`api/Program.cs`). Keep three user-secret keys locally as a vault: `ConnectionStrings:StockHubDatabaseTest` (TEST), `ConnectionStrings:StockHubDatabase` (UAT), `ConnectionStrings:StockHubDatabaseProd` (PROD). Target selection happens by exporting the chosen value as `ConnectionStrings__StockHubDatabase` (the `__` env form of the `:` key); `ci/with-db.sh` does this for VS Code tasks and launch configs. Check the selected environment before running database commands.
- Firebase Authentication is used for JWT bearer authentication. Follow the existing authentication and endpoint conventions when changing API access.

## EF Core Migration Safety
The VS Code EF tasks (`ef-migrations-add`, `ef-update-test`, `ef-update-uat`, `ef-update-prod`, `ef-migrations-remove`) resolve their target via `ci/with-db.sh`, which copies the matching user-secret vault key into the `ConnectionStrings__StockHubDatabase` environment override (`migrations-add`/`migrations-remove` default to UAT). The VS Code launch configs (`Launch (UAT DB)`, `Launch (PROD DB)`) do the same via a generated `envFile`. Treat these as real environment-changing operations:

- Do not run `database update`, migration removal, or other database-mutating commands unless the requested target is explicit and authorized.
- Never target PROD for routine development or verification.
- For migration work, inspect the generated migration and model snapshot before applying it.
- Configure local secrets as documented in `README.md`; do not include secret values in code, logs, or this guide.

## Change and Test Practices
- Keep changes focused in the owning layer and follow the surrounding C# patterns and namespace/layout conventions.
- When changing an API contract, check its callers and related tests; update the relevant tests with the behavior change.
- Add or update automated test cases for code changes whenever a practical test path exists; prefer focused unit tests that cover both valid and invalid boundary cases. If testing is not feasible, state why and what remains unverified.
- Prefer the narrowest relevant test project. Dev tests may require PostgreSQL, credentials, or other external services; do not assume they are safe to run against a shared database.
- Do not edit generated build output under `bin/` or `obj/`.
- Avoid unrelated formatting and cleanup.

## Documentation
Use `README.md` for setup and run instructions, `CONTEXT.md` for domain terminology, and `document/` for project documentation. Keep these references current when a change alters their documented behavior.
