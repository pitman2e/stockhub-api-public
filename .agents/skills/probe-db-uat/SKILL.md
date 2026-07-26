---
name: probe-db-uat
description: Read-only UAT database probe for bugs unreproducible in mock tests (human-invoked only).
disable-model-invocation: true
---

# Probe DB UAT

Human-invoked only. You run only when the human explicitly invokes this skill. Never auto-activate, never suggest activation on your own. Invocation is permission; no further permission check is needed.

Use when the human asks to inspect live UAT data because a bug cannot be reproduced in mock or isolated tests.

## Step 1 - Resolve connection without printing

Run from the repo root:

```sh
dotnet user-secrets list --project ./api
```

Take the exact key `ConnectionStrings:StockHubDatabase`. Export it to `$DATABASE_CONSTR` and keep it in the environment only.

- Never print the secret value in chat, tool output, logs, error messages, or files. When shape must be shown, write `Host=***;Database=***`.
- Never paste the value into a command line; reference `$DATABASE_CONSTR` only.
- If the key is missing, stop and reply: "UAT connection not configured - create `ConnectionStrings:StockHubDatabase` via user-secrets, then re-invoke." Do nothing else.

## Step 2 - Orient via code-first schema

Primary source of truth is EF Core code-first entities in `api/Database/` (including `StockHubContext.cs` and `EntityConfigurations/`).

Use `ci/DatabaseSeed/20-sh_demo_schema.sql` only for on-demand column-level detail. Never inline the full dump.

## Step 3 - Probe read-only with psql

The user-secrets value is .NET Npgsql format (`Host=...;Port=...;Database=...;Username=...;Password=...`, semicolon-separated). `psql` (libpq) rejects it directly, so convert in memory to libpq keyword format without ever printing the secret:

- Extract to env only in one command so the secret never lands in output:
  ```sh
  DATABASE_CONSTR=$(dotnet user-secrets list --project ./api 2>&1 | sed -n 's/^ConnectionStrings:StockHubDatabase = //p')
  ```
- Convert in memory (`$DATABASE_CONSTR` -> `host= port= dbname= user= password=` plus `sslmode=` when present). Key map: `Host`/`Server`->`host`, `Port`->`port`, `Database`->`dbname`, `Username`/`User Id`->`user`, `Password`->`password`, `Ssl Mode`->`sslmode`. Split segments on `;`, split each on the first `=`. Never echo the value or the converted string; pass it to `psql` via environment/subprocess only.
- Query with `psql` via the converted conninfo only, e.g.:
  ```sh
  psql "$LIBPQ_CONN" -v statement_timeout=10000 -c "SET TRANSACTION READ ONLY; SELECT ... LIMIT 100;"
  ```

- Allowlist: statements starting with `SELECT`, `EXPLAIN`, or `WITH ... SELECT` only.
- Prohibited: `UPDATE`, `DELETE`, `INSERT`, DDL (`ALTER`, `CREATE`, `DROP`, `TRUNCATE`), `COPY`, `GRANT`, `VACUUM`, `CALL`, EF `SaveChanges`, migrations, seeds, `database update`. On any such statement, abort. Writes are prohibited. Do not perform them and do not ask the human to perform them.
- Default `EXPLAIN` without `ANALYZE`. Use `EXPLAIN (ANALYZE, BUFFERS)` only on explicit human request.
- Every query requires `WHERE` plus `LIMIT` (max 100 rows). No unbounded `SELECT *`, no full-table dumps.

## Step 4 - Report redacted

- Cite the bounded `SELECT` behind every finding.
- Mask PII (emails, Firebase UIDs, tokens) and the connection string as `<REDACTED>`. Quote only the lines carrying the signal.

Done when every finding cites its bounded read-only query, no secret value appears in any output, and zero write statements were executed.
