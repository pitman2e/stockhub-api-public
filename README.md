# TODO
- Data seed at demo mode

# Run Demo (Unfinished)
- WARNING: Unfinished
- Custom JWT authentication is not implemented
- WARNING: Empty database without seed, some data has no GUI to enter information
- docker is required

## Aspire
- Run the Aspire Project from an IDE

## docker-compose
```
docker-compose -f ./ci/docker-compose-demo.yml up --build
```

# Run locally
Recommended to use Visual Studio Code because `.vscode/launch.json` is included (only tested in Linux environment)

Can also use IntelliJ Rider (But `.idea` not included)

Only tested in Linux environment

## To run `Launch (UAT DB)` task
env is configured as: `DB_SECRET_ENV=UAT`


In `Program`, if overriding env `DATABASE_CONSTR` not set, it will choose the text secret from local file
```
string configKey = dbSecretEnv switch
{
    "PROD" => "ConnectionString:StockHubDatabaseProd",
    "TEST" => "ConnectionString:StockHubDatabaseTest",
    "UAT"  => "ConnectionString:StockHubDatabase",
    _      => null
};
```
To add secret locally:
```
dotnet user-secrets set "ConnectionString:StockHubDatabase" "<ConStrHere>"
```
Note that the so call secret stores values in plain text

## Add EF Core migration
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
Create `.\ci\build.uat.sh`, use `.\ci\build.uat.example.sh`

When the Current Directory is the project root, which contains the `ci` folder, run `.\ci\build,uat.sh

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
