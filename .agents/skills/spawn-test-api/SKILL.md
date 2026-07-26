---
name: spawn-test-api
description: Start the isolated PostgreSQL test database for database-backed tests, or the seeded test API stack for API probing.
---

# Use the isolated test stack

Use `ci/docker-compose-test-stack.yml` for local test database work and API probing. It uses the separate `sh-test-stack` Compose project, database `sh_test`, and test-only seed data. It does not use the demo, UAT, or production environments.

Choose one mode based on the task:

- For PostgreSQL-backed automated tests that do not call the API, start only `pg`.
- For API-backed tests or API exploration, start the full stack: `api`, `pg`, and `yfinance`.

The two modes share a Compose project, fixed container names, and host ports. Do not run them concurrently or stop the project while another process is using it. Before starting, check that the required host ports and container names are free. If one is occupied, identify its owner and do not stop unrelated containers.

## PostgreSQL-only mode

From the `stockhub-api` repository root, start only PostgreSQL:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml up -d pg
```

Wait for PostgreSQL to report healthy:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml ps
```

The host connection string is:

```text
Host=localhost;Port=6432;Database=sh_test;Username=pgadmin;Password=temp_password
```

Set `DATABASE_CONSTR` for the test process, for example:

```sh
DATABASE_CONSTR='Host=localhost;Port=6432;Database=sh_test;Username=pgadmin;Password=temp_password' dotnet test
```

Automated tests must create and clean up their own rows; do not depend on fixture rows being present.

## API mode

From the `stockhub-api` repository root, start all three services:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml up --build -d
```

Wait for PostgreSQL to report healthy and the API container to be running:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml ps
```

If startup fails or either service is not ready, inspect this project's logs:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml logs api pg yfinance
```

Confirm the API responds with its Swagger UI:

```sh
curl --fail --silent --show-error http://localhost:4100/swagger/index.html -o /dev/null
```

Use the API base URL `http://localhost:4100` and Swagger UI `http://localhost:4100/swagger/index.html`. The host PostgreSQL connection string is `Host=localhost;Port=6432;Database=sh_test;Username=pgadmin;Password=temp_password`. From another container in this Compose project, use `Host=sh-test-pgdb;Port=5432;Database=sh_test;Username=pgadmin;Password=temp_password`.

The Compose file fixes container names to `sh-test-api`, `sh-test-pgdb`, and `sh-test-yfinance`, and publishes host ports `4100`, `6432`, and `50052` respectively.

## Seed data and cleanup

The SQL and CSV fixtures in `ci/DatabaseSeed/` initialize PostgreSQL only when it creates a fresh data directory. After cleanup with `--volumes`, the next start creates a fresh database and applies those fixtures again. Stopping and restarting the existing stack does not reseed it. Manual API exploration may use the fixture data; automated tests must remain independent of fixture rows and create the data they need.

Only clean up this project if this workflow started it and no other process is using it. Remove the containers and PostgreSQL data volume with:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml down --volumes
```

Confirm the project's services are removed:

```sh
docker-compose -p sh-test-stack -f ci/docker-compose-test-stack.yml ps -a
```
