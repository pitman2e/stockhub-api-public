var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

var postgres = builder.AddPostgres("postgres", username, password)
    .WithImage("postgres", "18")
    .WithHostPort(5432)
    .WithEnvironment("POSTGRES_DB", "sh_demo")
    .WithEnvironment("TZ", "Asia/Hong_Kong")
    .WithEnvironment("PGTZ", "Asia/Hong_Kong")
    .WithBindMount("../ci/DatabaseSeed/", "/docker-entrypoint-initdb.d/");

//Automatically inject constr
var shDemoDb = postgres.AddDatabase("StockHubDatabase", "sh_demo");

var api = builder.AddProject<Projects.StockHub>("api")
    .WithReference(shDemoDb)
    .WaitFor(shDemoDb)
    .WithEnvironment("STOCKHUB_API_PY_BASE_URL", builder.Configuration["STOCKHUB_API_PY_BASE_URL"])
    .WithEnvironment("JWT_SECRET_KEY", builder.Configuration["JWT_SECRET_KEY"])
    .WithEnvironment("CORS_ORIGINS", builder.Configuration["CORS_ORIGINS"])
    .WithEnvironment("IS_DEMO", builder.Configuration["IS_DEMO"]);

var appPath = Directory.Exists("../../stockhub-app") 
    ? "../../stockhub-app" 
    : "../../stockhub-app-public";

if (Directory.Exists(appPath))
{
    var vite = builder.AddViteApp("vite", appPath)
        .WithNpmPackageInstallation()
        .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http", isProxied: false)
        .WithReference(api)
        .WaitFor(api)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("http").Property(EndpointProperty.Url))
        .WithEnvironment("VITE_DEMO_JWT", builder.Configuration["JWT"] ?? "");
}

builder.Build().Run();