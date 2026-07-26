# Run Demo (WARNING: Unfinished)
- TODO: Missing optional database entries, 
- TODO: Some data has no GUI to enter which I wrote directly to database
- TODO: Crawling server missing, only watchlist charts will update

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
- Spin up PostgreSQL database, backend, and frontend
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
- Only spin up the PostgreSQL database and backend (`stockhub-app-public` not required)

#### Via helper script
```
cd ci
./build.demo.sh
```

#### Via Commandline
Run `docker-compose` directly, the above script basically just call `docker-compose` for you. Please reference to the content of `build.demo.sh`

# Run locally (For my reference only)
Add database secret:
```
dotnet user-secrets set "ConnectionStrings:StockHubDatabase" "<ConStrHere>"
```

## Jetbrain Rider
- Open the solution and `SH UAT DB`
- This configuration uses my own setup

## VSCode
- Launch via `Launch (UAT DB)` task

# Add EF Core migration (For my reference only)
For convenience, use `Ctrl + Shift + B` in Visual Studio Code and use the following Tasks:
```
ef-migrations-add
ef-update-test
ef-update-uat
ef-update-prod
ef-migrations-remove
```

Requires setting up `dotnet user-secrets` as mentioned above

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
