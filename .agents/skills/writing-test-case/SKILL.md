---
name: writing-test-case
description: Use when writing or updating tests that need PostgreSQL behavior or database-backed test data.
---

# PostgreSQL-backed tests

Use this skill when a test needs real PostgreSQL behavior, such as EF Core queries, transactions, constraints, migrations, or persistence. For tests that only exercise application logic, prefer the existing unit-test conventions and mock dependencies instead of starting a database.

Before running a PostgreSQL-backed test, follow the PostgreSQL-only mode in the `spawn-test-api` skill to start the local test database. Do not start the API, crawler, frontend, or test runner through Docker Compose as part of database provisioning.

Use the connection string provided by that skill through `DATABASE_CONSTR`. In this repository, `TestStockHubContext.Get()` reads that environment variable before falling back to user-secrets.

Keep test data deterministic and limited to the test's needs. The database's initialization scripts provide demo seed data only on first initialization; do not assume those rows are present in a previously used database. Create the rows needed by each test and clean up test-owned data where appropriate. Never point tests at UAT or production databases.