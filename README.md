# Run Demo
- TODO: Missing optional database entries, 
- Crawling latest prices need to manually triggered from the "Admin" page (Cooldown applies)
## TL;DR
```
git clone https://github.com/pitman2e/stockhub-api-public
git clone https://github.com/pitman2e/stockhub-app-public
cd ./stockhub-api-public/AppHost
dotnet run --launch-profile http
```

## TL;DR (SKILL.md Version; DB and API only - Untested)
```
/demo-stack
```

## Requirements
### Postgresql Database
- docker
### C# Backend
#### Run locally
- [Optional] IDE (VSCode / Rider / VS)
- dotnet SDK 10
#### Run via Aspire
- dotnet SDK 10
- docker
#### Run via docker-compose
- docker
### Frontend Vite React App
#### Run via Aspire
- npm
#### Run via docker-compose
- docker

## Steps
### Aspire
- Spin up PostgreSQL database, backend, crawler and frontend
- This repo's folder `stockhub-api-public` should be parallel with another frontend repo's folder `stockhub-app-public`
- Open the link of the 'vite' project from the Aspire Dashboard

#### Via IDE
- Run the Aspire Project with Launch Profile `http`

#### Via Commandline
```
cd AppHost
dotnet run --launch-profile http
```

### docker-compose
- Only spin up the PostgreSQL database, backend and crawler (`stockhub-app-public` not required)

#### Via helper script
```
cd ci
./build.demo.sh
```

#### Via Commandline
Run `docker-compose` directly, the above script basically just call `docker-compose` for you. Please reference to the content of `build.demo.sh`

# Run locally (For my reference only)
Add database secrets (local vault; `Program.cs` reads only the bare UAT key, `ci/with-db.sh` selects the target via the `ConnectionStrings__StockHubDatabase` override):
```
dotnet user-secrets set "ConnectionStrings:StockHubDatabase" "<UatConStrHere>" --project ./api
dotnet user-secrets set "ConnectionStrings:StockHubDatabaseTest" "<TestConStrHere>" --project ./api
dotnet user-secrets set "ConnectionStrings:StockHubDatabaseProd" "<ProdConStrHere>" --project ./api
```

## Jetbrain Rider
- Open the solution and `SH UAT DB`
- This configuration uses my own setup

## VSCode
- Launch via `Launch (UAT DB)` or `Launch (PROD DB)`; the `gen-env-uat`/`gen-env-prod` pre-launch task copies the matching user-secret into a git-ignored `.vscode/.env.<ENV>.generated` file consumed via `envFile` (the `launch.json` `"env"` block is static and cannot call scripts directly).

# Add EF Core migration (For my reference only)
For convenience, use `Ctrl + Shift + B` in Visual Studio Code and use the following Tasks (each resolves its DB via `ci/with-db.sh TEST|UAT|PROD`; `ef-migrations-add`/`ef-migrations-remove` default to UAT):
```
ef-migrations-add
ef-update-test
ef-update-uat
ef-update-prod
ef-migrations-remove
```

Requires setting up `dotnet user-secrets` as mentioned above. Equivalent manual form without VS Code:
```
./ci/with-db.sh UAT ${HOME}/.dotnet/tools/dotnet-ef database update
```

## JetBrains Rider
Rider's EF migration preview runs `dotnet ef` with an explicit `--connection "<ConStrHere>"`, e.g.:
```
dotnet ef database update --project api/StockHub.csproj --startup-project api/StockHub.csproj \
  --context StockHub.Database.StockHubContext --configuration Debug \
  "<migration>" --connection "<ConStrHere>"
```

Design-time `DbContext` creation order and how it interacts with this repo:

1. The tools look for an `IDesignTimeDbContextFactory<StockHubContext>` — there is none, so they fall through to building the app host from `Program.cs` and resolving `DbContextOptions<StockHubContext>` from DI.
2. `Program.cs` therefore executes up to `builder.Build()` (service registration, `GetConnectionString("StockHubDatabase")`, `AddDbContext`, entity configuration, `AuditInterceptor`). `app.Run()` is not invoked. The `StockHubContext(string connectionString)` constructor plays no role here — it is test-only (`tests/`, `DevTests`); the tools construct the context from the DI-resolved options.
3. After the options are built, `--connection` replaces just the relational connection string on them. The migration model comes from your code; the target server comes from `--connection`, never from user-secrets or `ConnectionStrings__StockHubDatabase`.

Practical consequences:

- The bare `ConnectionStrings:StockHubDatabase` user-secret must still exist locally: `Program.cs:228-231` throws `InvalidOperationException("No connection string is configured")` at host-build time when it is missing, which fails even a PROD-targeted `--connection` command before anything is applied.
- Always verify the `--connection` value's `Database=` segment (`sh_test` vs `sh_uat` vs `sh_prod`) in Rider's preview before applying — unlike the VS Code tasks, nothing maps or labels the target for you, and `database update` against PROD runs immediately.

# Build docker image and run (For UAT)
```
cd ci
cp build.uat.example.sh build.uat.sh
```
Modify `build.uat.sh` as needed, then run
```
./build.uat.sh
```

# Authentication and Authorization
- Firebase Authentication - Accepts JWT bearer from Google Cloud Firebase authentication services:

## Settings:
In `Program.cs`
```
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    ...)
```

- No Authorization control

# Jenkins Schedule Job Setting:
- Required plugins: [Environment Injector](https://plugins.jenkins.io/envinject/)
- Create as a `Pipeline` project

## Settings:
`Triggers` > `Build periodically`
```
H/5 * * * *
```
### Environment
- Checks `Delete workspace before build starts`
#### Inject passwords to the build as environment variables
```
Job passwords:
Name: JWT
Password: Bearer MDZjOGxFN1hrS0abcd1234
```
Checks `Mask password parameters`

### Build Steps
```
curl -f -H "Authorization: $JWT" http://localhost:4000/api/ScheduledJobs/CrawlStockPrice_Minutely
```

# AI Usage Disclosure
- Originally hand-written and AI-assisted recently (mostly for refactoring and boilerplate coding, effectiveness limited by free tier LLM)
- LLM used: GitHub Copilot Free, Gemini
