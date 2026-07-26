---
name: demo-stack
description: Use when user want to explore the API or database with seeded demo data.
disable-model-invocation: true
---

# Use the seeded demo stack

Use this stack for local API exploration or database-backed work that needs the demo seed data. It is a disposable local environment, not UAT or production.

## Start

From the `stockhub-api` repository root, check that Docker Compose is available and that host ports `4000`, `5432`, and `50051` and the fixed container names `sh-api-demo`, `sh-pgdb-demo`, and `sh-yfinance-demo` are free. If one is occupied, identify its owner; do not stop unrelated containers.

Start the same Compose project used by `ci/build.demo.sh` in detached mode so the agent can use the API and database while it runs:

```sh
docker-compose -p sh-demo -f ci/docker-compose-demo.yml up --build -d
```

Wait for PostgreSQL to report healthy and the API container to be running:

```sh
docker-compose -p sh-demo -f ci/docker-compose-demo.yml ps
```

If either service is not ready, inspect its logs with `docker-compose -p sh-demo -f ci/docker-compose-demo.yml logs api pg` and resolve the startup issue before accessing it. Startup is complete when `pg` is healthy and `api` is running.

## Demo access

- API base URL: `http://localhost:4000`
- Swagger UI: `http://localhost:4000/swagger/index.html`
- PostgreSQL from the host: `Host=localhost;Port=5432;Database=sh_demo;Username=pgadmin;Password=temp_password`
- PostgreSQL from another Compose container: `Host=pg;Port=5432;Database=sh_demo;Username=pgadmin;Password=temp_password`

For SQL access, use the host connection with `psql` if installed, or run `docker-compose -p sh-demo -f ci/docker-compose-demo.yml exec pg psql -U pgadmin -d sh_demo`. Use Swagger to discover API routes and their authentication requirements; the demo stack does not imply that protected routes are anonymous.

The SQL and CSV fixtures in `ci/DatabaseSeed/` are applied by PostgreSQL only when it initializes a fresh data directory. The Compose setup has no persistent data volume, so removing its containers discards database changes; restarting an existing initialized container does not rerun the fixtures.

## Stop

Only tear down the stack if this agent started it and it is still the same `sh-demo` project. Stop and remove its containers and locally built images with the cleanup used by `ci/build.demo.sh`:

```sh
docker-compose -p sh-demo -f ci/docker-compose-demo.yml down --rmi local
```

The helper script itself runs Compose in the foreground and reaches its `down` command after `up` exits. For concurrent agent access, use the detached command above and perform cleanup explicitly after work. Cleanup is complete when the `sh-demo` services no longer appear in `docker-compose -p sh-demo -f ci/docker-compose-demo.yml ps -a`.