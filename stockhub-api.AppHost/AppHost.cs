using Microsoft.Extensions.DependencyInjection;
#pragma warning disable ASPIREPROCESSCOMMAND001

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

var postgres = builder.AddPostgres("postgres", username, password)
    .WithImage("postgres", "18")
    .WithHostPort(5432)
    .WithEnvironment("TZ", "Asia/Hong_Kong")
    .WithEnvironment("PGTZ", "Asia/Hong_Kong")
    .WithBindMount("../ci/DatabaseSeed/", "/docker-entrypoint-initdb.d/");

var shDemoDb = postgres.AddDatabase("shDemo", "sh_demo");

var api = builder.AddProject<Projects.StockHub>("api")
    .WithReference(shDemoDb)
    .WaitFor(shDemoDb)
    .WithEnvironment("DATABASE_CONSTR", ReferenceExpression.Create(
        $"User ID={username};Password={password};" +
        $"Host={postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)};" +
        $"Port={postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)};" +
        $"Database={shDemoDb.Resource.DatabaseName}"))
    .WithEnvironment("STOCKHUB_API_PY_BASE_URL", builder.Configuration["STOCKHUB_API_PY_BASE_URL"] ?? "")
    .WithEnvironment("JWT_SECRET_KEY", builder.Configuration["JWT_SECRET_KEY"] ?? "")
    .WithEnvironment("IS_DEMO", "1")
    .WithEnvironment("CORS_ORIGINS", "http://localhost:3000;http://127.0.0.1:3000");

builder.Build().Run();