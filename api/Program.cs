using System;
using System.Linq;
using System.Net;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StockHub.Crawlers;
using StockHub.Crawlers.Dividend;
using StockHub.Crawlers.Price;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Repositories;
using StockHub.Services;
using StockHub.Services.Position;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
    builder.AddServiceDefaults(); //Added for Aspire
#endif

// **Logging & Services Configuration**
using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var logger = loggerFactory.CreateLogger("Program");
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpResponseExceptionFilter>();
    options.Filters.Add<ExceptionFilter>();
});

//For Injecting IUserClaims/UserClaims
//https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-context?view=aspnetcore-3.1
builder.Services.AddHttpContextAccessor();

var envCorsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
logger.LogInformation("Env CORS_ORIGINS is {envCorsOrigins}", envCorsOrigins);
var origins = 
    (envCorsOrigins ?? string.Empty).Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(origins);
        });
});

//http://localhost:4000/swagger/index.html
builder.Services.AddSwaggerGen(options =>
{
    //https://stackoverflow.com/questions/46071513/swagger-error-conflicting-schemaids-duplicate-schemaids-detected-for-types-a-a
    //UseFullTypeNameInSchemaIds replacement for .NET Core
    options.CustomSchemaIds(x => x.FullName);

    //https://stackoverflow.com/questions/56234504/migrating-to-swashbuckle-aspnetcore-version-5
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\r\n" +
                      "For example: 'Bearer _JwtToken_'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Scheme = "oauth2",
        In = ParameterLocation.Header,
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => new
            {
                Field = e.Key,
                Errors = string.Join(Environment.NewLine, e.Value.Errors.Select(x => x.ErrorMessage)),
            });

        var apiActionResult = new ApiActionResult<dynamic>
        {
            IsSuccess = false
        };
        foreach(var error in errors)
        {
            apiActionResult.HookErrors.Add(new HookError(error.Field, error.Errors));
        }

        return new BadRequestObjectResult(apiActionResult);
    };
});

// **Forwarded Headers Configuration**
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services
    .AddAuthentication(options =>
    {
        // Set Firebase as the default auth
        options.DefaultAuthenticateScheme = Config.FirebaseScheme;
        options.DefaultChallengeScheme = Config.FirebaseScheme;
    })
    .AddJwtBearer(Config.FirebaseScheme, options =>
    {
        options.IncludeErrorDetails = true;
        options.Authority = "https://securetoken.google.com/stockhub-pm";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/stockhub-pm",
            ValidateAudience = true,
            ValidAudience = "stockhub-pm",
            ValidateLifetime = true
        };
    });

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:SecretKey"];
if (!string.IsNullOrWhiteSpace(jwtSecretKey))
{
    logger.LogInformation("jwtSecretKey configured: using custom JWT auth");
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(Config.CustomScheme, options =>
        {
            options.IncludeErrorDetails = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = true,
                ValidIssuer = "StockHub",
                ValidateAudience = true,
                ValidAudience = "StockHubClient",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireFirebaseJwt", policy =>
    {
        policy.AuthenticationSchemes.Add(Config.FirebaseScheme);
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("RequireCustomJwt", policy =>
    {
        policy.AuthenticationSchemes.Add(Config.CustomScheme);
        policy.RequireAuthenticatedUser();
    });
});

// **Database Setup**

// Get connection string from Secret Store
// To set the secret, for example:
// dotnet user-secrets set "ConnectionString:StockHubDatabase" "<ConStrHere>"
{
    var dbSecretEnv = Environment.GetEnvironmentVariable("DB_SECRET_ENV");
    logger.LogInformation("dbSecretEnv is {DbSecretEnv}", dbSecretEnv);

    string configKey = dbSecretEnv switch
    {
        "PROD" => "ConnectionString:StockHubDatabaseProd",
        "TEST" => "ConnectionString:StockHubDatabaseTest",
        "UAT"  => "ConnectionString:StockHubDatabase",
        _      => null
    };
    string secretConStr = configKey != null ? builder.Configuration[configKey] : null;
    
    var envConStr = Environment.GetEnvironmentVariable("DATABASE_CONSTR");
    logger.LogInformation("ENV Constr length is {envConStrLength}", (envConStr + "").Length);

    string actingConstr;
    if (!string.IsNullOrWhiteSpace(envConStr))
    {
        logger.LogInformation("Env Constr is set, use env constr as database constr");
        actingConstr = envConStr;
    }
    else if (!string.IsNullOrWhiteSpace(dbSecretEnv))
    {
        logger.LogInformation("Env Constr not set and Secret Constr is set, use Secret Constr as database constr");
        actingConstr = secretConStr;
    }
    else
    {
        logger.LogError("Both Secret Constr and env Constr are not set. Aborting...");
        throw new InvalidOperationException("No connection string is configured");
    }

    builder.Services.AddDbContext<StockHubContext>(options =>
        options.UseNpgsql(actingConstr, npgsqlOptions =>
            {
                // Retries database commands if connection is temporarily refused
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
    );
}

// **DI**
builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
{
    httpClientBuilder
        .ConfigureHttpClient(HttpClientFactory.ConfigureHttpClient)
        .ConfigurePrimaryHttpMessageHandler(HttpClientFactory.GetDefaultHttpHandler);
});
builder.Services.AddHttpClient();

builder.Services.AddTransient<IUserClaims, UserClaims>();

builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<PositionValueService>();
builder.Services.AddScoped<PositionValueServiceV1>();
builder.Services.AddScoped<StocksService>();
builder.Services.AddScoped<DividendService>();
builder.Services.AddScoped<WatchlistService>();
builder.Services.AddScoped<RealisedDividendService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<Stock2ExchangeService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RealisedScripService>();
builder.Services.AddScoped<TagsService>();

builder.Services.AddScoped<DividendRepo>();
builder.Services.AddScoped<PortfolioRepo>();
builder.Services.AddScoped<TransactionRepo>();
builder.Services.AddScoped<PositionRepo>();
builder.Services.AddScoped<StockRepo>();

builder.Services.AddScoped<AllExchanges>();
builder.Services.AddScoped<CASH>();
builder.Services.AddScoped<HK>();
builder.Services.AddScoped<HKBND>();
builder.Services.AddScoped<HSBC>();
builder.Services.AddScoped<LSE>();
builder.Services.AddScoped<MANU>();
builder.Services.AddScoped<PCP>();
builder.Services.AddScoped<US>();
builder.Services.AddScoped<USBND>();

builder.Services.AddScoped<StockPriceCrawler>();
builder.Services.AddScoped<IHsbcPriceCrawler, HsbcPriceCrawler>();
builder.Services.AddScoped<IPcpPriceCrawler, PcpPriceCrawler>();
builder.Services.AddScoped<IYfinancePriceCrawler, YfinancePriceCrawler>();

builder.Services.AddScoped<DividendCrawler>();
builder.Services.AddScoped<IAastockHKDividendCrawler, AastockHKDividendCrawler>();
builder.Services.AddScoped<IAastockUSDividendCrawler, AastockUSDividendCrawler>();
builder.Services.AddScoped<IYfinanceDividendCrawler, YfinanceDividendCrawler>();
builder.Services.AddScoped<IBondDummyCrawler, BondDummyCrawler>();

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// **HTTP Request Pipeline Configuration**
var app = builder.Build();

#if DEBUG
    app.MapDefaultEndpoints(); //Added for Aspire
#endif

app.UseExceptionHandler(options =>
{
    options.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync("").ConfigureAwait(false);
    });
});

// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//app.UseHsts();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    app.UseSwagger();
}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockHub API v1");
    });
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();